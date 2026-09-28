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
                "Your 3-month ShoppetCare Premium access is active. Premium Pet ID themes, eligible private card details, selected care summaries, and supported export features are now available.",
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

        public void SellerActivated(int userId, DateTime expiresAt)
        {
            _db.CreateNotification(
                userId,
                "Verified Seller activated",
                $"You can now create and manage marketplace listings until {expiresAt:MMM d, yyyy}.",
                "/marketplace",
                "bi-shop");
        }

        public void ClinicClaimApproved(int userId, int clinicId)
        {
            _db.CreateNotification(
                userId,
                "Clinic claim approved",
                "Your clinic claim was approved. Complete the Verified Clinic subscription to activate the badge and clinic management benefits.",
                "/clinic-portal",
                "bi-building-check");
        }

        public void ClinicListingApproved(int userId, int clinicId)
        {
            _db.CreateNotification(
                userId,
                "Clinic listing approved",
                "Your requested clinic was added to the ShoppetCare directory. You may now submit a claim for the listing.",
                $"/claim-clinic/{clinicId}",
                "bi-hospital");
        }

        public void ClinicRequestRejected(int userId, string kind)
        {
            _db.CreateNotification(
                userId,
                $"{kind} request update",
                "Your request was reviewed but could not be approved. Open the clinic directory or contact ShoppetCare if you need to submit updated information.",
                "/clinics",
                "bi-info-circle");
        }

        public void ClinicSubscriptionActivated(int userId, int clinicId, DateTime expiresAt)
        {
            _db.CreateNotification(
                userId,
                "Verified Clinic activated",
                $"Your clinic verification is active until {expiresAt:MMM d, yyyy}.",
                "/clinic-portal",
                "bi-patch-check-fill");
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