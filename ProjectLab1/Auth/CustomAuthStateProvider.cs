using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Authorization;

namespace ProjectLab1.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    private ClaimsPrincipal _currentUser;
    public string? JwtToken { get; private set; }

    public CustomAuthStateProvider()
    {
        _currentUser = _anonymous;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public void MarkUserAsAuthenticated(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        JwtToken = token;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwtSecurityToken.Claims, "jwt");
            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }
        catch
        {
            MarkUserAsLoggedOut();
        }
    }

    public void MarkUserAsLoggedOut()
    {
        JwtToken = null;
        _currentUser = _anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }
}