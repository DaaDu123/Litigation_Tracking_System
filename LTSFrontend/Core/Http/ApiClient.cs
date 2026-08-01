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

            if (!string.IsNullOrWhiteSpace(_session.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
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

            if (!response.IsSuccessStatusCode)
            {
                var message = parsed?.Message ??
                    $"Request failed with status {(int)response.StatusCode} ({response.StatusCode}).";
                throw new ApiException(message, (int)response.StatusCode, parsed?.Errors);
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
