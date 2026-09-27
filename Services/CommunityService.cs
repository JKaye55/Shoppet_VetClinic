using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services
{
    public class CommunityService
    {
        private readonly string _connectionString;


        public CommunityService(
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
        // GET POSTS
        // =========================================================

        public List<CommunityPost> GetRecentPosts(
            int count = 50)
        {
            var posts =
                new List<CommunityPost>();


            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    SELECT TOP (@Count)

                        c.Id,
                        c.UserId,
                        c.PetId,
                        c.Caption,
                        c.ImageUrl,
                        c.LikeCount,
                        c.PostedAt,

                        ISNULL(
                            u.FullName,
                            'ShoppetCare User'
                        ) AS AuthorName,

                        ISNULL(
                            p.PetName,
                            ''
                        ) AS PetName

                    FROM CommunityPosts c

                    LEFT JOIN UserAccounts u
                        ON u.Id = c.UserId

                    LEFT JOIN PetProfiles p
                        ON p.Id = c.PetId

                    ORDER BY
                        c.PostedAt DESC;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@Count",
                count);


            using var reader =
                cmd.ExecuteReader();


            while (reader.Read())
            {
                posts.Add(
                    MapPost(reader));
            }


            return posts;
        }


        // =========================================================
        // CREATE POST
        // Returns the new Post ID so FileStorageService can save
        // the uploaded image using that ID.
        // =========================================================

        public int CreatePost(
            int userId,
            int? petId,
            string caption)
        {
            petId =
                ValidateOwnedPet(
                    userId,
                    petId);


            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    INSERT INTO CommunityPosts
                    (
                        UserId,
                        PetId,
                        Caption,
                        ImageUrl,
                        PostedAt
                    )

                    OUTPUT INSERTED.Id

                    VALUES
                    (
                        @UserId,
                        @PetId,
                        @Caption,
                        '',
                        SYSDATETIME()
                    );",
                    conn);


            cmd.Parameters.AddWithValue(
                "@UserId",
                userId);


            cmd.Parameters.AddWithValue(
                "@PetId",
                petId.HasValue
                    ? petId.Value
                    : DBNull.Value);


            cmd.Parameters.AddWithValue(
                "@Caption",
                caption);


            return Convert.ToInt32(
                cmd.ExecuteScalar());
        }


        // =========================================================
        // UPDATE POST
        // UserId in WHERE protects ownership.
        // =========================================================

        public bool UpdatePost(
            int postId,
            int userId,
            int? petId,
            string caption)
        {
            petId =
                ValidateOwnedPet(
                    userId,
                    petId);


            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    UPDATE CommunityPosts

                    SET
                        PetId = @PetId,
                        Caption = @Caption

                    WHERE
                        Id = @PostId
                        AND
                        UserId = @UserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@PostId",
                postId);


            cmd.Parameters.AddWithValue(
                "@UserId",
                userId);


            cmd.Parameters.AddWithValue(
                "@PetId",
                petId.HasValue
                    ? petId.Value
                    : DBNull.Value);


            cmd.Parameters.AddWithValue(
                "@Caption",
                caption);


            return
                cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // IMAGE
        // =========================================================

        public bool UpdatePostImage(
            int postId,
            int userId,
            string imageUrl)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    UPDATE CommunityPosts

                    SET ImageUrl = @ImageUrl

                    WHERE
                        Id = @PostId
                        AND
                        UserId = @UserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@PostId",
                postId);


            cmd.Parameters.AddWithValue(
                "@UserId",
                userId);


            cmd.Parameters.AddWithValue(
                "@ImageUrl",
                imageUrl ?? string.Empty);


            return
                cmd.ExecuteNonQuery() > 0;
        }


        public bool ClearPostImage(
            int postId,
            int userId)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    UPDATE CommunityPosts

                    SET ImageUrl = ''

                    WHERE
                        Id = @PostId
                        AND
                        UserId = @UserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@PostId",
                postId);


            cmd.Parameters.AddWithValue(
                "@UserId",
                userId);


            return
                cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // DELETE OWN POST
        // =========================================================

        public bool DeletePost(
            int postId,
            int userId)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    DELETE FROM CommunityPosts

                    WHERE
                        Id = @PostId
                        AND
                        UserId = @UserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@PostId",
                postId);


            cmd.Parameters.AddWithValue(
                "@UserId",
                userId);


            return
                cmd.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // PET OWNERSHIP
        // A post may only tag one of that user's pets.
        // =========================================================

        private int? ValidateOwnedPet(
            int userId,
            int? petId)
        {
            if (!petId.HasValue ||
                petId.Value <= 0)
            {
                return null;
            }


            using var conn =
                new SqlConnection(
                    _connectionString);

            conn.Open();


            using var cmd =
                new SqlCommand(@"
                    SELECT COUNT(*)

                    FROM PetProfiles

                    WHERE
                        Id = @PetId
                        AND
                        UserId = @UserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@PetId",
                petId.Value);


            cmd.Parameters.AddWithValue(
                "@UserId",
                userId);


            var count =
                Convert.ToInt32(
                    cmd.ExecuteScalar());


            return
                count > 0
                    ? petId
                    : null;
        }


        // =========================================================
        // MAP
        // =========================================================

        private static CommunityPost MapPost(
            SqlDataReader reader)
        {
            return new CommunityPost
            {
                Id =
                    reader.GetInt32(0),

                UserId =
                    reader.GetInt32(1),

                PetId =
                    reader.IsDBNull(2)
                        ? null
                        : reader.GetInt32(2),

                Caption =
                    reader.IsDBNull(3)
                        ? string.Empty
                        : reader.GetString(3),

                ImageUrl =
                    reader.IsDBNull(4)
                        ? string.Empty
                        : reader.GetString(4),

                LikeCount =
                    reader.IsDBNull(5)
                        ? 0
                        : reader.GetInt32(5),

                PostedAt =
                    reader.GetDateTime(6),

                AuthorName =
                    reader.IsDBNull(7)
                        ? "ShoppetCare User"
                        : reader.GetString(7),

                PetName =
                    reader.IsDBNull(8)
                        ? string.Empty
                        : reader.GetString(8)
            };
        }
    }
}