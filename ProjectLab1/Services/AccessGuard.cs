using Microsoft.AspNetCore.Components;

namespace ProjectLab1.Services
{
    public static class AccessGuard
    {
        public static bool DenyIfMissingRole(UserSessionService session, NavigationManager navigation, params string[] allowedRoles)
        {
            if (!session.IsLoggedIn)
            {
                navigation.NavigateTo("/login");
                return true;
            }

            if (!allowedRoles.Contains(session.Role, StringComparer.Ordinal))
            {
                navigation.NavigateTo("/forbidden");
                return true;
            }

            return false;
        }
    }
}
