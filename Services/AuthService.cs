using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Shoppet_VetClinic.Models;
using System.Net.Mail;

namespace Shoppet_VetClinic.Services
{
    public class AuthService
    {
        private const string UserIdKey = "userId";
        private const string GuestModeKey = "guestMode";

        public const int MinimumPasswordLength = 8;
        public const int MaximumPasswordLength = 64;

        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly DatabaseService _db;

        private event Action? _stateChanged;

        private bool _initialized;
        private bool _initializing;


        public UserAccount? CurrentUser { get; private set; }

        public bool IsLoggedIn =>
            CurrentUser is not null;

        public bool IsAuthenticated =>
            CurrentUser is not null;

        public bool IsGuest { get; private set; }

        public bool IsSignedOut =>
            !IsLoggedIn &&
            !IsGuest;


        public bool IsAdmin =>
            IsRole("Admin")
            || IsRole("SuperAdmin")
            || IsRole("Super Admin");


        public bool IsClinicStaff =>
            IsRole("Clinic Staff")
            || IsRole("Clinic Owner")
            || IsRole("Clinic Representative");

        public bool IsClinicOwner =>
            IsRole("Clinic Owner")
            || IsRole("Clinic Representative");


        public bool IsPetOwner =>
            IsRole("Pet Owner") || IsRole("PetOwner");


        public bool IsPremium => CurrentUser?.IsPremium == true;

        // Kept for compatibility with existing pages.
        public bool CanAddMorePets =>
            !IsPetOwner ||
            IsPremium;

        private bool IsRole(string role) =>
            string.Equals(
                CurrentUser?.Role?.Trim(),
                role,
                StringComparison.OrdinalIgnoreCase);


        public AuthService(
            ProtectedSessionStorage sessionStorage,
            DatabaseService db)
        {
            _sessionStorage = sessionStorage;
            _db = db;
        }


        // =========================================================
        // CENTRAL VALIDATION
        // =========================================================

        public static string NormalizeEmail(
            string? email)
        {
            return
                email?
                    .Trim()
                    .ToLowerInvariant()
                ??
                string.Empty;
        }


        public static bool IsValidEmailAddress(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return false;
            }


            var email =
                value.Trim();


            if (email.Length > 254)
                return false;


            try
            {
                var parsed =
                    new MailAddress(
                        email);


                return string.Equals(
                    parsed.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }


        public static string? ValidateFullName(
            string? value)
        {
            var name =
                value?.Trim()
                ??
                string.Empty;


            if (name.Length < 2)
            {
                return
                    "Please enter your full name.";
            }


            if (name.Length > 150)
            {
                return
                    "Full name cannot exceed 150 characters.";
            }


            return null;
        }


        public static string? ValidateNewPassword(
            string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Please enter a password.";
            }

            if (value.Length < 8)
            {
                return "Must be at least 8 characters and include a special character.";
            }

            if (value.Length > MaximumPasswordLength)
            {
                return $"Password cannot exceed {MaximumPasswordLength} characters.";
            }

            if (!value.Any(char.IsLetter))
            {
                return "Password must contain at least one letter.";
            }

            if (!value.Any(char.IsDigit))
            {
                return "Password must contain at least one number.";
            }

            if (!value.Any(c => "!@#$%^&*(),.?\":{}|<>".Contains(c) || (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))))
            {
                return "Must be at least 8 characters and include a special character (!@#$%^&*(),.?\":{}|<>).";
            }

            return null;
        }


        // =========================================================
        // LISTENERS
        // =========================================================

        public void RegisterListener(
            Action listener)
        {
            _stateChanged +=
                listener;
        }


        public void UnregisterListener(
            Action listener)
        {
            _stateChanged -=
                listener;
        }


        private void NotifyStateChanged()
        {
            _stateChanged?.Invoke();
        }


        // =========================================================
        // INITIALIZE
        // =========================================================

