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

        public HashSet<int> GetLikedPostIds(
    int userId)
        {
            var ids =
                new HashSet<int>();


            using var conn =
                new SqlConnection(
                    _connectionString);


            conn.Open();


            using var cmd =
                new SqlCommand(@"
            SELECT PostId

            FROM CommunityLikes

            WHERE UserId =
                @UserId;",
                    conn);


            cmd.Parameters.AddWithValue(
                "@UserId",
                userId);


            using var reader =
                cmd.ExecuteReader();


            while (reader.Read())
            {
                ids.Add(
                    reader.GetInt32(0));
            }


            return ids;
        }


        public bool ToggleLike(
            int postId,
            int userId)
        {
            using var conn =
                new SqlConnection(
                    _connectionString);


            conn.Open();


            using var transaction =
                conn.BeginTransaction();


            try
            {
                bool alreadyLiked;


                using (var check =
                    new SqlCommand(@"
                SELECT COUNT(1)

                FROM CommunityLikes

                WHERE
                    PostId = @PostId
                    AND
                    UserId = @UserId;",
                        conn,
                        transaction))
                {
                    check.Parameters.AddWithValue(
                        "@PostId",
                        postId);


                    check.Parameters.AddWithValue(
                        "@UserId",
                        userId);


                    alreadyLiked =
                        Convert.ToInt32(
                            check.ExecuteScalar()) > 0;
                }


                if (alreadyLiked)
                {
                    using var remove =
                        new SqlCommand(@"
                    DELETE FROM CommunityLikes

                    WHERE
                        PostId = @PostId
                        AND
                        UserId = @UserId;",
                            conn,
                            transaction);


                    remove.Parameters.AddWithValue(
                        "@PostId",
                        postId);


                    remove.Parameters.AddWithValue(
                        "@UserId",
                        userId);


                    remove.ExecuteNonQuery();


                    using var decrease =
                        new SqlCommand(@"
                    UPDATE CommunityPosts

                    SET LikeCount =
                        CASE
                            WHEN LikeCount > 0
                                THEN LikeCount - 1
                            ELSE 0
                        END

                    WHERE Id =
                        @PostId;",
                            conn,
                            transaction);


                    decrease.Parameters.AddWithValue(
                        "@PostId",
                        postId);


                    decrease.ExecuteNonQuery();
                }
                else
                {
                    using var add =
                        new SqlCommand(@"
                    INSERT INTO CommunityLikes
                    (
                        PostId,
                        UserId
                    )

                    VALUES
                    (
                        @PostId,
                        @UserId
                    );",
                            conn,
                            transaction);


                    add.Parameters.AddWithValue(
                        "@PostId",
                        postId);


                    add.Parameters.AddWithValue(
                        "@UserId",
                        userId);


                    add.ExecuteNonQuery();


                    using var increase =
                        new SqlCommand(@"
                    UPDATE CommunityPosts

                    SET LikeCount =
                        LikeCount + 1

                    WHERE Id =
                        @PostId;",
                            conn,
                            transaction);


                    increase.Parameters.AddWithValue(
                        "@PostId",
                        postId);


                    increase.ExecuteNonQuery();
                }


                transaction.Commit();


                return
                    !alreadyLiked;
            }
            catch
            {
                transaction.Rollback();

                throw;
            }
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