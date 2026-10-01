using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;
using System.Data;

namespace Shoppet_VetClinic.Services
{
    public class MarketplaceService
    {
        private readonly string _connectionString;


        public MarketplaceService(
            IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString(
                    "ShoppetDb")
                ??
                throw new InvalidOperationException(
                    "Connection string 'ShoppetDb' was not found.");
        }


        // =========================================================
        // GET ALL LISTINGS
        // =========================================================

        public List<MarketplaceListing>
            GetAllListings()
        {
            var listings =
                new List<MarketplaceListing>();


            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    SELECT
                        m.Id,
                        m.SellerUserId,
                        m.Title,
                        m.Category,
                        m.ItemCondition,
                        m.Price,
                        m.Description,
                        m.Location,
                        m.ImageUrl,
                        m.Status,
                        m.CreatedAt,

                        ISNULL(
                            u.FullName,
                            'ShoppetCare User'
                        ) AS SellerName,

                        ISNULL(
                            u.Email,
                            ''
                        ) AS SellerEmail,

                        CAST(0 AS bit) AS IsVerifiedSeller

                    FROM MarketplaceListings m

                    INNER JOIN UserAccounts u
                        ON u.Id = m.SellerUserId

                    WHERE m.Status<>'Deleted'
                    ORDER BY
                        m.CreatedAt DESC;",
                    conn);


            using var reader =
                cmd.ExecuteReader();


            while (reader.Read())
            {
                listings.Add(
                    MapListing(reader));
            }


            return listings;
        }


        // =========================================================
        // GET SINGLE LISTING
        // =========================================================

        public MarketplaceListing?
            GetListingById(
                int listingId)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    SELECT
                        m.Id,
                        m.SellerUserId,
                        m.Title,
                        m.Category,
                        m.ItemCondition,
                        m.Price,
                        m.Description,
                        m.Location,
                        m.ImageUrl,
                        m.Status,
                        m.CreatedAt,

                        ISNULL(
                            u.FullName,
                            'ShoppetCare User'
                        ) AS SellerName,

                        ISNULL(
                            u.Email,
                            ''
                        ) AS SellerEmail,

                        CAST(0 AS bit) AS IsVerifiedSeller

                    FROM MarketplaceListings m

                    INNER JOIN UserAccounts u
                        ON u.Id = m.SellerUserId

