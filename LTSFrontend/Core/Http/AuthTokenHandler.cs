using System.Net.Http.Headers;
using LTSFrontend.State;

namespace LTSFrontend.Core.Http
{
    /// <summary>
    /// DelegatingHandler that stamps every outgoing API request with the
    /// current user's "Authorization: Bearer {token}" header, read from
    /// the scoped UserSessionState. Chained in front of ApiClient's
    /// HttpClientHandler.
    /// </summary>
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly UserSessionState _session;

        public AuthTokenHandler(UserSessionState session)
        {
            _session = session;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_session.AccessToken))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _session.AccessToken);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
