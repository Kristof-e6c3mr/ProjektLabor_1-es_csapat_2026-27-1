using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Components;

namespace ProjectLab1.Services
{
    public static class HttpAccess
    {
        public static async Task<T?> GetJsonAsync<T>(HttpClient http, NavigationManager navigation, string requestUri)
        {
            var response = await http.GetAsync(requestUri);
            if (RedirectIfDenied(response, navigation))
            {
                return default;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public static bool RedirectIfDenied(HttpResponseMessage response, NavigationManager navigation)
        {
            // 401: a bejelentkezés érvénytelen/lejárt (a handler már kijelentkeztetett)
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                navigation.NavigateTo("/login");
                return true;
            }

            // 403: be van jelentkezve, de nincs jogosultsága – a fiók megmarad
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                navigation.NavigateTo("/forbidden");
                return true;
            }

            return false;
        }
    }
}