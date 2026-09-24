using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services
{
    public class AuthService
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly DatabaseService _db;

        public UserAccount? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        public bool IsAdmin =>
            string.Equals(CurrentUser?.Role, "Admin",
                StringComparison.OrdinalIgnoreCase);

        public bool IsClinicStaff =>
            string.Equals(CurrentUser?.Role, "Clinic Staff",
                StringComparison.OrdinalIgnoreCase);

        public bool IsPetOwner =>
            string.Equals(CurrentUser?.Role, "Pet Owner",
                StringComparison.OrdinalIgnoreCase);

        public bool IsPremium => CurrentUser?.IsPremium == true;

        public bool CanAddMorePets => !IsPetOwner || IsPremium;

        private Action? _listener;
        private bool _initialized;

        public AuthService(
            ProtectedSessionStorage sessionStorage,
            DatabaseService db)
        {
            _sessionStorage = sessionStorage;
            _db = db;
        }

        public void RegisterListener(Action listener) => _listener = listener;
        public void UnregisterListener(Action listener) => _listener = null;

        // =========================================================
        // Persistence — call this once on first render
        // =========================================================

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            try
            {
                var stored = await _sessionStorage.GetAsync<int>("userId");

                if (stored.Success && stored.Value > 0)
                {
                    var user = _db.GetUserById(stored.Value);

                    if (user is not null)
                    {
                        CurrentUser = user;
                    }
                }

                // Only mark as initialized AFTER a successful read.
                _initialized = true;

                _listener?.Invoke();
            }
            catch
            {
                // Prerender phase — ProtectedSessionStorage not available.
                // Do NOT set _initialized = true, so we retry on the
                // next interactive render.
                _initialized = false;
            }
        }
        public async Task LoginAsync(UserAccount user)
        {
            CurrentUser = user;

            try
            {
                await _sessionStorage.SetAsync("userId", user.Id);
            }
            catch { /* ignore prerender errors */ }

            _listener?.Invoke();
        }

        public async Task LogoutAsync()
        {
            CurrentUser = null;

            try
            {
                await _sessionStorage.DeleteAsync("userId");
            }
            catch { /* ignore */ }

            _listener?.Invoke();
        }

        /// <summary>
        /// Legacy sync login — kept for backward compatibility with existing
        /// code. Prefer LoginAsync for persistence.
        /// </summary>
        public void Login(UserAccount user)
        {
            CurrentUser = user;
            _ = PersistUserAsync(user.Id);
            _listener?.Invoke();
        }

        public void Logout()
        {
            CurrentUser = null;
            _ = ClearPersistedAsync();
            _listener?.Invoke();
        }

        private async Task PersistUserAsync(int userId)
        {
            try { await _sessionStorage.SetAsync("userId", userId); }
            catch { }
        }

        private async Task ClearPersistedAsync()
        {
            try { await _sessionStorage.DeleteAsync("userId"); }
            catch { }
        }

        public void RefreshUser(UserAccount updated)
        {
            CurrentUser = updated;
            _listener?.Invoke();
        }

        public bool CanAccess(string requiredRole)
        {
            if (CurrentUser == null) return requiredRole == "Pet Owner";

            if (requiredRole == "Pet Owner") return true;

            if (requiredRole == "Clinic Staff" &&
                (CurrentUser.Role == "Clinic Staff" ||
                 CurrentUser.Role == "Admin")) return true;

            if (requiredRole == "Admin" && CurrentUser.Role == "Admin") return true;

            return false;
        }
    }
}