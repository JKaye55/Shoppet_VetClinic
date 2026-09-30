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
            NormalizeRole(CurrentUser?.Role) == "Admin";

        public bool IsPetOwner =>
            NormalizeRole(CurrentUser?.Role) == "Pet Owner";

        // Legacy role retained only so old code can compile.
        // It is never active in Final Scope v4.0.
        public bool IsClinicStaff => false;

        public bool IsPremium => CurrentUser?.IsPremium == true;

        public bool CanAddMorePets => IsPetOwner && IsPremium;

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
            if (CurrentUser == null)
                return false;

            var activeRole = NormalizeRole(CurrentUser.Role);
            var required = NormalizeRole(requiredRole);

            return activeRole == required;
        }

        public static string NormalizeRole(string? role)
        {
            var value = (role ?? string.Empty).Trim();

            if (value.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
                return "Admin";

            if (value.Equals("Pet Owner", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("PetOwner", StringComparison.OrdinalIgnoreCase))
                return "Pet Owner";

            // Clinic Staff / Business Owner / other legacy roles are
            // outside the active Final Scope v4.0.
            return string.Empty;
        }

        public bool HasActiveRole =>
            CurrentUser is not null &&
            !string.IsNullOrEmpty(NormalizeRole(CurrentUser.Role));
    }
}