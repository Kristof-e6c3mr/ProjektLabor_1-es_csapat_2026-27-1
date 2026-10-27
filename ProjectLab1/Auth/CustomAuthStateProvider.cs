using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;


namespace ProjectLab1.Auth;

// own Token holder class
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    public string? JwtToken { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        //Anonymous if no login
        return Task.FromResult(new AuthenticationState(_anonymous));
    }

    public void MarkUserAsAuthenticated(string token)
    {
        JwtToken = token;

        // JWT token unencrypt
        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);

        // Hand claims to Webapp
        var identity = new ClaimsIdentity(jwtSecurityToken.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        // Notify Webapp on change of  login
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void MarkUserAsLoggedOut()
    {
        JwtToken = null;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }
}
