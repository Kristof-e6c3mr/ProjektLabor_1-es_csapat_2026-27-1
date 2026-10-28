using System;

namespace ProjectLab1.Services
{
    public class UserSessionService
    {
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

        public void Logout()
        {
            UserId = 0;
            Username = "";
            Token = "";
            Role = "";
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}