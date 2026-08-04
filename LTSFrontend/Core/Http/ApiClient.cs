using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LTSFrontend.Core.Auth;
using LTSFrontend.Core.Exceptions;
using LTSFrontend.Core.Models;
using LTSFrontend.State;

namespace LTSFrontend.Core.Http
{
    public class ApiClient
    {
        public HttpClient Http { get; }

        private readonly UserSessionState _session;
        private readonly ITokenStorageService _tokenStorage;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // Guards TryRefreshAccessTokenAsync so that several requests firing
        // at once right as the token expires (e.g. a page that kicks off
        // 3-4 API calls in parallel on load) don't each independently hit
        // POST /auth/refresh-token - only the first waits on the real
        // network call, the rest wait on this lock and then reuse whatever
        // token it produced.
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        // ================================================================
        // ROOT-CAUSE FIX: the Bearer token used to be attached by a
        // DelegatingHandler (AuthTokenHandler) wired in via
        // AddHttpMessageHandler<T>(). That is a well-known ASP.NET Core
        // pitfall: IHttpClientFactory builds/pools its message-handler
        // pipeline in its OWN internal DI scope, separate from - and
        // reused across - the actual per-circuit/per-request scope. Any
        // scoped service (like UserSessionState) injected into a handler
        // registered that way gets captured ONCE from whichever scope
        // happened to build the pooled handler, and is then reused for
        // every request across every user/circuit for up to
        // HandlerLifetime (default 2 minutes) - i.e. effectively a
        // different, permanently-empty UserSessionState than the one the
        // rest of the app (Login page, MainLayout, etc.) actually uses.
        // That's why the name showed correctly in the top bar (read
        // directly from the *correctly*-scoped Session) while every API
        // call still 401'd (Authorization header built from the
        // *wrongly*-scoped one) - no amount of retrying or browser-storage
        // rehydration inside that handler could ever fix it.
        //
        // ApiClient itself doesn't have this problem: AddHttpClient<T>()
        // constructs the typed client class (this class) using the
        // caller's own real scope, so UserSessionState/ITokenStorageService
        // injected directly here are always the correct, live-for-this-
        // circuit instances. Attaching the header here instead is the fix.
        // ================================================================
        public ApiClient(HttpClient httpClient, UserSessionState session, ITokenStorageService tokenStorage)
        {
            Http = httpClient;
            _session = session;
            _tokenStorage = tokenStorage;
        }

