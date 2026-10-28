using System.Net.Http.Headers;

namespace ProjectLab1.Services
{
    public class JwtAuthorizationHandler : DelegatingHandler
    {
        private readonly UserSessionService _userSession;

        public JwtAuthorizationHandler(UserSessionService userSession)
        {
            _userSession = userSession;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(_userSession.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _userSession.Token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}