        public async Task InitializeAsync()
        {
            if (_initialized ||
                _initializing)
            {
                return;
            }


            _initializing =
                true;


            try
            {
                var storedUser =
                    await _sessionStorage
                        .GetAsync<int>(
                            UserIdKey);


                if (storedUser.Success &&
                    storedUser.Value > 0)
                {
                    var user =
                        _db.GetUserById(
                            storedUser.Value);


                    if (user is not null &&
                        new[] { "Pet Owner", "PetOwner", "Admin", "SuperAdmin", "Super Admin" }
                            .Contains(user.Role?.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        CurrentUser =
                            user;


                        IsGuest =
                            false;


                        _initialized =
                            true;


                        NotifyStateChanged();


                        return;
                    }
                }


                var storedGuest =
                    await _sessionStorage
                        .GetAsync<bool>(
                            GuestModeKey);


                CurrentUser =
                    null;


                IsGuest =
                    storedGuest.Success
                    &&
                    storedGuest.Value;


                _initialized =
                    true;


                NotifyStateChanged();
            }
            catch
            {
                // ProtectedSessionStorage isn't available
                // during prerender. Initialization will retry.
                _initialized =
                    false;
            }
            finally
            {
                _initializing =
                    false;
            }
        }


        // =========================================================
        // LOGIN
        // =========================================================

        public async Task LoginAsync(
            UserAccount user)
        {
            CurrentUser =
                user;


            IsGuest =
                false;


            _initialized =
                true;


            try
            {
                await _sessionStorage
                    .SetAsync(
                        UserIdKey,
                        user.Id);


                await _sessionStorage
                    .DeleteAsync(
                        GuestModeKey);
            }
            catch
            {
            }


            NotifyStateChanged();
        }


        // =========================================================
        // GUEST
        // =========================================================

        public async Task EnterGuestModeAsync()
        {
            CurrentUser =
                null;


            IsGuest =
                true;


            _initialized =
                true;


            try
            {
                await _sessionStorage
                    .DeleteAsync(
                        UserIdKey);


                await _sessionStorage
                    .SetAsync(
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
            CurrentUser =
                null;


            IsGuest =
                false;


            _initialized =
                true;


            try
            {
                await _sessionStorage
                    .DeleteAsync(
                        GuestModeKey);


                await _sessionStorage
                    .DeleteAsync(
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
            CurrentUser =
                null;


            IsGuest =
                false;


            _initialized =
                true;


            try
            {
                await _sessionStorage
                    .DeleteAsync(
                        UserIdKey);


                await _sessionStorage
                    .DeleteAsync(
                        GuestModeKey);
            }
            catch
            {
            }


            NotifyStateChanged();
        }


        // =========================================================
        // LEGACY COMPATIBILITY
        // =========================================================

        public void Login(
            UserAccount user)
        {
            CurrentUser =
                user;


            IsGuest =
                false;


            _initialized =
                true;


            _ =
                PersistUserAsync(
                    user.Id);


            NotifyStateChanged();
        }


        public void Logout()
        {
            CurrentUser =
                null;


            IsGuest =
                false;


            _initialized =
                true;


            _ =
                ClearPersistedAsync();


            NotifyStateChanged();
        }


        private async Task PersistUserAsync(
            int userId)
        {
            try
            {
                await _sessionStorage
                    .SetAsync(
                        UserIdKey,
                        userId);


                await _sessionStorage
                    .DeleteAsync(
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
                await _sessionStorage
                    .DeleteAsync(
                        UserIdKey);


                await _sessionStorage
                    .DeleteAsync(
                        GuestModeKey);
            }
            catch
            {
            }
        }

        // =========================================================
        // REFRESH
        // =========================================================

        public void RefreshUser(
            UserAccount updated)
        {
            if (updated is null)
            {
                return;
            }


            CurrentUser =
                updated;


            IsGuest =
                false;


            NotifyStateChanged();
        }


        /// <summary>
        /// Reloads the currently authenticated user directly
        /// from the database.
        ///
        /// Use this after account changes such as:
        /// - Premium activation
        /// - Premium renewal
        /// - Profile updates
        /// - Mobile-number updates
        ///
        /// This prevents pages from relying on stale
        /// CurrentUser information.
        /// </summary>
        public bool RefreshCurrentUser()
        {
            if (CurrentUser is null)
            {
                return false;
            }


            var refreshed =
                _db.GetUserById(
                    CurrentUser.Id);


            if (refreshed is null)
            {
                return false;
            }


            CurrentUser =
                refreshed;


            IsGuest =
                false;


            NotifyStateChanged();


            return true;
        }

        // =========================================================
        // ROLE ACCESS
        // =========================================================

        public bool CanAccess(
            string requiredRole)
        {
            if (CurrentUser is null)
            {
                return false;
            }


            if (string.Equals(
                requiredRole,
                "Pet Owner",
                StringComparison.OrdinalIgnoreCase))
            {
                return IsPetOwner;
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
