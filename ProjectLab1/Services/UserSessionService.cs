using System;

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ProjectLab1.Services
{
    public class UserSessionService
    {
        private const string TarolasKulcs = "user-session";

        private readonly ProtectedLocalStorage _tarolas;

        public UserSessionService(ProtectedLocalStorage tarolas)
        {
            _tarolas = tarolas;
        }

        public int UserId { get; private set; }
        public string Username { get; private set; } = "";
        public string Role { get; private set; } = "";
        public string Token { get; private set; } = "";

        public bool IsLoggedIn => !string.IsNullOrEmpty(Username);

        public event Action? OnChange;

        public void SetUser(int id, string username, string token, string role)
        {
            UserId = id;
            Username = username;
            Token = token;
            Role = role;
            NotifyStateChanged();
        }

        public async Task PersistAsync()
        {
            await _tarolas.SetAsync(TarolasKulcs, new StoredSession
            {
                Id = UserId,
                Username = Username,
                Token = Token,
                Role = Role
            });
        }

        public async Task<bool> RestoreAsync()
        {
            try
            {
                var eredmeny = await _tarolas.GetAsync<StoredSession>(TarolasKulcs);
                if (!eredmeny.Success || eredmeny.Value == null)
                {
                    return false;
                }

                SetUser(eredmeny.Value.Id, eredmeny.Value.Username, eredmeny.Value.Token, eredmeny.Value.Role);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Logout()
        {
            UserId = 0;
            Username = "";
            Token = "";
            Role = "";
            _ = TorlesTarolasbolAsync();
            NotifyStateChanged();
        }

        private async Task TorlesTarolasbolAsync()
        {
            try
            {
                await _tarolas.DeleteAsync(TarolasKulcs);
            }
            catch
            {
                // A tároló törlése legfeljebb elmarad, a memóriabeli kijelentkezés így is megtörtént
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        public class StoredSession
        {
            public int Id { get; set; }
            public string Username { get; set; } = "";
            public string Token { get; set; } = "";
            public string Role { get; set; } = "";
        }
    }
}
