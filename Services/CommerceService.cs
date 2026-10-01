using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;
using System.Data;

namespace Shoppet_VetClinic.Services
{
    public class CommerceService
    {
        public const string PremiumType = "PremiumSubscription";
        public const string SellerType = "SellerSubscription";
        public const string ClinicType = "ClinicSubscription";

        private readonly string _connectionString;

        public CommerceService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("ShoppetDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'ShoppetDb' was not found.");
        }

        public SubscriptionOffering? GetOffering(string? type)
        {
            return type switch
            {
                PremiumType => new SubscriptionOffering
                {
                    Type = PremiumType,
                    Name = "ShoppetCare Premium",
                    ShortDescription = "Lifetime Digital Pet ID features and expanded pet-owner benefits.",
                    Amount = 49m,
                    DurationMonths = 1200
                },
                SellerType => new SubscriptionOffering
                {
                    Type = SellerType,
                    Name = "Verified Seller",
                    ShortDescription = "Create and manage pre-loved pet-item listings with a Verified Seller badge.",
                    Amount = 50m,
                    DurationMonths = 1
                },
                ClinicType => new SubscriptionOffering
                {
                    Type = ClinicType,
                    Name = "Verified Clinic",
                    ShortDescription = "Verified badge and management access for your claimed clinic.",
                    Amount = 50m,
                    DurationMonths = 1
                },
                _ => null
            };
        }

