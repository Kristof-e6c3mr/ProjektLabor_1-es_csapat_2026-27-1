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

            var response = await base.SendAsync(request, cancellationToken);

            // 401 = hiányzó/lejárt/érvénytelen token: a munkamenet megszűnt, ki kell jelentkeztetni.
            // A 403 (jogosultság hiánya) NEM jelent kijelentkeztetést.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _userSession.Logout();
            }

            return response;
        }
    }
}