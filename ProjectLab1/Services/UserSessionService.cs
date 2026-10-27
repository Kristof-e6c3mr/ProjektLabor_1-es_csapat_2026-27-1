namespace ProjectLab1.Services
{
    public class UserSessionService
    {
        public string Username { get; private set; } = "";
        public string Token { get; private set; } = "";
        public string Role { get; private set; } = "";
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        public event Action? OnChange;

        public void SetUser(string username, string token, string role)
        {
            Username = username;
            Token = token;
            Role = role;
            NotifyStateChanged();
        }

        public void Logout()
        {
            Username = "";
            Token = "";
            Role = "";
            NotifyStateChanged();
        }

        public void ClearSession() => Logout();

        private void NotifyStateChanged()
        {
            try
            {
                OnChange?.Invoke();
            }
            catch
            {
                // Elnyomjuk a szálkezelési hibaüzenetet, hogy ne akadályozza a működést
            }
        }
    }
}