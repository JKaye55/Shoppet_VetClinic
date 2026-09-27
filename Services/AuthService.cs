using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services
{
    public class AuthService
    {
        private const string UserIdKey = "userId";
        private const string GuestModeKey = "guestMode";

        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly DatabaseService _db;

        private event Action? _stateChanged;

        private bool _initialized;
        private bool _initializing;

        public UserAccount? CurrentUser { get; private set; }

        public bool IsLoggedIn => CurrentUser is not null;
        public bool IsAuthenticated => CurrentUser is not null;
        public bool IsGuest { get; private set; }
        public bool IsSignedOut => !IsLoggedIn && !IsGuest;

        public bool IsAdmin =>
            string.Equals(CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(CurrentUser?.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(CurrentUser?.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);

        public bool IsClinicStaff =>
            string.Equals(
                CurrentUser?.Role,
                "Clinic Staff",
                StringComparison.OrdinalIgnoreCase);

        public bool IsPetOwner =>
            string.Equals(
                CurrentUser?.Role,
                "Pet Owner",
                StringComparison.OrdinalIgnoreCase);

        public bool IsPremium =>
            CurrentUser?.IsPremium == true;

        public bool CanAddMorePets =>
            !IsPetOwner || IsPremium;

        public AuthService(
            ProtectedSessionStorage sessionStorage,
            DatabaseService db)
        {
            _sessionStorage = sessionStorage;
            _db = db;
        }

        // =========================================================
        // STATE LISTENERS
        // =========================================================

        public void RegisterListener(Action listener)
        {
            _stateChanged += listener;
        }

        public void UnregisterListener(Action listener)
        {
            _stateChanged -= listener;
        }

        private void NotifyStateChanged()
        {
            _stateChanged?.Invoke();
        }

        // =========================================================
        // INITIALIZE SESSION
        // =========================================================

        public async Task InitializeAsync()
        {
            if (_initialized || _initializing)
                return;

            _initializing = true;

            try
            {
                var storedUser =
                    await _sessionStorage.GetAsync<int>(UserIdKey);

                if (storedUser.Success &&
                    storedUser.Value > 0)
                {
                    var user =
                        _db.GetUserById(storedUser.Value);

                    if (user is not null)
                    {
                        CurrentUser = user;
                        IsGuest = false;

                        _initialized = true;

                        NotifyStateChanged();

                        return;
                    }
                }

                var storedGuest =
                    await _sessionStorage.GetAsync<bool>(
                        GuestModeKey);

                CurrentUser = null;

                IsGuest =
                    storedGuest.Success &&
                    storedGuest.Value;

                _initialized = true;

                NotifyStateChanged();
            }
            catch
            {
                // ProtectedSessionStorage is not available
                // during prerender. Retry when interactive.
                _initialized = false;
            }
            finally
            {
                _initializing = false;
            }
        }

        // =========================================================
        // LOGIN
        // =========================================================

        public async Task LoginAsync(
            UserAccount user)
        {
            CurrentUser = user;

            IsGuest = false;

            _initialized = true;

            try
            {
                await _sessionStorage.SetAsync(
                    UserIdKey,
                    user.Id);

                await _sessionStorage.DeleteAsync(
                    GuestModeKey);
            }
            catch
            {
            }

            NotifyStateChanged();
        }

        // =========================================================
        // GUEST MODE
        // =========================================================

        public async Task EnterGuestModeAsync()
        {
            CurrentUser = null;

            IsGuest = true;

            _initialized = true;

            try
            {
                await _sessionStorage.DeleteAsync(
                    UserIdKey);

                await _sessionStorage.SetAsync(
                    GuestModeKey,
                    true);
            }
            catch
            {
            }

            NotifyStateChanged();
        }

        public async Task ExitGuestModeAsync()
        {
            CurrentUser = null;

            IsGuest = false;

            _initialized = true;

            try
            {
                await _sessionStorage.DeleteAsync(
                    GuestModeKey);

                await _sessionStorage.DeleteAsync(
                    UserIdKey);
            }
            catch
            {
            }

            NotifyStateChanged();
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        public async Task LogoutAsync()
        {
            CurrentUser = null;

            IsGuest = false;

            _initialized = true;

            try
            {
                await _sessionStorage.DeleteAsync(
                    UserIdKey);

                await _sessionStorage.DeleteAsync(
                    GuestModeKey);
            }
            catch
            {
            }

            NotifyStateChanged();
        }

        // =========================================================
        // OLD METHODS
        //
        // Keep these temporarily because some of your older
        // Admin/Clinic components still use them.
        // =========================================================

        public void Login(
            UserAccount user)
        {
            CurrentUser = user;

            IsGuest = false;

            _initialized = true;

            _ = PersistUserAsync(user.Id);

            NotifyStateChanged();
        }

        public void Logout()
        {
            CurrentUser = null;

            IsGuest = false;

            _initialized = true;

            _ = ClearPersistedAsync();

            NotifyStateChanged();
        }

        private async Task PersistUserAsync(
            int userId)
        {
            try
            {
                await _sessionStorage.SetAsync(
                    UserIdKey,
                    userId);

                await _sessionStorage.DeleteAsync(
                    GuestModeKey);
            }
            catch
            {
            }
        }

        private async Task ClearPersistedAsync()
        {
            try
            {
                await _sessionStorage.DeleteAsync(
                    UserIdKey);

                await _sessionStorage.DeleteAsync(
                    GuestModeKey);
            }
            catch
            {
            }
        }

        // =========================================================
        // REFRESH USER
        // =========================================================

        public void RefreshUser(
            UserAccount updated)
        {
            CurrentUser = updated;

            IsGuest = false;

            NotifyStateChanged();
        }

        // =========================================================
        // ROLE ACCESS
        // =========================================================

        public bool CanAccess(
            string requiredRole)
        {
            if (CurrentUser is null)
                return false;

            if (string.Equals(
                    requiredRole,
                    "Pet Owner",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    requiredRole,
                    "Clinic Staff",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    IsClinicStaff ||
                    IsAdmin;
            }

            if (string.Equals(
                    requiredRole,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return IsAdmin;
            }

            if (string.Equals(
                    requiredRole,
                    "SuperAdmin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    string.Equals(
                        CurrentUser.Role,
                        "SuperAdmin",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    string.Equals(
                        CurrentUser.Role,
                        "Super Admin",
                        StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}