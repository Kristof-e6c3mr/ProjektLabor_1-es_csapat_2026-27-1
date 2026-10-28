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
            if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
            {
                return false;
            }

            navigation.NavigateTo("/forbidden");
            return true;
        }
    }
}
