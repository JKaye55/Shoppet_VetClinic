using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ShoppetDb")
                ?? throw new InvalidOperationException(
                    "Connection string 'ShoppetDb' was not found.");
        }

        private readonly List<CartItem> _cart = new();


        // =========================================================
        // CLINICS
        // =========================================================

        public List<ClinicTenant> GetAllClinics()
        {
            var clinics = new List<ClinicTenant>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using var cmd = new SqlCommand(@"
                    SELECT
                        Id,
                        ClinicName,
                        Branch,
                        Address,
                        PrcLicenseNo,
                        PtrNumber,
                        SubscriptionPlan,
                        FacilityType,
                        ISNULL(HasClinic, 1),
                        ISNULL(HasPetShop, 1),
                        ISNULL(ContactPhone, ''),
                        ISNULL(OperatingHours, '8:00 AM - 6:00 PM'),
                        ISNULL(BrandsCarried, ''),
                        ISNULL(ServiceCapabilities, ''),
                        ISNULL(MessengerUrl, ''),
                        ISNULL(IsVerified, 0),
                        VerifiedAt,
                        VerificationFee,
                        VerificationReference,
                        ISNULL(VerificationStatus, 'Unverified')
                    FROM ClinicTenants
                    ORDER BY ClinicName", conn);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    clinics.Add(new ClinicTenant
                    {
                        Id = reader.GetInt32(0),

                        ClinicName = reader.IsDBNull(1)
                            ? string.Empty
                            : reader.GetString(1),

                        Branch = reader.IsDBNull(2)
                            ? string.Empty
                            : reader.GetString(2),

                        Address = reader.IsDBNull(3)
                            ? string.Empty
                            : reader.GetString(3),

                        PrcLicenseNo = reader.IsDBNull(4)
                            ? string.Empty
                            : reader.GetString(4),

                        PtrNumber = reader.IsDBNull(5)
                            ? string.Empty
                            : reader.GetString(5),

                        SubscriptionPlan = reader.IsDBNull(6)
                            ? string.Empty
                            : reader.GetString(6),

                        FacilityType = reader.IsDBNull(7)
                            ? string.Empty
                            : reader.GetString(7),

                        HasClinic = !reader.IsDBNull(8)
                            && reader.GetBoolean(8),

                        HasPetShop = !reader.IsDBNull(9)
                            && reader.GetBoolean(9),

                        ContactPhone = reader.IsDBNull(10)
                            ? string.Empty
                            : reader.GetString(10),

                        OperatingHours = reader.IsDBNull(11)
                            ? string.Empty
                            : reader.GetString(11),

                        BrandsCarried = reader.IsDBNull(12)
                            ? string.Empty
                            : reader.GetString(12),

                        ServiceCapabilities = reader.IsDBNull(13)
                            ? string.Empty
                            : reader.GetString(13),

                        MessengerUrl = reader.IsDBNull(14)
                            ? string.Empty
                            : reader.GetString(14),

                        // ---- Verification fields ----
                        IsVerified = !reader.IsDBNull(15)
                            && reader.GetBoolean(15),

                        VerifiedAt = reader.IsDBNull(16)
                            ? null
                            : reader.GetDateTime(16),

                        VerificationFee = reader.IsDBNull(17)
                            ? null
                            : reader.GetDecimal(17),

                        VerificationReference = reader.IsDBNull(18)
                            ? null
                            : reader.GetString(18),

                        VerificationStatus = reader.IsDBNull(19)
                            ? "Unverified"
                            : reader.GetString(19)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[DatabaseService Error] GetAllClinics failed: {ex}");
            }

            return clinics;
        }


        public ClinicTenant? GetClinicById(int clinicId)
        {
            return GetAllClinics()
                .FirstOrDefault(c => c.Id == clinicId);
        }


        public ClinicTenant? GetClinicForUser(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT ClinicId FROM UserAccounts WHERE Id=@UserId",
                conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            var value = cmd.ExecuteScalar();

            if (value is null || value == DBNull.Value)
                return null;

            return GetClinicById(Convert.ToInt32(value));
        }


        public int CreateClinic(ClinicTenant clinic)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                INSERT INTO ClinicTenants
                (
                    ClinicName, Branch, Address,
                    PrcLicenseNo, PtrNumber,
                    SubscriptionPlan, FacilityType,
                    HasClinic, HasPetShop,
                    ContactPhone, OperatingHours,
                    BrandsCarried, ServiceCapabilities,
                    MessengerUrl
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @ClinicName, @Branch, @Address,
                    @PrcLicenseNo, @PtrNumber,
                    @SubscriptionPlan, @FacilityType,
                    @HasClinic, @HasPetShop,
                    @ContactPhone, @OperatingHours,
                    @BrandsCarried, @ServiceCapabilities,
                    @MessengerUrl
                )", conn);

            AddClinicParameters(cmd, clinic);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        public bool UpdateClinic(ClinicTenant clinic)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE ClinicTenants
                SET
                    ClinicName=@ClinicName,
                    Branch=@Branch,
                    Address=@Address,
                    PrcLicenseNo=@PrcLicenseNo,
                    PtrNumber=@PtrNumber,
                    SubscriptionPlan=@SubscriptionPlan,
                    FacilityType=@FacilityType,
                    HasClinic=@HasClinic,
                    HasPetShop=@HasPetShop,
                    ContactPhone=@ContactPhone,
                    OperatingHours=@OperatingHours,
                    BrandsCarried=@BrandsCarried,
                    ServiceCapabilities=@ServiceCapabilities,
                    MessengerUrl=@MessengerUrl
                WHERE Id=@Id", conn);

            cmd.Parameters.AddWithValue("@Id", clinic.Id);

            AddClinicParameters(cmd, clinic);

            return cmd.ExecuteNonQuery() > 0;
        }


        public bool DeleteClinic(int clinicId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var dependencyCmd = new SqlCommand(@"
                SELECT
                    (SELECT COUNT(*) FROM UserAccounts        WHERE ClinicId=@ClinicId)
                  + (SELECT COUNT(*) FROM PetProducts         WHERE ClinicId=@ClinicId)
                  + (SELECT COUNT(*) FROM ClinicAppointments  WHERE ClinicId=@ClinicId)
                  + (SELECT COUNT(*) FROM Inquiries           WHERE ClinicId=@ClinicId)
                  + (SELECT COUNT(*) FROM Orders              WHERE ClinicId=@ClinicId)",
                conn);

            dependencyCmd.Parameters.AddWithValue("@ClinicId", clinicId);

            if (Convert.ToInt32(dependencyCmd.ExecuteScalar()) > 0)
                return false;

            using var cmd = new SqlCommand(
                "DELETE FROM ClinicTenants WHERE Id=@Id", conn);

            cmd.Parameters.AddWithValue("@Id", clinicId);

            return cmd.ExecuteNonQuery() > 0;
        }


        private static void AddClinicParameters(SqlCommand cmd, ClinicTenant clinic)
        {
            cmd.Parameters.AddWithValue("@ClinicName", clinic.ClinicName ?? "");
            cmd.Parameters.AddWithValue("@Branch", clinic.Branch ?? "");
            cmd.Parameters.AddWithValue("@Address", clinic.Address ?? "");
            cmd.Parameters.AddWithValue("@PrcLicenseNo", clinic.PrcLicenseNo ?? "");
            cmd.Parameters.AddWithValue("@PtrNumber", clinic.PtrNumber ?? "");
            cmd.Parameters.AddWithValue("@SubscriptionPlan", clinic.SubscriptionPlan ?? "");
            cmd.Parameters.AddWithValue("@FacilityType", clinic.FacilityType ?? "");
            cmd.Parameters.AddWithValue("@HasClinic", clinic.HasClinic);
            cmd.Parameters.AddWithValue("@HasPetShop", clinic.HasPetShop);
            cmd.Parameters.AddWithValue("@ContactPhone", clinic.ContactPhone ?? "");
            cmd.Parameters.AddWithValue("@OperatingHours", clinic.OperatingHours ?? "");
            cmd.Parameters.AddWithValue("@BrandsCarried", clinic.BrandsCarried ?? "");
            cmd.Parameters.AddWithValue("@ServiceCapabilities", clinic.ServiceCapabilities ?? "");
            cmd.Parameters.AddWithValue("@MessengerUrl", clinic.MessengerUrl ?? "");
        }


        // =========================================================
        // BUSINESS VERIFICATION (₱150–₱300 one-time)
        // =========================================================

        public bool SubmitVerificationRequest(int clinicId, decimal fee)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE ClinicTenants
                SET VerificationStatus = 'Pending',
                    VerificationFee = @Fee
                WHERE Id = @ClinicId
                  AND VerificationStatus IN ('Unverified', 'Rejected')",
                conn);

            cmd.Parameters.AddWithValue("@ClinicId", clinicId);
            cmd.Parameters.AddWithValue("@Fee", fee);

            return cmd.ExecuteNonQuery() > 0;
        }


        public bool ApproveVerification(int clinicId, string reference)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                decimal fee;

                using (var read = new SqlCommand(
                    "SELECT VerificationFee FROM ClinicTenants WHERE Id=@Id",
                    conn, tx))
                {
                    read.Parameters.AddWithValue("@Id", clinicId);
                    var v = read.ExecuteScalar();
                    fee = v == null || v == DBNull.Value
                        ? 0m
                        : Convert.ToDecimal(v);
                }

                using (var cmd = new SqlCommand(@"
                    UPDATE ClinicTenants
                    SET IsVerified = 1,
                        VerifiedAt = SYSDATETIME(),
                        VerificationStatus = 'Verified',
                        VerificationReference = @Ref
                    WHERE Id = @ClinicId
                      AND VerificationStatus = 'Pending'",
                    conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ClinicId", clinicId);
                    cmd.Parameters.AddWithValue("@Ref", reference);

                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        tx.Rollback();
                        return false;
                    }
                }

                using (var log = new SqlCommand(@"
                    INSERT INTO Transactions
                        (ClinicId, Type, Amount, Reference)
                    VALUES
                        (@ClinicId, 'BusinessVerification', @Amount, @Ref)",
                    conn, tx))
                {
                    log.Parameters.AddWithValue("@ClinicId", clinicId);
                    log.Parameters.AddWithValue("@Amount", fee);
                    log.Parameters.AddWithValue("@Ref", reference);
                    log.ExecuteNonQuery();
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


        public bool RejectVerification(int clinicId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE ClinicTenants
                SET VerificationStatus = 'Rejected'
                WHERE Id = @ClinicId
                  AND VerificationStatus = 'Pending'",
                conn);

            cmd.Parameters.AddWithValue("@ClinicId", clinicId);

            return cmd.ExecuteNonQuery() > 0;
        }


        public List<ClinicTenant> GetPendingVerifications()
        {
            return GetAllClinics()
                .Where(c => c.VerificationStatus == "Pending")
                .ToList();
        }


        // =========================================================
        // CLINIC STAFF
        // =========================================================

        public bool AssignUserToClinic(int userId, int clinicId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var clinicCheck = new SqlCommand(
                "SELECT COUNT(1) FROM ClinicTenants WHERE Id=@ClinicId",
                conn);

            clinicCheck.Parameters.AddWithValue("@ClinicId", clinicId);

            if (Convert.ToInt32(clinicCheck.ExecuteScalar()) == 0)
                return false;

            using var cmd = new SqlCommand(@"
                UPDATE UserAccounts
                SET ClinicId=@ClinicId
                WHERE Id=@UserId
                  AND Role='Clinic Staff'", conn);

            cmd.Parameters.AddWithValue("@ClinicId", clinicId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            return cmd.ExecuteNonQuery() > 0;
        }


        public bool UnassignUserFromClinic(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE UserAccounts
                SET ClinicId=NULL
                WHERE Id=@UserId
                  AND Role='Clinic Staff'", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            return cmd.ExecuteNonQuery() > 0;
        }


        public List<UserAccount> GetClinicStaffUsers()
        {
            var users = new List<UserAccount>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    Id, FullName, Email, PasswordHash, Role,
                    ClinicId, CreatedAt,
                    ISNULL(IsPremium, 0),
                    PremiumActivatedAt,
                    PremiumReference,
                    ApiToken,
                    ApiTokenExpiresAt
                FROM UserAccounts
                WHERE Role='Clinic Staff'
                ORDER BY FullName",
                conn);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }


        // =========================================================
        // USERS
        // =========================================================

        public List<UserAccount> GetAllUsers()
        {
            var users = new List<UserAccount>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using var cmd = new SqlCommand(@"
                    SELECT
                        Id, FullName, Email, PasswordHash, Role,
                        ClinicId, CreatedAt,
                        ISNULL(IsPremium, 0),
                        PremiumActivatedAt,
                        PremiumReference,
                        ApiToken,
                        ApiTokenExpiresAt
                    FROM UserAccounts",
                    conn);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(MapUser(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[DatabaseService Error] GetAllUsers failed: {ex}");
            }

            return users;
        }


        private static UserAccount MapUser(SqlDataReader reader)
        {
            return new UserAccount
            {
                Id = reader.GetInt32(0),

                FullName = reader.IsDBNull(1)
                    ? string.Empty
                    : reader.GetString(1),

                Email = reader.IsDBNull(2)
                    ? string.Empty
                    : reader.GetString(2),

                Password = reader.IsDBNull(3)
                    ? string.Empty
                    : reader.GetString(3),

                Role = reader.IsDBNull(4)
                    ? string.Empty
                    : reader.GetString(4),

                ClinicId = reader.IsDBNull(5)
                    ? null
                    : reader.GetInt32(5),

                CreatedAt = reader.GetDateTime(6),

                IsPremium = !reader.IsDBNull(7)
                    && reader.GetBoolean(7),

                PremiumActivatedAt = reader.IsDBNull(8)
                    ? null
                    : reader.GetDateTime(8),

                PremiumReference = reader.IsDBNull(9)
                    ? null
                    : reader.GetString(9),

                ApiToken = reader.IsDBNull(10)
                    ? null
                    : reader.GetString(10),

                ApiTokenExpiresAt = reader.IsDBNull(11)
                    ? null
                    : reader.GetDateTime(11)
            };
        }


        public UserAccount? RegisterUser(
            string fullName,
            string email,
            string password,
            string role)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var checkCmd = new SqlCommand(
                "SELECT COUNT(1) FROM UserAccounts WHERE Email=@Email",
                conn);

            checkCmd.Parameters.AddWithValue("@Email", email);

            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                return null;

            using var insertCmd = new SqlCommand(@"
                INSERT INTO UserAccounts
                    (FullName, Email, PasswordHash, Role)
                OUTPUT
                    INSERTED.Id,
                    INSERTED.CreatedAt
                VALUES
                    (@FullName, @Email, @Password, @Role)",
                conn);

            insertCmd.Parameters.AddWithValue("@FullName", fullName);
            insertCmd.Parameters.AddWithValue("@Email", email);
            insertCmd.Parameters.AddWithValue("@Password", password);
            insertCmd.Parameters.AddWithValue("@Role", role);

            using var reader = insertCmd.ExecuteReader();

            if (reader.Read())
            {
                return new UserAccount
                {
                    Id = reader.GetInt32(0),
                    FullName = fullName,
                    Email = email,
                    Password = password,
                    Role = role,
                    CreatedAt = reader.GetDateTime(1)
                };
            }

            return null;
        }


        public UserAccount? AuthenticateUser(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    Id, FullName, Email, PasswordHash, Role,
                    ClinicId, CreatedAt,
                    ISNULL(IsPremium, 0),
                    PremiumActivatedAt,
                    PremiumReference,
                    ApiToken,
                    ApiTokenExpiresAt
                FROM UserAccounts
                WHERE Email=@Email
                  AND PasswordHash=@Password",
                conn);

            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
                return MapUser(reader);

            return null;
        }


        public UserAccount? GetUserById(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    Id, FullName, Email, PasswordHash, Role,
                    ClinicId, CreatedAt,
                    ISNULL(IsPremium, 0),
                    PremiumActivatedAt,
                    PremiumReference,
                    ApiToken,
                    ApiTokenExpiresAt
                FROM UserAccounts
                WHERE Id=@Id",
                conn);

            cmd.Parameters.AddWithValue("@Id", userId);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
                return MapUser(reader);

            return null;
        }


        // =========================================================
        // PREMIUM (Pet ID Virtual Card Premium — ₱49 one-time)
        // =========================================================

        public bool IsUserPremium(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT ISNULL(IsPremium, 0) FROM UserAccounts WHERE Id=@Id",
                conn);

            cmd.Parameters.AddWithValue("@Id", userId);

            var v = cmd.ExecuteScalar();

            return v != null && Convert.ToBoolean(v);
        }


        public bool ActivatePremiumUpgrade(
            int userId,
            string reference,
            decimal amount = 49m)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                using (var cmd = new SqlCommand(@"
                    UPDATE UserAccounts
                    SET IsPremium = 1,
                        PremiumActivatedAt = SYSDATETIME(),
                        PremiumReference = @Ref
                    WHERE Id = @UserId
                      AND Role = 'Pet Owner'
                      AND ISNULL(IsPremium, 0) = 0",
                    conn, tx))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Ref", reference);

                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        tx.Rollback();
                        return false;
                    }
                }

                using (var log = new SqlCommand(@"
                    INSERT INTO Transactions
                        (UserId, Type, Amount, Reference)
                    VALUES
                        (@UserId, 'PremiumUpgrade', @Amount, @Ref)",
                    conn, tx))
                {
                    log.Parameters.AddWithValue("@UserId", userId);
                    log.Parameters.AddWithValue("@Amount", amount);
                    log.Parameters.AddWithValue("@Ref", reference);
                    log.ExecuteNonQuery();
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


        // =========================================================
        // PET PROFILES
        // =========================================================

        public List<PetProfile> GetPetsByUser(int userId)
        {
            var pets = new List<PetProfile>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    Id, UserId, PetName, Breed, Species,
                    Age, WeightKg, Diet, CreatedAt,
                    CardId, CardIssuedAt, CardTheme
                FROM PetProfiles
                WHERE UserId=@UserId
                ORDER BY PetName",
                conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                pets.Add(MapPet(reader));
            }

            return pets;
        }


        public PetProfile? GetPetById(int petId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    Id, UserId, PetName, Breed, Species,
                    Age, WeightKg, Diet, CreatedAt,
                    CardId, CardIssuedAt, CardTheme
                FROM PetProfiles
                WHERE Id=@Id",
                conn);

            cmd.Parameters.AddWithValue("@Id", petId);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
                return MapPet(reader);

            return null;
        }


        public PetProfile? GetPetByCardId(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return null;

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    Id, UserId, PetName, Breed, Species,
                    Age, WeightKg, Diet, CreatedAt,
                    CardId, CardIssuedAt, CardTheme
                FROM PetProfiles
                WHERE CardId=@CardId",
                conn);

            cmd.Parameters.AddWithValue("@CardId", cardId.Trim().ToUpperInvariant());

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
                return MapPet(reader);

            return null;
        }


        private static PetProfile MapPet(SqlDataReader reader)
        {
            return new PetProfile
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),

                PetName = reader.IsDBNull(2)
                    ? string.Empty
                    : reader.GetString(2),

                Breed = reader.IsDBNull(3)
                    ? string.Empty
                    : reader.GetString(3),

                Species = reader.IsDBNull(4)
                    ? string.Empty
                    : reader.GetString(4),

                Age = reader.IsDBNull(5)
                    ? string.Empty
                    : reader.GetString(5),

                WeightKg = reader.IsDBNull(6)
                    ? null
                    : reader.GetDecimal(6),

                Diet = reader.IsDBNull(7)
                    ? string.Empty
                    : reader.GetString(7),

                CreatedAt = reader.GetDateTime(8),

                CardId = reader.IsDBNull(9)
                    ? string.Empty
                    : reader.GetString(9),

                CardIssuedAt = reader.IsDBNull(10)
                    ? null
                    : reader.GetDateTime(10),

                CardTheme = reader.IsDBNull(11)
                    ? null
                    : reader.GetString(11)
            };
        }


        public int AddPet(PetProfile pet)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // ---- Enforce free-tier limit: 1 pet unless Premium ----
            using (var check = new SqlCommand(@"
                SELECT
                    ISNULL(u.IsPremium, 0),
                    (SELECT COUNT(*) FROM PetProfiles WHERE UserId=u.Id)
                FROM UserAccounts u
                WHERE u.Id = @UserId", conn))
            {
                check.Parameters.AddWithValue("@UserId", pet.UserId);

                using var r = check.ExecuteReader();
                if (r.Read())
                {
                    bool premium = r.GetBoolean(0);
                    int count = r.GetInt32(1);
                    r.Close();

                    if (!premium && count >= 2)
                        throw new InvalidOperationException(
                            "Free accounts support 1 pet. Upgrade to the " +
                            "Pet ID Virtual Card Premium (₱49) to add more.");
                }
            }

            // ---- Generate CardId ----
            var cardId = string.IsNullOrWhiteSpace(pet.CardId)
                ? "PET-" + Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 6)
                    .ToUpperInvariant()
                : pet.CardId;

            using var cmd = new SqlCommand(@"
                INSERT INTO PetProfiles
                    (UserId, PetName, Breed, Species, Age, WeightKg, Diet,
                     CardId, CardIssuedAt, CardTheme)
                OUTPUT INSERTED.Id
                VALUES
                    (@UserId, @PetName, @Breed, @Species, @Age, @WeightKg, @Diet,
                     @CardId, SYSDATETIME(), @CardTheme)",
                conn);

            cmd.Parameters.AddWithValue("@UserId", pet.UserId);
            cmd.Parameters.AddWithValue("@PetName", pet.PetName ?? "");
            cmd.Parameters.AddWithValue("@Breed", pet.Breed ?? "");
            cmd.Parameters.AddWithValue("@Species", pet.Species ?? "Dog");
            cmd.Parameters.AddWithValue("@Age",
                string.IsNullOrWhiteSpace(pet.Age) ? DBNull.Value : pet.Age);
            cmd.Parameters.AddWithValue("@WeightKg",
                pet.WeightKg.HasValue ? pet.WeightKg.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Diet",
                string.IsNullOrWhiteSpace(pet.Diet) ? DBNull.Value : pet.Diet);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@CardTheme",
                string.IsNullOrWhiteSpace(pet.CardTheme)
                    ? (object)DBNull.Value
                    : pet.CardTheme);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        public bool UpdatePet(PetProfile pet)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE PetProfiles
                SET
                    PetName=@PetName,
                    Breed=@Breed,
                    Species=@Species,
                    Age=@Age,
                    WeightKg=@WeightKg,
                    Diet=@Diet,
                    CardTheme=@CardTheme
                WHERE Id=@Id
                  AND UserId=@UserId",
                conn);

            cmd.Parameters.AddWithValue("@Id", pet.Id);
            cmd.Parameters.AddWithValue("@UserId", pet.UserId);
            cmd.Parameters.AddWithValue("@PetName", pet.PetName ?? "");
            cmd.Parameters.AddWithValue("@Breed", pet.Breed ?? "");
            cmd.Parameters.AddWithValue("@Species", pet.Species ?? "Dog");
            cmd.Parameters.AddWithValue("@Age",
                string.IsNullOrWhiteSpace(pet.Age) ? DBNull.Value : pet.Age);
            cmd.Parameters.AddWithValue("@WeightKg",
                pet.WeightKg.HasValue ? pet.WeightKg.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Diet",
                string.IsNullOrWhiteSpace(pet.Diet) ? DBNull.Value : pet.Diet);
            cmd.Parameters.AddWithValue("@CardTheme",
                string.IsNullOrWhiteSpace(pet.CardTheme)
                    ? (object)DBNull.Value
                    : pet.CardTheme);

            return cmd.ExecuteNonQuery() > 0;
        }


        public bool SetCardTheme(int petId, int userId, string? theme)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE PetProfiles
                SET CardTheme=@Theme
                WHERE Id=@PetId
                  AND UserId=@UserId",
                conn);

            cmd.Parameters.AddWithValue("@PetId", petId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Theme",
                string.IsNullOrWhiteSpace(theme)
                    ? (object)DBNull.Value
                    : theme);

            return cmd.ExecuteNonQuery() > 0;
        }


        public bool DeletePet(int petId, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                DELETE FROM PetProfiles
                WHERE Id=@Id
                  AND UserId=@UserId",
                conn);

            cmd.Parameters.AddWithValue("@Id", petId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            return cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // PET HEALTH RECORDS
        // =========================================================

        public List<PetHealthRecord> GetHealthRecords(int petId)
        {
            var list = new List<PetHealthRecord>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT
                    Id, PetId, ClinicId, RecordType, Title, Notes,
                    RecordDate, NextDueDate, VetName, CreatedAt
                FROM PetHealthRecords
                WHERE PetId=@PetId
                ORDER BY RecordDate DESC",
                conn);

            cmd.Parameters.AddWithValue("@PetId", petId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new PetHealthRecord
                {
                    Id = reader.GetInt32(0),
                    PetId = reader.GetInt32(1),

                    ClinicId = reader.IsDBNull(2)
                        ? null
                        : reader.GetInt32(2),

                    RecordType = reader.IsDBNull(3)
                        ? "Checkup"
                        : reader.GetString(3),

                    Title = reader.IsDBNull(4)
                        ? string.Empty
                        : reader.GetString(4),

                    Notes = reader.IsDBNull(5)
                        ? string.Empty
                        : reader.GetString(5),

                    RecordDate = reader.GetDateTime(6),

                    NextDueDate = reader.IsDBNull(7)
                        ? null
                        : reader.GetDateTime(7),

                    VetName = reader.IsDBNull(8)
                        ? string.Empty
                        : reader.GetString(8),

                    CreatedAt = reader.GetDateTime(9)
                });
            }

            return list;
        }


        public int AddHealthRecord(PetHealthRecord rec)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Free tier: max 5 records per pet. Premium: unlimited.
            using (var check = new SqlCommand(@"
                SELECT
                    ISNULL(u.IsPremium, 0),
                    (SELECT COUNT(*) FROM PetHealthRecords WHERE PetId=@PetId)
                FROM PetProfiles p
                JOIN UserAccounts u ON u.Id = p.UserId
                WHERE p.Id = @PetId", conn))
            {
                check.Parameters.AddWithValue("@PetId", rec.PetId);

                using var r = check.ExecuteReader();
                if (r.Read())
                {
                    bool premium = r.GetBoolean(0);
                    int count = r.GetInt32(1);
                    r.Close();

                    if (!premium && count >= 5)
                        throw new InvalidOperationException(
                            "Free accounts store up to 5 health records per pet. " +
                            "Upgrade to the Pet ID Virtual Card Premium (₱49) " +
                            "for unlimited history.");
                }
            }

            using var cmd = new SqlCommand(@"
                INSERT INTO PetHealthRecords
                    (PetId, ClinicId, RecordType, Title, Notes,
                     RecordDate, NextDueDate, VetName)
                OUTPUT INSERTED.Id
                VALUES
                    (@PetId, @ClinicId, @RecordType, @Title, @Notes,
                     @RecordDate, @NextDueDate, @VetName)",
                conn);

            cmd.Parameters.AddWithValue("@PetId", rec.PetId);
            cmd.Parameters.AddWithValue("@ClinicId",
                rec.ClinicId.HasValue ? rec.ClinicId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@RecordType", rec.RecordType ?? "Checkup");
            cmd.Parameters.AddWithValue("@Title", rec.Title ?? "");
            cmd.Parameters.AddWithValue("@Notes", rec.Notes ?? "");
            cmd.Parameters.AddWithValue("@RecordDate", rec.RecordDate);
            cmd.Parameters.AddWithValue("@NextDueDate",
                rec.NextDueDate.HasValue ? rec.NextDueDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@VetName", rec.VetName ?? "");

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        public bool DeleteHealthRecord(int recordId, int petId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "DELETE FROM PetHealthRecords WHERE Id=@Id AND PetId=@PetId",
                conn);

            cmd.Parameters.AddWithValue("@Id", recordId);
            cmd.Parameters.AddWithValue("@PetId", petId);

            return cmd.ExecuteNonQuery() > 0;
        }


        public byte[] ExportHealthRecordsCsv(int petId, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Premium-only export
            using (var check = new SqlCommand(@"
                SELECT ISNULL(u.IsPremium, 0)
                FROM PetProfiles p
                JOIN UserAccounts u ON u.Id = p.UserId
                WHERE p.Id = @PetId AND p.UserId = @UserId", conn))
            {
                check.Parameters.AddWithValue("@PetId", petId);
                check.Parameters.AddWithValue("@UserId", userId);

                var v = check.ExecuteScalar();

                if (v == null || !Convert.ToBoolean(v))
                    throw new InvalidOperationException(
                        "Exporting health records requires the " +
                        "Pet ID Virtual Card Premium (₱49).");
            }

            var records = GetHealthRecords(petId);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Type,Title,Vet,NextDue,Notes");

            foreach (var r in records)
            {
                sb.AppendLine(
                    $"{r.RecordDate:yyyy-MM-dd}," +
                    $"{r.RecordType}," +
                    $"\"{r.Title.Replace("\"", "'")}\"," +
                    $"\"{r.VetName.Replace("\"", "'")}\"," +
                    $"{(r.NextDueDate?.ToString("yyyy-MM-dd") ?? "")}," +
                    $"\"{r.Notes.Replace("\"", "'")}\"");
            }

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }

        // =========================================================
        // TRANSACTIONS (revenue audit)
        // =========================================================

        public List<Transaction> GetAllTransactions()
        {
            var list = new List<Transaction>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT Id, UserId, ClinicId, Type, Amount, Reference, PaidAt
                FROM Transactions
                ORDER BY PaidAt DESC",
                conn);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new Transaction
                {
                    Id = reader.GetInt32(0),

                    UserId = reader.IsDBNull(1)
                        ? null
                        : reader.GetInt32(1),

                    ClinicId = reader.IsDBNull(2)
                        ? null
                        : reader.GetInt32(2),

                    Type = reader.IsDBNull(3)
                        ? string.Empty
                        : reader.GetString(3),

                    Amount = reader.GetDecimal(4),

                    Reference = reader.IsDBNull(5)
                        ? string.Empty
                        : reader.GetString(5),

                    PaidAt = reader.GetDateTime(6)
                });
            }

            return list;
        }


        public decimal GetTotalRevenue()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT ISNULL(SUM(Amount), 0) FROM Transactions", conn);

            var v = cmd.ExecuteScalar();

            return v == null || v == DBNull.Value
                ? 0m
                : Convert.ToDecimal(v);
        }


        // =========================================================
        // PRODUCTS
        // =========================================================

        public List<PetProduct> GetAllProducts()
        {
            var products = new List<PetProduct>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using var cmd = new SqlCommand(@"
                    SELECT
                        Id, ClinicId, Name, Category, Price, Stock,
                        PetSize, Description, ImageSource
                    FROM PetProducts",
                    conn);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    products.Add(new PetProduct
                    {
                        Id = reader.GetInt32(0),

                        ClinicId = reader.IsDBNull(1)
                            ? null
                            : reader.GetInt32(1),

                        Name = reader.GetString(2),
                        Category = reader.GetString(3),
                        Price = reader.GetDecimal(4),
                        Stock = reader.GetInt32(5),
                        PetSize = reader.GetString(6),

                        Description = reader.IsDBNull(7)
                            ? string.Empty
                            : reader.GetString(7),

                        ImageSource = reader.IsDBNull(8)
                            ? string.Empty
                            : reader.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[DatabaseService Error] GetAllProducts failed: {ex}");
            }

            return products;
        }


        public List<PetProduct> GetProductsByClinic(int clinicId)
        {
            return GetAllProducts()
                .Where(p => p.ClinicId == clinicId)
                .ToList();
        }


        public void AddProduct(PetProduct product)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                INSERT INTO PetProducts
                    (ClinicId, Name, Category, Price, Stock, PetSize,
                     Description, ImageSource)
                VALUES
                    (@ClinicId, @Name, @Category, @Price, @Stock, @PetSize,
                     @Description, @ImageSource)",
                conn);

            cmd.Parameters.AddWithValue("@ClinicId",
                product.ClinicId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@Stock", product.Stock);
            cmd.Parameters.AddWithValue("@PetSize", product.PetSize);
            cmd.Parameters.AddWithValue("@Description",
                product.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ImageSource",
                product.ImageSource ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }


        public void UpdateProduct(PetProduct product)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE PetProducts
                SET
                    Name=@Name,
                    Category=@Category,
                    Price=@Price,
                    Stock=@Stock,
                    PetSize=@PetSize,
                    Description=@Description
                WHERE Id=@Id",
                conn);

            cmd.Parameters.AddWithValue("@Id", product.Id);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@Stock", product.Stock);
            cmd.Parameters.AddWithValue("@PetSize", product.PetSize);
            cmd.Parameters.AddWithValue("@Description",
                product.Description ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }


        public void DeleteProduct(int productId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "DELETE FROM PetProducts WHERE Id=@Id", conn);

            cmd.Parameters.AddWithValue("@Id", productId);

            cmd.ExecuteNonQuery();
        }


        // =========================================================
        // INQUIRIES
        // =========================================================

        public int CreateInquiry(Inquiry inquiry)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                INSERT INTO Inquiries
                    (UserId, ClinicId, OwnerName, PetName,
                     ServiceType, Notes, Status)
                OUTPUT INSERTED.Id
                VALUES
                    (@UserId, @ClinicId, @OwnerName, @PetName,
                     @ServiceType, @Notes, 'Submitted')",
                conn);

            cmd.Parameters.AddWithValue("@UserId", inquiry.UserId);
            cmd.Parameters.AddWithValue("@ClinicId", inquiry.ClinicId);
            cmd.Parameters.AddWithValue("@OwnerName", inquiry.OwnerName ?? "");
            cmd.Parameters.AddWithValue("@PetName", inquiry.PetName ?? "");
            cmd.Parameters.AddWithValue("@ServiceType", inquiry.ServiceType ?? "");
            cmd.Parameters.AddWithValue("@Notes", inquiry.Notes ?? "");

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        public int GetInquiryCountByUser(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Inquiries WHERE UserId=@UserId", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        // =========================================================
        // APPOINTMENTS
        // =========================================================

        public void BookAppointment(
            ClinicAppointment appointment,
            int userId = 0)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                INSERT INTO ClinicAppointments
                    (UserId, ClinicId, OwnerName, PetName, ServiceType,
                     AppointmentDate, ContactNumber, Status)
                VALUES
                    (@UserId, @ClinicId, @OwnerName, @PetName, @ServiceType,
                     @AppointmentDate, @ContactNumber, 'Scheduled')",
                conn);

            cmd.Parameters.AddWithValue("@UserId",
                userId > 0 ? userId : DBNull.Value);
            cmd.Parameters.AddWithValue("@ClinicId", appointment.ClinicId);
            cmd.Parameters.AddWithValue("@OwnerName", appointment.OwnerName ?? "");
            cmd.Parameters.AddWithValue("@PetName", appointment.PetName ?? "");
            cmd.Parameters.AddWithValue("@ServiceType", appointment.ServiceType ?? "");
            cmd.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
            cmd.Parameters.AddWithValue("@ContactNumber", appointment.ContactNumber ?? "");

            cmd.ExecuteNonQuery();
        }


        public List<ClinicAppointment> GetAppointments(
            int? userId = null,
            int? clinicId = null)
        {
            var appointments = new List<ClinicAppointment>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                var sql = @"
                    SELECT Id, UserId, ClinicId, OwnerName, PetName,
                           ServiceType, AppointmentDate, ContactNumber, Status
                    FROM ClinicAppointments
                    WHERE 1=1";

                if (userId.HasValue) sql += " AND UserId=@UserId";
                if (clinicId.HasValue) sql += " AND ClinicId=@ClinicId";

                sql += " ORDER BY AppointmentDate ASC";

                using var cmd = new SqlCommand(sql, conn);

                if (userId.HasValue)
                    cmd.Parameters.AddWithValue("@UserId", userId.Value);
                if (clinicId.HasValue)
                    cmd.Parameters.AddWithValue("@ClinicId", clinicId.Value);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    appointments.Add(new ClinicAppointment
                    {
                        Id = reader.GetInt32(0),

                        UserId = reader.IsDBNull(1)
                            ? 0
                            : reader.GetInt32(1),

                        ClinicId = reader.IsDBNull(2)
                            ? 0
                            : reader.GetInt32(2),

                        OwnerName = reader.IsDBNull(3)
                            ? string.Empty
                            : reader.GetString(3),

                        PetName = reader.IsDBNull(4)
                            ? string.Empty
                            : reader.GetString(4),

                        ServiceType = reader.IsDBNull(5)
                            ? string.Empty
                            : reader.GetString(5),

                        AppointmentDate = reader.IsDBNull(6)
                            ? DateTime.Now
                            : reader.GetDateTime(6),

                        ContactNumber = reader.IsDBNull(7)
                            ? string.Empty
                            : reader.GetString(7),

                        Status = reader.IsDBNull(8)
                            ? "Scheduled"
                            : reader.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[DatabaseService Error] GetAppointments failed: {ex}");
            }

            return appointments;
        }


        public bool UpdateAppointmentStatus(
            int appointmentId,
            string status,
            int? clinicId = null)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var sql = "UPDATE ClinicAppointments SET Status=@Status WHERE Id=@Id";
            if (clinicId.HasValue) sql += " AND ClinicId=@ClinicId";

            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Id", appointmentId);

            if (clinicId.HasValue)
                cmd.Parameters.AddWithValue("@ClinicId", clinicId.Value);

            return cmd.ExecuteNonQuery() > 0;
        }


        public void CancelAppointment(int appointmentId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "DELETE FROM ClinicAppointments WHERE Id=@Id", conn);

            cmd.Parameters.AddWithValue("@Id", appointmentId);

            cmd.ExecuteNonQuery();
        }


        public int GetAppointmentCountByUser(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM ClinicAppointments
                WHERE UserId=@UserId
                  AND AppointmentDate >= SYSDATETIME()", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        public int GetAppointmentCountByClinic(int clinicId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM ClinicAppointments
                WHERE ClinicId=@ClinicId
                  AND AppointmentDate >= SYSDATETIME()", conn);

            cmd.Parameters.AddWithValue("@ClinicId", clinicId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        // =========================================================
        // ORDERS / CART
        // =========================================================

        public int GetOrderCountByUser(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Orders WHERE UserId=@UserId", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        public int GetOrderCountByCustomer(string customerName)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Orders WHERE CustomerName=@CustomerName",
                conn);

            cmd.Parameters.AddWithValue("@CustomerName", customerName);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        public string? VerifyClaimToken(string token)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT TOP 1 ClaimToken
                FROM Orders
                WHERE ClaimToken=@Token
                  AND Status='Pending'", conn);

            cmd.Parameters.AddWithValue(
                "@Token",
                token.Trim().ToUpperInvariant());

            var result = cmd.ExecuteScalar();

            return result?.ToString();
        }


        public bool MarkOrderClaimed(string token)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE Orders
                SET Status='Claimed'
                WHERE ClaimToken=@Token
                  AND Status='Pending'", conn);

            cmd.Parameters.AddWithValue(
                "@Token",
                token.Trim().ToUpperInvariant());

            return cmd.ExecuteNonQuery() > 0;
        }


        public List<CartItem> GetCartItems()
        {
            return _cart;
        }


        public void AddOrIncrementCart(int productId)
        {
            var products = GetAllProducts();

            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product == null || product.Stock <= 0)
                return;

            var existing = _cart.FirstOrDefault(c => c.ProductId == productId);

            if (existing != null)
            {
                if (existing.Quantity < product.Stock)
                    existing.Quantity++;
            }
            else
            {
                _cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImageSource = product.ImageSource
                });
            }
        }


        public void RemoveFromCart(int productId)
        {
            var item = _cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
                _cart.Remove(item);
        }


        public string CreateClickAndCollectOrder(
            int clinicId,
            string customerName,
            string contact,
            string fulfillment,
            List<CartItem> items,
            int userId = 0)
        {
            if (items is null || items.Count == 0)
                throw new InvalidOperationException("Your cart is empty.");

            string token;

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                foreach (var item in items)
                {
                    using var stockCheck = new SqlCommand(@"
                        SELECT ClinicId, Stock
                        FROM PetProducts WITH (UPDLOCK, ROWLOCK)
                        WHERE Id=@Id",
                        conn, tx);

                    stockCheck.Parameters.AddWithValue("@Id", item.ProductId);

                    using var stockReader = stockCheck.ExecuteReader();

                    if (!stockReader.Read())
                        throw new InvalidOperationException(
                            $"Product '{item.Name}' is no longer available.");

                    var productClinicId = stockReader.IsDBNull(0)
                        ? (int?)null
                        : stockReader.GetInt32(0);

                    var stock = stockReader.GetInt32(1);

                    stockReader.Close();

                    if (productClinicId != clinicId)
                        throw new InvalidOperationException(
                            $"Product '{item.Name}' does not belong to the selected clinic.");

                    if (stock < item.Quantity)
                        throw new InvalidOperationException(
                            $"Not enough stock for '{item.Name}'. Available: {stock}.");
                }

                decimal totalAmount = items.Sum(i => i.Subtotal);

                token = "CLAIM-" + Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 8)
                    .ToUpperInvariant();

                using var orderCmd = new SqlCommand(@"
                    INSERT INTO Orders
                        (UserId, ClinicId, CustomerName, ContactNumber,
                         FulfillmentMethod, ClaimToken, TotalAmount, Status)
                    OUTPUT INSERTED.Id
                    VALUES
                        (@UserId, @ClinicId, @CustomerName, @Contact,
                         @Fulfillment, @Token, @Total, 'Pending')",
                    conn, tx);

                orderCmd.Parameters.AddWithValue("@UserId",
                    userId > 0 ? userId : DBNull.Value);
                orderCmd.Parameters.AddWithValue("@ClinicId", clinicId);
                orderCmd.Parameters.AddWithValue("@CustomerName", customerName);
                orderCmd.Parameters.AddWithValue("@Contact", contact);
                orderCmd.Parameters.AddWithValue("@Fulfillment", fulfillment);
                orderCmd.Parameters.AddWithValue("@Token", token);
                orderCmd.Parameters.AddWithValue("@Total", totalAmount);

                int orderId = Convert.ToInt32(orderCmd.ExecuteScalar());

                foreach (var item in items)
                {
                    using var itemCmd = new SqlCommand(@"
                        INSERT INTO OrderItems
                            (OrderId, ProductId, ProductName, Quantity, UnitPrice)
                        VALUES
                            (@OrderId, @ProductId, @ProductName, @Quantity, @UnitPrice)",
                        conn, tx);

                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@ProductName", item.Name);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", item.Price);

                    itemCmd.ExecuteNonQuery();

                    using var stockCmd = new SqlCommand(@"
                        UPDATE PetProducts
                        SET Stock = Stock - @Qty
                        WHERE Id = @Id",
                        conn, tx);

                    stockCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                    stockCmd.Parameters.AddWithValue("@Id", item.ProductId);

                    stockCmd.ExecuteNonQuery();
                }

                tx.Commit();

                _cart.Clear();

                return token;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        // =========================================================
        // ANNOUNCEMENTS — system-wide (admin) and clinic (future)
        // =========================================================

        public List<Announcement> GetActiveAnnouncements(int? clinicId = null, int limit = 10)
        {
            var list = new List<Announcement>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // If clinicId is null → fetch SYSTEM-wide (ClinicId IS NULL)
            // If clinicId is set  → fetch that clinic's announcements only
            var sql = @"
        SELECT TOP (@Limit)
               Id, ClinicId, Title, Body, Category, Priority,
               PostedAt, ExpiresAt, IsActive
        FROM Announcements
        WHERE IsActive = 1
          AND (ExpiresAt IS NULL OR ExpiresAt > SYSDATETIME())";

            if (clinicId.HasValue)
                sql += " AND ClinicId = @ClinicId";
            else
                sql += " AND ClinicId IS NULL";

            sql += " ORDER BY PostedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Limit", limit);
            if (clinicId.HasValue)
                cmd.Parameters.AddWithValue("@ClinicId", clinicId.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Announcement
                {
                    Id = r.GetInt32(0),
                    ClinicId = r.IsDBNull(1) ? null : r.GetInt32(1),
                    Title = r.GetString(2),
                    Body = r.GetString(3),
                    Category = r.GetString(4),
                    Priority = r.GetString(5),
                    PostedAt = r.GetDateTime(6),
                    ExpiresAt = r.IsDBNull(7) ? null : r.GetDateTime(7),
                    IsActive = r.GetBoolean(8)
                });
            }
            return list;
        }


        public bool DeactivateAnnouncement(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        UPDATE Announcements
        SET IsActive = 0
        WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool PostAnnouncement(Announcement a)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        INSERT INTO Announcements
            (ClinicId, Title, Body, Category, Priority, ExpiresAt)
        OUTPUT INSERTED.Id
        VALUES
            (@ClinicId, @Title, @Body, @Category, @Priority, @ExpiresAt)", conn);

            cmd.Parameters.AddWithValue("@ClinicId", a.ClinicId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Title", a.Title);
            cmd.Parameters.AddWithValue("@Body", a.Body);
            cmd.Parameters.AddWithValue("@Category", a.Category);
            cmd.Parameters.AddWithValue("@Priority", a.Priority);
            cmd.Parameters.AddWithValue("@ExpiresAt", a.ExpiresAt ?? (object)DBNull.Value);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public List<Announcement> GetAllAnnouncementsForAdmin()
        {
            var list = new List<Announcement>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        SELECT Id, ClinicId, Title, Body, Category, Priority,
               PostedAt, ExpiresAt, IsActive
        FROM Announcements
        ORDER BY PostedAt DESC", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Announcement
                {
                    Id = r.GetInt32(0),
                    ClinicId = r.IsDBNull(1) ? null : r.GetInt32(1),
                    Title = r.GetString(2),
                    Body = r.GetString(3),
                    Category = r.GetString(4),
                    Priority = r.GetString(5),
                    PostedAt = r.GetDateTime(6),
                    ExpiresAt = r.IsDBNull(7) ? null : r.GetDateTime(7),
                    IsActive = r.GetBoolean(8)
                });
            }
            return list;
        }
        public DateTime? GetLastFedAt(int petId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        SELECT TOP 1 FedAt
        FROM FoodLogs
        WHERE PetId = @PetId
        ORDER BY FedAt DESC", conn);
            cmd.Parameters.AddWithValue("@PetId", petId);

            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : (DateTime?)result;
        }

        // =========================================================
        // CLINIC VERIFICATION (admin actions)
        // =========================================================

        public bool VerifyClinic(int clinicId, string reference, decimal fee)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                using (var cmd = new SqlCommand(@"
            UPDATE ClinicTenants
            SET IsVerified = 1,
                VerifiedAt = SYSDATETIME(),
                VerificationStatus = 'Verified',
                VerificationFee = @Fee,
                VerificationReference = @Ref
            WHERE Id = @Id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", clinicId);
                    cmd.Parameters.AddWithValue("@Fee", fee);
                    cmd.Parameters.AddWithValue("@Ref", reference);

                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        tx.Rollback();
                        return false;
                    }
                }

                using (var log = new SqlCommand(@"
            INSERT INTO Transactions (ClinicId, Type, Amount, Reference)
            VALUES (@ClinicId, 'BusinessVerification', @Amount, @Ref)", conn, tx))
                {
                    log.Parameters.AddWithValue("@ClinicId", clinicId);
                    log.Parameters.AddWithValue("@Amount", fee);
                    log.Parameters.AddWithValue("@Ref", reference);
                    log.ExecuteNonQuery();
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

        public bool UnverifyClinic(int clinicId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        UPDATE ClinicTenants
        SET IsVerified = 0,
            VerificationStatus = 'Unverified'
        WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", clinicId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // =========================================================
        // COMMUNITY (web preview)
        // =========================================================

        public List<CommunityPost> GetRecentCommunityPosts(int count = 5)
        {
            var list = new List<CommunityPost>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        SELECT TOP (@Count)
               c.Id, c.UserId, c.PetId, c.Caption, c.ImageUrl,
               c.LikeCount, c.PostedAt,
               ISNULL(u.FullName, 'Unknown') AS AuthorName,
               ISNULL(p.PetName, '') AS PetName
        FROM CommunityPosts c
        LEFT JOIN UserAccounts u ON u.Id = c.UserId
        LEFT JOIN PetProfiles p ON p.Id = c.PetId
        ORDER BY c.PostedAt DESC", conn);
            cmd.Parameters.AddWithValue("@Count", count);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new CommunityPost
                {
                    Id = r.GetInt32(0),
                    UserId = r.GetInt32(1),
                    PetId = r.IsDBNull(2) ? null : r.GetInt32(2),
                    Caption = r.IsDBNull(3) ? "" : r.GetString(3),
                    ImageUrl = r.IsDBNull(4) ? "" : r.GetString(4),
                    LikeCount = r.GetInt32(5),
                    PostedAt = r.GetDateTime(6),
                    AuthorName = r.GetString(7),
                    PetName = r.GetString(8)
                });
            }
            return list;
        }
        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        public int GetUnreadNotificationCount(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        SELECT COUNT(*)
        FROM Notifications
        WHERE UserId = @UserId
          AND IsRead = 0", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Notification> GetNotifications(int userId, int limit = 20)
        {
            var list = new List<Notification>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        SELECT TOP (@Limit)
               Id, UserId, Title, Body, Link, Icon,
               IsRead, CreatedAt
        FROM Notifications
        WHERE UserId = @UserId
        ORDER BY CreatedAt DESC", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Limit", limit);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Notification
                {
                    Id = r.GetInt32(0),
                    UserId = r.GetInt32(1),
                    Title = r.IsDBNull(2) ? "" : r.GetString(2),
                    Body = r.IsDBNull(3) ? "" : r.GetString(3),
                    Link = r.IsDBNull(4) ? "" : r.GetString(4),
                    Icon = r.IsDBNull(5) ? "🔔" : r.GetString(5),
                    IsRead = r.GetBoolean(6),
                    CreatedAt = r.GetDateTime(7)
                });
            }
            return list;
        }

        public bool MarkNotificationRead(int notificationId, int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        UPDATE Notifications
        SET IsRead = 1
        WHERE Id = @Id
          AND UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@Id", notificationId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool MarkAllNotificationsRead(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        UPDATE Notifications
        SET IsRead = 1
        WHERE UserId = @UserId
          AND IsRead = 0", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public int CreateNotification(int userId, string title,
                                      string body = "", string link = "",
                                      string icon = "🔔")
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        INSERT INTO Notifications (UserId, Title, Body, Link, Icon)
        OUTPUT INSERTED.Id
        VALUES (@UserId, @Title, @Body, @Link, @Icon)", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Body", body ?? "");
            cmd.Parameters.AddWithValue("@Link", link ?? "");
            cmd.Parameters.AddWithValue("@Icon", icon ?? "🔔");

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        // =========================================================
        // COMMUNITY POSTS (pet owner posts)
        // =========================================================

        public bool CreateCommunityPost(
            int userId,
            int? petId,
            string caption,
            string imageUrl)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
        INSERT INTO CommunityPosts
            (UserId, PetId, Caption, ImageUrl, PostedAt)
        VALUES
            (@UserId, @PetId, @Caption, @ImageUrl, SYSDATETIME())",
                conn);

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@PetId",
                petId.HasValue ? petId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Caption", caption);
            cmd.Parameters.AddWithValue("@ImageUrl",
                string.IsNullOrWhiteSpace(imageUrl) ? "" : imageUrl);

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}