                    WHERE m.Id = @Id;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@Id",
                listingId);


            using var reader =
                cmd.ExecuteReader();


            if (reader.Read())
            {
                return MapListing(
                    reader);
            }


            return null;
        }


        // =========================================================
        // CREATE LISTING
        // =========================================================

        public int CreateListing(
            MarketplaceListing listing)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    INSERT INTO MarketplaceListings
                    (
                        SellerUserId,
                        Title,
                        Category,
                        ItemCondition,
                        Price,
                        Description,
                        Location,
                        ImageUrl,
                        Status,
                        CreatedAt
                    )

                    OUTPUT INSERTED.Id

                    VALUES
                    (
                        @SellerUserId,
                        @Title,
                        @Category,
                        @ItemCondition,
                        @Price,
                        @Description,
                        @Location,
                        @ImageUrl,
                        'Available',
                        SYSDATETIME()
                    );",
                    conn);


            cmd.Parameters.AddWithValue(
                "@SellerUserId",
                listing.SellerUserId);

            cmd.Parameters.AddWithValue(
                "@Title",
                listing.Title);

            cmd.Parameters.AddWithValue(
                "@Category",
                listing.Category);

            cmd.Parameters.AddWithValue(
                "@ItemCondition",
                listing.ItemCondition);


            var priceParameter =
                new SqlParameter(
                    "@Price",
                    SqlDbType.Decimal)
                {
                    Precision = 10,
                    Scale = 2,
                    Value = listing.Price
                };


            cmd.Parameters.Add(
                priceParameter);


            cmd.Parameters.AddWithValue(
                "@Description",
                string.IsNullOrWhiteSpace(
                    listing.Description)
                    ? DBNull.Value
                    : listing.Description);

            cmd.Parameters.AddWithValue(
                "@Location",
                string.IsNullOrWhiteSpace(
                    listing.Location)
                    ? DBNull.Value
                    : listing.Location);

            cmd.Parameters.AddWithValue(
                "@ImageUrl",
                string.IsNullOrWhiteSpace(
                    listing.ImageUrl)
                    ? DBNull.Value
                    : listing.ImageUrl);


            return Convert.ToInt32(
                cmd.ExecuteScalar());
        }


        // =========================================================
        // UPDATE LISTING
        //
        // SellerUserId is included in the WHERE clause so one
        // pet owner cannot edit another owner's listing.
        // =========================================================

        public bool UpdateListing(
            MarketplaceListing listing)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    UPDATE MarketplaceListings

                    SET
                        Title = @Title,
                        Category = @Category,
                        ItemCondition = @ItemCondition,
                        Price = @Price,
                        Description = @Description,
                        Location = @Location

                    WHERE
                        Id = @Id
                        AND
                        SellerUserId = @SellerUserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@Id",
                listing.Id);

            cmd.Parameters.AddWithValue(
                "@SellerUserId",
                listing.SellerUserId);

            cmd.Parameters.AddWithValue(
                "@Title",
                listing.Title);

            cmd.Parameters.AddWithValue(
                "@Category",
                listing.Category);

            cmd.Parameters.AddWithValue(
                "@ItemCondition",
                listing.ItemCondition);


            var priceParameter =
                new SqlParameter(
                    "@Price",
                    SqlDbType.Decimal)
                {
                    Precision = 10,
                    Scale = 2,
                    Value = listing.Price
                };


            cmd.Parameters.Add(
                priceParameter);


            cmd.Parameters.AddWithValue(
                "@Description",
                string.IsNullOrWhiteSpace(
                    listing.Description)
                    ? DBNull.Value
                    : listing.Description);

            cmd.Parameters.AddWithValue(
                "@Location",
                string.IsNullOrWhiteSpace(
                    listing.Location)
                    ? DBNull.Value
                    : listing.Location);


            return
                cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // UPDATE IMAGE
        // =========================================================

        public bool UpdateListingImage(
            int listingId,
            int sellerUserId,
            string imageUrl)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    UPDATE MarketplaceListings

                    SET ImageUrl = @ImageUrl

                    WHERE
                        Id = @Id
                        AND
                        SellerUserId = @SellerUserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@Id",
                listingId);

            cmd.Parameters.AddWithValue(
                "@SellerUserId",
                sellerUserId);

            cmd.Parameters.AddWithValue(
                "@ImageUrl",
                imageUrl);


            return
                cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // STATUS
        // Available / Reserved / Sold
        // =========================================================

        public bool UpdateListingStatus(
            int listingId,
            int sellerUserId,
            string status)
        {
            var allowedStatuses =
                new[]
                {
                    "Available",
                    "Reserved",
                    "Unavailable",
                    "Sold"
                };


            if (!allowedStatuses.Contains(
                    status,
                    StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }


            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    UPDATE MarketplaceListings

                    SET Status = @Status,UpdatedAt=SYSDATETIME()

                    WHERE
                        Id = @Id
                        AND
                        SellerUserId = @SellerUserId AND Status<>'Deleted' AND (@Status<>'Available' OR NOT EXISTS(SELECT 1 FROM MarketplaceOrderItems WHERE ListingId=@Id));",
                    conn);


            cmd.Parameters.AddWithValue(
                "@Id",
                listingId);

            cmd.Parameters.AddWithValue(
                "@SellerUserId",
                sellerUserId);

            cmd.Parameters.AddWithValue(
                "@Status",
                status);


            return
                cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // DELETE OWN LISTING
        // =========================================================

        public bool DeleteListing(
            int listingId,
            int sellerUserId)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    UPDATE MarketplaceListings SET Status='Deleted',UpdatedAt=SYSDATETIME()

                    WHERE
                        Id = @Id
                        AND
                        SellerUserId = @SellerUserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@Id",
                listingId);

            cmd.Parameters.AddWithValue(
                "@SellerUserId",
                sellerUserId);


            return
                cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // ADMIN MODERATION
        // =========================================================

        public bool AdminDeleteListing(int listingId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "UPDATE MarketplaceListings SET Status='Deleted',UpdatedAt=SYSDATETIME() WHERE Id = @Id;",
                conn);

            cmd.Parameters.AddWithValue("@Id", listingId);

            return cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // DATABASE → MODEL
        // =========================================================

        private static MarketplaceListing
            MapListing(
                SqlDataReader reader)
        {
            return new MarketplaceListing
            {
                Id =
                    reader.GetInt32(0),

                SellerUserId =
                    reader.GetInt32(1),

                Title =
                    reader.IsDBNull(2)
                        ? string.Empty
                        : reader.GetString(2),

                Category =
                    reader.IsDBNull(3)
                        ? string.Empty
                        : reader.GetString(3),

                ItemCondition =
                    reader.IsDBNull(4)
                        ? string.Empty
                        : reader.GetString(4),

                Price =
                    reader.GetDecimal(5),

                Description =
                    reader.IsDBNull(6)
                        ? string.Empty
                        : reader.GetString(6),

                Location =
                    reader.IsDBNull(7)
                        ? string.Empty
                        : reader.GetString(7),

                ImageUrl =
                    reader.IsDBNull(8)
                        ? string.Empty
                        : reader.GetString(8),

                Status =
                    reader.IsDBNull(9)
                        ? "Available"
                        : reader.GetString(9),

                CreatedAt =
                    reader.GetDateTime(10),

                SellerName =
                    reader.IsDBNull(11)
                        ? "ShoppetCare User"
                        : reader.GetString(11),

                SellerEmail =
                    reader.IsDBNull(12)
                        ? string.Empty
                        : reader.GetString(12),

                IsVerifiedSeller =
                    !reader.IsDBNull(13)
                    && reader.GetBoolean(13)
            };
        }
    }
}
