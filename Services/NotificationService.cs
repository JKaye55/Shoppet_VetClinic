using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services
{
    /// <summary>
    /// Creates notifications for real events across the app.
    /// Each method represents one type of notification with a
    /// specific Bootstrap icon and tone.
    /// </summary>
    public class NotificationService
    {
        private readonly DatabaseService _db;

        public NotificationService(DatabaseService db) => _db = db;

        // =========================================================
        // PINK HEART — Welcome & Pet Love
        // =========================================================
        public void WelcomeUser(int userId, string name)
        {
            var firstName = string.IsNullOrWhiteSpace(name)
                ? "friend"
                : name.Trim().Split(' ').First();

            _db.CreateNotification(
                userId,
                $"Welcome aboard, {firstName}!",
                "So glad you're here. Your pet's health, records, and ID card — all in one place. Ready to meet your new best friend?",
                "/my-pets",
                "bi-heart-fill");
        }

        // =========================================================
        // GOLD STAR — Premium Upgrade
        // =========================================================
        public void PremiumActivated(int userId, string reference)
        {
            _db.CreateNotification(
                userId,
                "You're now Premium",
                "Thanks for supporting ShoppetCare. Unlimited pets, unlimited records, and premium card themes are now unlocked.",
                "/my-pets",
                "bi-star-fill");
        }

        // =========================================================
        // GREEN CHECK — Clinic Verification
        // =========================================================
        public void ClinicVerified(int clinicId)
        {
            var staff = _db.GetClinicStaffUsers()
                .Where(u => u.ClinicId == clinicId);

            foreach (var user in staff)
            {
                _db.CreateNotification(
                    user.Id,
                    "Your clinic is verified",
                    "Your ShoppetCare listing now shows the Verified Partner badge. Pet owners will see this as a sign of trust.",
                    "/clinic/profile",
                    "bi-patch-check-fill");
            }
        }

        // =========================================================
        // BLUE MEGAPHONE — System Announcements
        // =========================================================
        public void NewAnnouncement(Announcement a)
        {
            var users = _db.GetAllUsers()
                .Where(u => u.Role == "Pet Owner");

            foreach (var u in users)
            {
                _db.CreateNotification(
                    u.Id,
                    a.Title,
                    a.Body.Length > 100 ? a.Body.Substring(0, 100) + "..." : a.Body,
                    "/announcements",
                    "bi-megaphone-fill");
            }
        }

        // =========================================================
        // PINK PULSE — Health Records
        // =========================================================
        public void HealthRecordAdded(int petId, string petName, int ownerUserId)
        {
            _db.CreateNotification(
                ownerUserId,
                $"New health record for {petName}",
                "Open your pet's card to see the update.",
                $"/pet/{petId}/card",
                "bi-heart-pulse-fill");
        }

        // =========================================================
        // BLUE CALENDAR — Appointment Reminders
        // =========================================================
        public void AppointmentReminder(int userId, string petName, DateTime when)
        {
            _db.CreateNotification(
                userId,
                "Appointment reminder",
                $"{petName} has an appointment on {when:MMM d 'at' h:mm tt}. Don't forget!",
                "/dashboard",
                "bi-calendar-event");
        }

        // =========================================================
        // GENERIC — For custom use
        // =========================================================
        public void Custom(
            int userId,
            string title,
            string body,
            string link = "",
            string icon = "bi-bell-fill")
        {
            _db.CreateNotification(userId, title, body, link, icon);
        }
    }
}