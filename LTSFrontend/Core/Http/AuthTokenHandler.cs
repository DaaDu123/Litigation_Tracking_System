using System.Net.Http.Headers;
using LTSFrontend.Core.Auth;
using LTSFrontend.State;

namespace LTSFrontend.Core.Http
{
    /// <summary>
    /// DelegatingHandler that stamps every outgoing API request with the
    /// current user's "Authorization: Bearer {token}" header, read from
    /// the scoped UserSessionState. Chained in front of ApiClient's
    /// HttpClientHandler.
    ///
    /// ROOT-CAUSE FIX: UserSessionState is normally populated once, early
    /// in the circuit's life, by CustomAuthStateProvider.GetAuthenticationStateAsync()
    /// reading the encrypted session out of ProtectedLocalStorage. That
    /// read needs real JS interop, which is only available *after* the
    /// interactive circuit has connected - not during the static prerender
    /// pass. If GetAuthenticationStateAsync() happens to run (and gets
    /// cached by Blazor's <CascadingAuthenticationState>) before the
    /// circuit connects, it silently resolves to "logged out" and nothing
    /// ever retries it - every API call for the rest of that circuit's
    /// life then goes out with no Authorization header and gets a 401,
    /// even though the user is genuinely logged in and the token exists in
    /// browser storage.
    ///
    /// This handler only ever runs when an actual HTTP call is being made,
    /// which can't happen until the circuit is fully connected and JS
    /// interop is live - so it's a safe, guaranteed-to-work last chance to
    /// rehydrate the session from storage before giving up and sending the
    /// request unauthenticated.
    /// </summary>
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly UserSessionState _session;
        private readonly ITokenStorageService _tokenStorage;

        public AuthTokenHandler(UserSessionState session, ITokenStorageService tokenStorage)
        {
            _session = session;
            _tokenStorage = tokenStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
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
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _session.AccessToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