        public Task<T?> GetAsync<T>(string url, CancellationToken ct = default) =>
            SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, url), ct);

        public Task<T?> PostAsync<T>(string url, object? body = null, CancellationToken ct = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (body != null)
                request.Content = JsonContent.Create(body, options: JsonOptions);
            return SendAsync<T>(request, ct);
        }

        public Task<T?> PutAsync<T>(string url, object? body = null, CancellationToken ct = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            if (body != null)
                request.Content = JsonContent.Create(body, options: JsonOptions);
            return SendAsync<T>(request, ct);
        }

        public Task<T?> PostFormAsync<T>(string url, MultipartFormDataContent form, CancellationToken ct = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            return SendAsync<T>(request, ct);
        }

        public Task<T?> PutFormAsync<T>(string url, MultipartFormDataContent form, CancellationToken ct = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = form };
            return SendAsync<T>(request, ct);
        }

        public Task<T?> DeleteAsync<T>(string url, CancellationToken ct = default) =>
            SendAsync<T>(new HttpRequestMessage(HttpMethod.Delete, url), ct);

        private async Task EnsureAuthorizationHeaderAsync(HttpRequestMessage request)
        {
            // If this circuit's session hasn't been populated yet (e.g. a
            // fresh browser tab / hard refresh, before CustomAuthStateProvider
            // got a chance to run), fall back to browser storage. By the
            // time an actual HTTP call is being made, the circuit is
            // guaranteed to be connected and JS interop guaranteed
            // available, so this is a safe, reliable last chance.
            if (!_session.IsAuthenticated)
            {
                var stored = await _tokenStorage.GetSessionAsync();
                if (stored != null && stored.AccessTokenExpiry > DateTime.UtcNow)
                {
                    _session.Set(
                        stored.UserID, stored.FullName, stored.Email, stored.Role,
                        stored.AccessToken, stored.AccessTokenExpiry);
                }
            }

            // ================================================================
            // SILENT TOKEN REFRESH: the access token is short-lived (60 min
            // by default - see JwtSettings.ExpiryMinutes on the backend).
            // Without this, once it expires every single request would
            // start failing with 401 until the user manually logs out and
            // back in, even though a perfectly valid refresh-token cookie
            // exists. Refresh proactively - a little before actual expiry -
            // using that HttpOnly cookie, so requests always go out with a
            // live token instead of reactively retrying after a 401 (which
            // would mean cloning/resending the original request, including
            // any multipart file-upload content that can only be read
            // once - proactive refresh avoids that whole class of bugs).
            // ================================================================
            bool hasKnownIdentity = _session.UserID != 0;
            bool tokenMissingOrExpiring = string.IsNullOrWhiteSpace(_session.AccessToken) ||
                !_session.AccessTokenExpiry.HasValue || _session.AccessTokenExpiry.Value <= DateTime.UtcNow.AddSeconds(30);

            if (hasKnownIdentity && tokenMissingOrExpiring)
            {
                await TryRefreshAccessTokenAsync();
            }

            if (!string.IsNullOrWhiteSpace(_session.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
            }
        }

        private async Task<bool> TryRefreshAccessTokenAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                // Someone else may have already refreshed while we were
                // waiting for the lock - re-check before making another
                // network call.
                if (!string.IsNullOrWhiteSpace(_session.AccessToken) && _session.AccessTokenExpiry.HasValue &&
                    _session.AccessTokenExpiry.Value > DateTime.UtcNow.AddSeconds(30))
                {
                    return true;
                }

                // Talk to the raw HttpClient directly here, NOT this
                // class's own SendAsync<T> - that would recurse back into
                // EnsureAuthorizationHeaderAsync. The refresh-token cookie
                // (HttpOnly, sent automatically) is all this endpoint
                // needs; it's [AllowAnonymous] on the backend.
                var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoints.Auth.RefreshToken);
                var response = await Http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var raw = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<ApiResponse<Features.Auth.Models.RefreshTokenResponseDTO>>(raw, JsonOptions);

                if (parsed?.Success != true || parsed.Data == null)
                {
                    return false;
                }

                _session.UpdateAccessToken(parsed.Data.AccessToken, parsed.Data.AccessTokenExpiry);

                // Persist the refreshed token too, so a brand new circuit
                // (new tab, F5) started right after this also picks up the
                // live token instead of the now-stale one that was
                // originally saved at login.
                await _tokenStorage.SaveSessionAsync(new StoredSession(
                    _session.UserID, _session.FullName, _session.Email, _session.Role,
                    _session.AccessToken!, _session.AccessTokenExpiry!.Value));

                return true;
            }
            catch
            {
                // Network hiccup, refresh token genuinely expired/revoked,
                // etc. - fall through and let the original request go out
                // with whatever token (possibly none) we already have; the
                // resulting 401, if any, surfaces normally to the caller.
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
        {
            await EnsureAuthorizationHeaderAsync(request);

            HttpResponseMessage response;
            try
            {
                response = await Http.SendAsync(request, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException(
                    "Backend se connect nahi ho saka. Ensure LTSBackend API is running on " +
                    Http.BaseAddress, null, new() { ex.Message });
            }

            var raw = await response.Content.ReadAsStringAsync(ct);

            // ================================================================
            // ROOT-CAUSE FIX: error responses were being deserialized into
            // ApiResponse<T> - the SAME T the caller expects back on
            // success (e.g. `int` for CreateFirmAsync). LTSBackend's error
            // envelope never sets Data, so it comes back as JSON null/
            // absent; System.Text.Json refuses to bind that into a
            // non-nullable value-type T (int/long/bool), throws
            // JsonException, which the old code silently swallowed - along
            // with the perfectly good Message/Errors sitting right next to
            // that Data field in the same payload. That's why validation
            // errors (e.g. "Firm code can only contain letters, numbers,
            // and hyphens.") were showing up on screen as a generic
            // "Request failed with status 400" instead of the real reason.
            //
            // Fix: parse error bodies with a small envelope that has no
            // Data property at all, so it can never fail to bind regardless
            // of what T the caller asked for. Only ever deserialize into
            // ApiResponse<T> once we know the call actually succeeded.
            // ================================================================
            if (!response.IsSuccessStatusCode)
            {
                string message = $"Request failed with status {(int)response.StatusCode} ({response.StatusCode}).";
                List<string>? errors = null;

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ApiErrorEnvelope>(raw, JsonOptions);
                        if (problem != null)
                        {
                            if (!string.IsNullOrWhiteSpace(problem.Message))
                            {
                                message = problem.Message;
                            }
                            errors = problem.Errors;
                        }
                    }
                    catch (JsonException)
                    {
                        // Not JSON at all (e.g. an IIS/Kestrel error page) -
                        // keep the generic status-code fallback message.
                    }
                }

                throw new ApiException(message, (int)response.StatusCode, errors);
            }

            ApiResponse<T>? parsed = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    parsed = JsonSerializer.Deserialize<ApiResponse<T>>(raw, JsonOptions);
                }
                catch (JsonException)
                {
                    parsed = null;
                }
            }

            // If expected return type is bool and response is successful with null/empty content
            if (typeof(T) == typeof(bool) && parsed == null)
            {
                return (T)(object)true;
            }

            if (parsed == null)
            {
                throw new ApiException("Server se invalid response mila.", (int)response.StatusCode);
            }

            if (!parsed.Success)
            {
                throw new ApiException(parsed.Message, (int)response.StatusCode, parsed.Errors);
            }

            return parsed.Data;
        }
    }
}