        public SubscriptionRecord? GetActiveSubscription(
            string type,
            int? userId = null,
            int? clinicId = null)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT TOP 1
                    s.Id,
                    s.UserId,
                    s.ClinicId,
                    s.SubscriptionType,
                    s.Status,
                    s.Reference,
                    s.StartsAt,
                    s.ExpiresAt,
                    s.CreatedAt,
                    ISNULL(u.FullName, ''),
                    ISNULL(c.ClinicName, '')
                FROM Subscriptions s
                LEFT JOIN UserAccounts u ON u.Id = s.UserId
                LEFT JOIN ClinicTenants c ON c.Id = s.ClinicId
                WHERE s.SubscriptionType = @Type
                  AND s.Status = 'Active'
                  AND s.StartsAt <= SYSDATETIME()
                  AND s.ExpiresAt >= SYSDATETIME()
                  AND (@UserId IS NULL OR s.UserId = @UserId)
                  AND (@ClinicId IS NULL OR s.ClinicId = @ClinicId)
                ORDER BY s.ExpiresAt DESC;",
                conn);

            cmd.Parameters.AddWithValue("@Type", type);
            cmd.Parameters.AddWithValue("@UserId",
                userId.HasValue ? userId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ClinicId",
                clinicId.HasValue ? clinicId.Value : DBNull.Value);

            using var reader = cmd.ExecuteReader();

            return reader.Read()
                ? MapSubscription(reader)
                : null;
        }

        public bool IsSellerActive(int userId) =>
            GetActiveSubscription(SellerType, userId: userId) is not null;

        public bool IsClinicSubscriptionActive(int clinicId) =>
            GetActiveSubscription(ClinicType, clinicId: clinicId) is not null;

        public bool IsPremiumActive(int userId)
        {
            if (userId <= 0) return false;

            var active =
                GetActiveSubscription(PremiumType, userId: userId);

            if (active is not null)
                return true;

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM UserAccounts
                WHERE Id = @UserId
                  AND ISNULL(IsPremium, 0) = 1;",
                conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public List<SubscriptionRecord> GetSubscriptions(string? type = null)
        {
            var items = new List<SubscriptionRecord>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    s.Id,
                    s.UserId,
                    s.ClinicId,
                    s.SubscriptionType,
                    s.Status,
                    s.Reference,
                    s.StartsAt,
                    s.ExpiresAt,
                    s.CreatedAt,
                    ISNULL(u.FullName, ''),
                    ISNULL(c.ClinicName, '')
                FROM Subscriptions s
                LEFT JOIN UserAccounts u ON u.Id = s.UserId
                LEFT JOIN ClinicTenants c ON c.Id = s.ClinicId
                WHERE (@Type IS NULL OR s.SubscriptionType = @Type)
                ORDER BY s.CreatedAt DESC;",
                conn);

            cmd.Parameters.AddWithValue("@Type",
                string.IsNullOrWhiteSpace(type)
                    ? DBNull.Value
                    : type);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                items.Add(MapSubscription(reader));

            return items;
        }

        public PaymentReceipt CompleteMockPayment(
            int userId,
            string type,
            string paymentMethod,
            int? clinicId = null)
        {
            var offering =
                GetOffering(type)
                ?? throw new InvalidOperationException(
                    "The selected subscription plan is not valid.");

            if (userId <= 0)
                throw new InvalidOperationException(
                    "A signed-in account is required.");

            var allowedMethods = new[]
            {
                "GCash",
                "Maya",
                "Card"
            };

            if (!allowedMethods.Contains(
                    paymentMethod,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The selected payment method is not supported.");
            }

            if (type == ClinicType && !clinicId.HasValue)
            {
                throw new InvalidOperationException(
                    "A claimed clinic is required for the Verified Clinic subscription.");
            }

            var now = DateTime.Now;
            var reference =
                $"SC-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                DateTime startsAt = now;

                using (var existingCmd = new SqlCommand(@"
                    SELECT MAX(ExpiresAt)
                    FROM Subscriptions
                    WHERE SubscriptionType = @Type
                      AND Status = 'Active'
                      AND (@UserId IS NULL OR UserId = @UserId)
                      AND (@ClinicId IS NULL OR ClinicId = @ClinicId);",
                    conn,
                    tx))
                {
                    existingCmd.Parameters.AddWithValue("@Type", type);
                    existingCmd.Parameters.AddWithValue("@UserId",
                        type == ClinicType ? DBNull.Value : userId);
                    existingCmd.Parameters.AddWithValue("@ClinicId",
                        type == ClinicType && clinicId.HasValue
                            ? clinicId.Value
                            : DBNull.Value);

                    var currentExpiry = existingCmd.ExecuteScalar();

                    if (currentExpiry is DateTime expiry &&
                        expiry > startsAt)
                    {
                        startsAt = expiry;
                    }
                }

                var expiresAt =
                    startsAt.AddMonths(offering.DurationMonths);

                int transactionId;

                using (var paymentCmd = new SqlCommand(@"
                    INSERT INTO Transactions
                    (
                        UserId,
                        ClinicId,
                        Type,
                        Amount,
                        Reference,
                        PaymentMethod,
                        Status,
                        PaidAt
                    )
                    OUTPUT INSERTED.Id
                    VALUES
                    (
                        @UserId,
                        @ClinicId,
                        @Type,
                        @Amount,
                        @Reference,
                        @PaymentMethod,
                        'Paid',
                        @PaidAt
                    );",
                    conn,
                    tx))
                {
                    paymentCmd.Parameters.AddWithValue("@UserId", userId);
                    paymentCmd.Parameters.AddWithValue("@ClinicId",
                        clinicId.HasValue ? clinicId.Value : DBNull.Value);
                    paymentCmd.Parameters.AddWithValue("@Type", type);

                    var amount = new SqlParameter("@Amount", SqlDbType.Decimal)
                    {
                        Precision = 10,
                        Scale = 2,
                        Value = offering.Amount
                    };

                    paymentCmd.Parameters.Add(amount);
                    paymentCmd.Parameters.AddWithValue("@Reference", reference);
                    paymentCmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                    paymentCmd.Parameters.AddWithValue("@PaidAt", now);

                    transactionId =
                        Convert.ToInt32(paymentCmd.ExecuteScalar());
                }

                using (var subscriptionCmd = new SqlCommand(@"
                    INSERT INTO Subscriptions
                    (
                        UserId,
                        ClinicId,
                        SubscriptionType,
                        Status,
                        Reference,
                        StartsAt,
                        ExpiresAt,
                        CreatedAt
                    )
                    VALUES
                    (
                        @UserId,
                        @ClinicId,
                        @Type,
                        'Active',
                        @Reference,
                        @StartsAt,
                        @ExpiresAt,
                        @CreatedAt
                    );",
                    conn,
                    tx))
                {
                    subscriptionCmd.Parameters.AddWithValue("@UserId",
                        type == ClinicType ? DBNull.Value : userId);
                    subscriptionCmd.Parameters.AddWithValue("@ClinicId",
                        type == ClinicType && clinicId.HasValue
                            ? clinicId.Value
                            : DBNull.Value);
                    subscriptionCmd.Parameters.AddWithValue("@Type", type);
                    subscriptionCmd.Parameters.AddWithValue("@Reference", reference);
                    subscriptionCmd.Parameters.AddWithValue("@StartsAt", startsAt);
                    subscriptionCmd.Parameters.AddWithValue("@ExpiresAt", expiresAt);
                    subscriptionCmd.Parameters.AddWithValue("@CreatedAt", now);
                    subscriptionCmd.ExecuteNonQuery();
                }

                if (type == PremiumType)
                {
                    using var premiumCmd = new SqlCommand(@"
                        UPDATE UserAccounts
                        SET IsPremium = 1,
                            PremiumActivatedAt = @StartsAt,
                            PremiumReference = @Reference
                        WHERE Id = @UserId;",
                        conn,
                        tx);

                    premiumCmd.Parameters.AddWithValue("@StartsAt", startsAt);
                    premiumCmd.Parameters.AddWithValue("@Reference", reference);
                    premiumCmd.Parameters.AddWithValue("@UserId", userId);
                    premiumCmd.ExecuteNonQuery();
                }

                if (type == ClinicType && clinicId.HasValue)
                {
                    using var clinicCmd = new SqlCommand(@"
                        UPDATE ClinicTenants
                        SET IsVerified = 1,
                            VerifiedAt = @PaidAt,
                            VerificationFee = @Amount,
                            VerificationReference = @Reference,
                            VerificationStatus = 'Verified',
                            SubscriptionPlan = 'Verified Clinic - Monthly'
                        WHERE Id = @ClinicId;",
                        conn,
                        tx);

                    clinicCmd.Parameters.AddWithValue("@PaidAt", now);

                    var amount = new SqlParameter("@Amount", SqlDbType.Decimal)
                    {
                        Precision = 10,
                        Scale = 2,
                        Value = offering.Amount
                    };

                    clinicCmd.Parameters.Add(amount);
                    clinicCmd.Parameters.AddWithValue("@Reference", reference);
                    clinicCmd.Parameters.AddWithValue("@ClinicId", clinicId.Value);
                    clinicCmd.ExecuteNonQuery();
                }

                tx.Commit();

                return new PaymentReceipt
                {
                    TransactionId = transactionId,
                    Reference = reference,
                    SubscriptionType = type,
                    OfferingName = offering.Name,
                    Amount = offering.Amount,
                    PaymentMethod = paymentMethod,
                    Status = "Paid",
                    PaidAt = now,
                    StartsAt = startsAt,
                    ExpiresAt = expiresAt
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public int SubmitClinicListingRequest(
            int userId,
            ClinicListingRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var duplicateCmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM ClinicListingRequests
                WHERE UserId = @UserId
                  AND ClinicName = @ClinicName
                  AND Status = 'Pending';",
                conn);

            duplicateCmd.Parameters.AddWithValue("@UserId", userId);
            duplicateCmd.Parameters.AddWithValue("@ClinicName", request.ClinicName.Trim());

            if (Convert.ToInt32(duplicateCmd.ExecuteScalar()) > 0)
            {
                throw new InvalidOperationException(
                    "You already have a pending listing request for this clinic.");
            }

            using var cmd = new SqlCommand(@"
                INSERT INTO ClinicListingRequests
                (
                    UserId,
                    RepresentativeName,
                    RelationshipToClinic,
                    ClinicName,
                    Address,
                    City,
                    ContactNumber,
                    ClinicEmail,
                    ServicesOffered,
                    Message,
                    Status,
                    CreatedAt
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @UserId,
                    @RepresentativeName,
                    @RelationshipToClinic,
                    @ClinicName,
                    @Address,
                    @City,
                    @ContactNumber,
                    @ClinicEmail,
                    @ServicesOffered,
                    @Message,
                    'Pending',
                    SYSDATETIME()
                );",
                conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@RepresentativeName", request.RepresentativeName.Trim());
            cmd.Parameters.AddWithValue("@RelationshipToClinic", request.RelationshipToClinic.Trim());
            cmd.Parameters.AddWithValue("@ClinicName", request.ClinicName.Trim());
            cmd.Parameters.AddWithValue("@Address", request.Address.Trim());
            cmd.Parameters.AddWithValue("@City", request.City.Trim());
            cmd.Parameters.AddWithValue("@ContactNumber", request.ContactNumber.Trim());
            cmd.Parameters.AddWithValue("@ClinicEmail", request.ClinicEmail.Trim());
            cmd.Parameters.AddWithValue("@ServicesOffered", request.ServicesOffered.Trim());
            cmd.Parameters.AddWithValue("@Message", request.Message.Trim());

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int SubmitClinicClaimRequest(
            int userId,
            ClinicClaimRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var duplicateCmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM ClinicClaimRequests
                WHERE UserId = @UserId
                  AND ClinicId = @ClinicId
                  AND Status = 'Pending';",
                conn);

            duplicateCmd.Parameters.AddWithValue("@UserId", userId);
            duplicateCmd.Parameters.AddWithValue("@ClinicId", request.ClinicId);

            if (Convert.ToInt32(duplicateCmd.ExecuteScalar()) > 0)
            {
                throw new InvalidOperationException(
                    "You already have a pending claim for this clinic.");
            }

            using var cmd = new SqlCommand(@"
                INSERT INTO ClinicClaimRequests
                (
                    UserId,
                    ClinicId,
                    RepresentativeName,
                    RelationshipToClinic,
                    ContactNumber,
                    SupportingDetails,
                    Status,
                    CreatedAt
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @UserId,
                    @ClinicId,
                    @RepresentativeName,
                    @RelationshipToClinic,
                    @ContactNumber,
                    @SupportingDetails,
                    'Pending',
                    SYSDATETIME()
                );",
                conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@ClinicId", request.ClinicId);
            cmd.Parameters.AddWithValue("@RepresentativeName", request.RepresentativeName.Trim());
            cmd.Parameters.AddWithValue("@RelationshipToClinic", request.RelationshipToClinic.Trim());
            cmd.Parameters.AddWithValue("@ContactNumber", request.ContactNumber.Trim());
            cmd.Parameters.AddWithValue("@SupportingDetails", request.SupportingDetails.Trim());

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<ClinicListingRequest> GetClinicListingRequests(
            string? status = null)
        {
            var items = new List<ClinicListingRequest>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    r.Id,
                    r.UserId,
                    r.RepresentativeName,
                    r.RelationshipToClinic,
                    r.ClinicName,
                    r.Address,
                    r.City,
                    r.ContactNumber,
                    r.ClinicEmail,
                    r.ServicesOffered,
                    r.Message,
                    r.Status,
                    r.CreatedClinicId,
                    r.ReviewedByUserId,
                    ISNULL(r.ReviewNote, ''),
                    r.CreatedAt,
                    r.ReviewedAt,
                    ISNULL(u.FullName, '')
                FROM ClinicListingRequests r
                LEFT JOIN UserAccounts u ON u.Id = r.UserId
                WHERE (@Status IS NULL OR r.Status = @Status)
                ORDER BY r.CreatedAt DESC;",
                conn);

            cmd.Parameters.AddWithValue("@Status",
                string.IsNullOrWhiteSpace(status)
                    ? DBNull.Value
                    : status);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                items.Add(new ClinicListingRequest
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    RepresentativeName = reader.GetString(2),
                    RelationshipToClinic = reader.GetString(3),
                    ClinicName = reader.GetString(4),
                    Address = reader.GetString(5),
                    City = reader.GetString(6),
                    ContactNumber = reader.GetString(7),
                    ClinicEmail = reader.GetString(8),
                    ServicesOffered = reader.GetString(9),
                    Message = reader.GetString(10),
                    Status = reader.GetString(11),
                    CreatedClinicId = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                    ReviewedByUserId = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                    ReviewNote = reader.GetString(14),
                    CreatedAt = reader.GetDateTime(15),
                    ReviewedAt = reader.IsDBNull(16) ? null : reader.GetDateTime(16),
                    RequesterName = reader.GetString(17)
                });
            }

            return items;
        }

        public List<ClinicClaimRequest> GetClinicClaimRequests(
            string? status = null)
        {
            var items = new List<ClinicClaimRequest>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    r.Id,
                    r.UserId,
                    r.ClinicId,
                    r.RepresentativeName,
                    r.RelationshipToClinic,
                    r.ContactNumber,
                    r.SupportingDetails,
                    r.Status,
                    r.ReviewedByUserId,
                    ISNULL(r.ReviewNote, ''),
                    r.CreatedAt,
                    r.ReviewedAt,
                    ISNULL(u.FullName, ''),
                    ISNULL(c.ClinicName, '')
                FROM ClinicClaimRequests r
                LEFT JOIN UserAccounts u ON u.Id = r.UserId
                LEFT JOIN ClinicTenants c ON c.Id = r.ClinicId
                WHERE (@Status IS NULL OR r.Status = @Status)
                ORDER BY r.CreatedAt DESC;",
                conn);

            cmd.Parameters.AddWithValue("@Status",
                string.IsNullOrWhiteSpace(status)
                    ? DBNull.Value
                    : status);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                items.Add(new ClinicClaimRequest
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    ClinicId = reader.GetInt32(2),
                    RepresentativeName = reader.GetString(3),
                    RelationshipToClinic = reader.GetString(4),
                    ContactNumber = reader.GetString(5),
                    SupportingDetails = reader.GetString(6),
                    Status = reader.GetString(7),
                    ReviewedByUserId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    ReviewNote = reader.GetString(9),
                    CreatedAt = reader.GetDateTime(10),
                    ReviewedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    RequesterName = reader.GetString(12),
                    ClinicName = reader.GetString(13)
                });
            }

            return items;
        }

        public int ApproveClinicListingRequest(
            int requestId,
            int adminUserId,
            string reviewNote)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                ClinicListingRequest? request = null;

                using (var getCmd = new SqlCommand(@"
                    SELECT
                        Id,
                        UserId,
                        RepresentativeName,
                        RelationshipToClinic,
                        ClinicName,
                        Address,
                        City,
                        ContactNumber,
                        ClinicEmail,
                        ServicesOffered,
                        Message,
                        Status
                    FROM ClinicListingRequests
                    WHERE Id = @Id;",
                    conn,
                    tx))
                {
                    getCmd.Parameters.AddWithValue("@Id", requestId);

                    using var reader = getCmd.ExecuteReader();

                    if (reader.Read())
                    {
                        request = new ClinicListingRequest
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            RepresentativeName = reader.GetString(2),
                            RelationshipToClinic = reader.GetString(3),
                            ClinicName = reader.GetString(4),
                            Address = reader.GetString(5),
                            City = reader.GetString(6),
                            ContactNumber = reader.GetString(7),
                            ClinicEmail = reader.GetString(8),
                            ServicesOffered = reader.GetString(9),
                            Message = reader.GetString(10),
                            Status = reader.GetString(11)
                        };
                    }
                }

                if (request is null ||
                    !string.Equals(
                        request.Status,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "This clinic listing request is no longer pending.");
                }

                int clinicId;

                using (var clinicCmd = new SqlCommand(@"
                    INSERT INTO ClinicTenants
                    (
                        ClinicName,
                        Branch,
                        Address,
                        PrcLicenseNo,
                        PtrNumber,
                        SubscriptionPlan,
                        FacilityType,
                        HasClinic,
                        HasPetShop,
                        ContactPhone,
                        OperatingHours,
                        BrandsCarried,
                        ServiceCapabilities,
                        MessengerUrl,
                        IsVerified,
                        VerificationStatus
                    )
                    OUTPUT INSERTED.Id
                    VALUES
                    (
                        @ClinicName,
                        @Branch,
                        @Address,
                        '',
                        '',
                        '',
                        'Veterinary Clinic',
                        1,
                        0,
                        @ContactPhone,
                        'Contact clinic for hours',
                        '',
                        @Services,
                        '',
                        0,
                        'Unverified'
                    );",
                    conn,
                    tx))
                {
                    clinicCmd.Parameters.AddWithValue("@ClinicName", request.ClinicName);
                    clinicCmd.Parameters.AddWithValue("@Branch", request.City);
                    clinicCmd.Parameters.AddWithValue("@Address", request.Address);
                    clinicCmd.Parameters.AddWithValue("@ContactPhone", request.ContactNumber);
                    clinicCmd.Parameters.AddWithValue("@Services", request.ServicesOffered);

                    clinicId =
                        Convert.ToInt32(clinicCmd.ExecuteScalar());
                }

                using (var reviewCmd = new SqlCommand(@"
                    UPDATE ClinicListingRequests
                    SET Status = 'Approved',
                        CreatedClinicId = @ClinicId,
                        ReviewedByUserId = @AdminUserId,
                        ReviewNote = @ReviewNote,
                        ReviewedAt = SYSDATETIME()
                    WHERE Id = @Id;",
                    conn,
                    tx))
                {
                    reviewCmd.Parameters.AddWithValue("@ClinicId", clinicId);
                    reviewCmd.Parameters.AddWithValue("@AdminUserId", adminUserId);
                    reviewCmd.Parameters.AddWithValue("@ReviewNote", reviewNote ?? string.Empty);
                    reviewCmd.Parameters.AddWithValue("@Id", requestId);
                    reviewCmd.ExecuteNonQuery();
                }

                tx.Commit();

                return clinicId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public bool RejectClinicListingRequest(
            int requestId,
            int adminUserId,
            string reviewNote)
        {
            return ReviewSimple(
                "ClinicListingRequests",
                requestId,
                adminUserId,
                "Rejected",
                reviewNote);
        }

        public bool ApproveClinicClaimRequest(
            int requestId,
            int adminUserId,
            string reviewNote)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                int userId;
                int clinicId;
                string status;

                using (var getCmd = new SqlCommand(@"
                    SELECT UserId, ClinicId, Status
                    FROM ClinicClaimRequests
                    WHERE Id = @Id;",
                    conn,
                    tx))
                {
                    getCmd.Parameters.AddWithValue("@Id", requestId);

                    using var reader = getCmd.ExecuteReader();

                    if (!reader.Read())
                        return false;

                    userId = reader.GetInt32(0);
                    clinicId = reader.GetInt32(1);
                    status = reader.GetString(2);
                }

                if (!string.Equals(
                        status,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                using (var userCmd = new SqlCommand(@"
                    UPDATE UserAccounts
                    SET ClinicId = @ClinicId,
                        Role = 'Clinic Owner'
                    WHERE Id = @UserId;",
                    conn,
                    tx))
                {
                    userCmd.Parameters.AddWithValue("@ClinicId", clinicId);
                    userCmd.Parameters.AddWithValue("@UserId", userId);

                    if (userCmd.ExecuteNonQuery() == 0)
                        return false;
                }

                using (var reviewCmd = new SqlCommand(@"
                    UPDATE ClinicClaimRequests
                    SET Status = 'Approved',
                        ReviewedByUserId = @AdminUserId,
                        ReviewNote = @ReviewNote,
                        ReviewedAt = SYSDATETIME()
                    WHERE Id = @Id;",
                    conn,
                    tx))
                {
                    reviewCmd.Parameters.AddWithValue("@AdminUserId", adminUserId);
                    reviewCmd.Parameters.AddWithValue("@ReviewNote", reviewNote ?? string.Empty);
                    reviewCmd.Parameters.AddWithValue("@Id", requestId);
                    reviewCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public bool RejectClinicClaimRequest(
            int requestId,
            int adminUserId,
            string reviewNote)
        {
            return ReviewSimple(
                "ClinicClaimRequests",
                requestId,
                adminUserId,
                "Rejected",
                reviewNote);
        }

        private bool ReviewSimple(
            string tableName,
            int requestId,
            int adminUserId,
            string status,
            string reviewNote)
        {
            if (tableName != "ClinicListingRequests" &&
                tableName != "ClinicClaimRequests")
            {
                throw new InvalidOperationException(
                    "Unsupported request table.");
            }

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var sql = $@"
                UPDATE {tableName}
                SET Status = @Status,
                    ReviewedByUserId = @AdminUserId,
                    ReviewNote = @ReviewNote,
                    ReviewedAt = SYSDATETIME()
                WHERE Id = @Id
                  AND Status = 'Pending';";

            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@AdminUserId", adminUserId);
            cmd.Parameters.AddWithValue("@ReviewNote", reviewNote ?? string.Empty);
            cmd.Parameters.AddWithValue("@Id", requestId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeactivateSubscription(int subscriptionId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE Subscriptions
                SET Status = 'Suspended'
                WHERE Id = @Id
                  AND Status = 'Active';",
                conn);

            cmd.Parameters.AddWithValue("@Id", subscriptionId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public int GetPendingClinicRequestCount()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    (SELECT COUNT(*) FROM ClinicListingRequests WHERE Status = 'Pending')
                  + (SELECT COUNT(*) FROM ClinicClaimRequests WHERE Status = 'Pending');",
                conn);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static SubscriptionRecord MapSubscription(
            SqlDataReader reader)
        {
            return new SubscriptionRecord
            {
                Id = reader.GetInt32(0),
                UserId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                ClinicId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                SubscriptionType = reader.GetString(3),
                Status = reader.GetString(4),
                Reference = reader.GetString(5),
                StartsAt = reader.GetDateTime(6),
                ExpiresAt = reader.GetDateTime(7),
                CreatedAt = reader.GetDateTime(8),
                UserName = reader.GetString(9),
                ClinicName = reader.GetString(10)
            };
        }
    }
}
