using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services
{
    public class AuthService
    {
        public UserAccount? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;
        private Action? _listener;

        public void RegisterListener(Action listener) => _listener = listener;
        public void UnregisterListener(Action listener) => _listener = null;

        public void Login(UserAccount user)
        {
            CurrentUser = user;
            _listener?.Invoke();
        }

        public void Logout()
        {
            CurrentUser = null;
            _listener?.Invoke();
        }

        public bool CanAccess(string requiredRole)
        {
            if (CurrentUser == null) return requiredRole == "Pet Owner";
            if (requiredRole == "Pet Owner") return true;
            if (requiredRole == "Clinic Staff" && (CurrentUser.Role == "Clinic Staff" || CurrentUser.Role == "Admin")) return true;
            if (requiredRole == "Admin" && CurrentUser.Role == "Admin") return true;
            return false;
        }
    }
}