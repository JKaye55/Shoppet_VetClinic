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
                        ISNULL(c.IsEdited, 0),

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
        // COMMENTS
        // The final migration creates this table. This lightweight
        // guard also makes the Community resilient on an existing
        // finals database that has not yet rerun the latest migration.
        // =========================================================

        private static void EnsureCommentsSchema(SqlConnection conn)
        {
            using var cmd = new SqlCommand(@"
                IF OBJECT_ID('dbo.CommunityComments', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.CommunityComments
                    (
                        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        PostId INT NOT NULL,
                        UserId INT NULL,
                        AuthorName NVARCHAR(120) NOT NULL,
                        Body NVARCHAR(300) NOT NULL,
                        IsGuest BIT NOT NULL
                            CONSTRAINT DF_CommunityComments_IsGuest_Runtime DEFAULT (0),
                        CreatedAt DATETIME2 NOT NULL
                            CONSTRAINT DF_CommunityComments_CreatedAt_Runtime DEFAULT (SYSDATETIME()),
                        CONSTRAINT FK_CommunityComments_Post_Runtime
                            FOREIGN KEY (PostId) REFERENCES dbo.CommunityPosts(Id),
                        CONSTRAINT FK_CommunityComments_User_Runtime
                            FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts(Id)
                    );
                END;", conn);

            cmd.ExecuteNonQuery();
        }


        // =========================================================
        // COMMENTS
        // =========================================================

        public List<CommunityComment> GetComments(int postId)
        {
            var comments = new List<CommunityComment>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            EnsureCommentsSchema(conn);

            using var cmd = new SqlCommand(@"
                SELECT cc.Id,cc.PostId,cc.UserId,COALESCE(u.FullName,cc.AuthorName,'Pet Owner'),COALESCE(cc.Content,cc.Body,''),cc.IsGuest,cc.CreatedAt,cc.ParentCommentId,(SELECT COUNT(*) FROM CommunityCommentLikes WHERE CommentId=cc.Id)
                FROM CommunityComments cc LEFT JOIN UserAccounts u ON u.Id=cc.UserId
                WHERE cc.PostId=@PostId ORDER BY cc.CreatedAt,cc.Id;", conn);

            cmd.Parameters.AddWithValue("@PostId", postId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                comments.Add(new CommunityComment
                {
                    Id = reader.GetInt32(0),
                    PostId = reader.GetInt32(1),
                    UserId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    AuthorName = reader.GetString(3),
                    Body = reader.GetString(4),
                    IsGuest = reader.GetBoolean(5),
                    CreatedAt = reader.GetDateTime(6),
                    ParentCommentId=reader.IsDBNull(7)?null:reader.GetInt32(7),LikeCount=reader.GetInt32(8)
                });
            }

            return comments;
        }

        public bool AddComment(int postId, int? userId, string authorName, string body, bool isGuest,int? parentId=null)
        {
            if(isGuest||!userId.HasValue)return false;
            // Guest identity is intentionally fixed. A guest has no
            // authenticated profile and must never be able to impersonate
            // a named ShoppetCare member.
            if (isGuest)
            {
                userId = null;
                authorName = "Guest";
            }
            else
            {
                authorName = string.IsNullOrWhiteSpace(authorName)
                    ? "Pet Owner"
                    : authorName.Trim();
            }

            body = (body ?? string.Empty).Trim();

            if (body.Length == 0 || body.Length > 300)
                return false;

            if (authorName.Length > 120)
                authorName = authorName[..120];

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            EnsureCommentsSchema(conn);

            using var cmd = new SqlCommand(@"
                INSERT INTO CommunityComments
                (PostId, UserId, AuthorName, Body, IsGuest, CreatedAt,Content,ParentCommentId)
                SELECT @PostId,@UserId,@AuthorName,@Body,0,SYSDATETIME(),@Body,@Parent
                WHERE EXISTS(SELECT 1 FROM CommunityPosts WHERE Id=@PostId)
                  AND (@Parent IS NULL OR EXISTS(SELECT 1 FROM CommunityComments WHERE Id=@Parent AND PostId=@PostId));", conn);

            cmd.Parameters.AddWithValue("@PostId", postId);
            cmd.Parameters.AddWithValue("@UserId", userId.HasValue ? userId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@AuthorName", authorName);
            cmd.Parameters.AddWithValue("@Body", body);
            cmd.Parameters.AddWithValue("@IsGuest", isGuest);
            cmd.Parameters.AddWithValue("@Parent",(object?)parentId??DBNull.Value);

            return cmd.ExecuteNonQuery() > 0;
        }

        // =========================================================
        public void ToggleCommentLike(int id,int user)
        {
            using var c=new SqlConnection(_connectionString);c.Open();using var tx=c.BeginTransaction(System.Data.IsolationLevel.Serializable);
            using var q=new SqlCommand("IF EXISTS(SELECT 1 FROM CommunityCommentLikes WITH(UPDLOCK,HOLDLOCK) WHERE CommentId=@Id AND UserId=@U) DELETE FROM CommunityCommentLikes WHERE CommentId=@Id AND UserId=@U; ELSE INSERT INTO CommunityCommentLikes(CommentId,UserId) SELECT @Id,@U WHERE EXISTS(SELECT 1 FROM CommunityComments WHERE Id=@Id);",c,tx);
            q.Parameters.AddWithValue("@Id",id);q.Parameters.AddWithValue("@U",user);q.ExecuteNonQuery();tx.Commit();
        }
        public void DeleteComment(int id,int user,bool admin)
        {
            using var c=new SqlConnection(_connectionString);c.Open();using var tx=c.BeginTransaction();
            using var q=new SqlCommand(@"IF EXISTS(SELECT 1 FROM CommunityComments WHERE Id=@Id AND (UserId=@U OR @Admin=1)) BEGIN
            ;WITH descendants AS(SELECT Id FROM CommunityComments WHERE Id=@Id UNION ALL SELECT cc.Id FROM CommunityComments cc JOIN descendants d ON cc.ParentCommentId=d.Id) SELECT Id INTO #Removal FROM descendants;
            DELETE FROM CommunityCommentLikes WHERE CommentId IN(SELECT Id FROM #Removal);
            DELETE FROM CommunityComments WHERE Id IN(SELECT Id FROM #Removal); END",c,tx);
            q.Parameters.AddWithValue("@Id",id);q.Parameters.AddWithValue("@U",user);q.Parameters.AddWithValue("@Admin",admin);q.ExecuteNonQuery();tx.Commit();
        }

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
                        Caption = @Caption,
                        IsEdited = 1

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
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            EnsureCommentsSchema(conn);

            using var tx = conn.BeginTransaction();

            try
            {
                using (var comments = new SqlCommand(@"
                    DELETE cl FROM CommunityCommentLikes cl JOIN CommunityComments cc ON cc.Id=cl.CommentId WHERE cc.PostId=@PostId AND EXISTS(SELECT 1 FROM CommunityPosts WHERE Id=@PostId AND UserId=@UserId);
                    DELETE FROM CommunityComments
                    WHERE PostId = @PostId
                      AND EXISTS
                      (
                          SELECT 1 FROM CommunityPosts
                          WHERE Id = @PostId AND UserId = @UserId
                      );", conn, tx))
                {
                    comments.Parameters.AddWithValue("@PostId", postId);
                    comments.Parameters.AddWithValue("@UserId", userId);
                    comments.ExecuteNonQuery();
                }

                using (var likes = new SqlCommand(@"
                    DELETE FROM CommunityLikes
                    WHERE PostId = @PostId
                      AND EXISTS
                      (
                          SELECT 1 FROM CommunityPosts
                          WHERE Id = @PostId AND UserId = @UserId
                      );", conn, tx))
                {
                    likes.Parameters.AddWithValue("@PostId", postId);
                    likes.Parameters.AddWithValue("@UserId", userId);
                    likes.ExecuteNonQuery();
                }

                using var post = new SqlCommand(@"
                    DELETE FROM CommunityPosts
                    WHERE Id = @PostId AND UserId = @UserId;", conn, tx);

                post.Parameters.AddWithValue("@PostId", postId);
                post.Parameters.AddWithValue("@UserId", userId);

                var deleted = post.ExecuteNonQuery() > 0;
                tx.Commit();
                return deleted;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }


        // =========================================================
        // ADMIN MODERATION
        // =========================================================

        public bool AdminDeletePost(int postId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            EnsureCommentsSchema(conn);

            using var tx = conn.BeginTransaction();

            try
            {
                using (var comments = new SqlCommand(
                    "DELETE cl FROM CommunityCommentLikes cl JOIN CommunityComments cc ON cc.Id=cl.CommentId WHERE cc.PostId=@PostId; DELETE FROM CommunityComments WHERE PostId = @PostId;",
                    conn,
                    tx))
                {
                    comments.Parameters.AddWithValue("@PostId", postId);
                    comments.ExecuteNonQuery();
                }

                using (var likes = new SqlCommand(
                    "DELETE FROM CommunityLikes WHERE PostId = @PostId;",
                    conn,
                    tx))
                {
                    likes.Parameters.AddWithValue("@PostId", postId);
                    likes.ExecuteNonQuery();
                }

                using var post = new SqlCommand(
                    "DELETE FROM CommunityPosts WHERE Id = @PostId;",
                    conn,
                    tx);

                post.Parameters.AddWithValue("@PostId", postId);

                var deleted = post.ExecuteNonQuery() > 0;
                tx.Commit();
                return deleted;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
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

                IsEdited =
                    !reader.IsDBNull(7)
                    && reader.GetBoolean(7),

                AuthorName =
                    reader.IsDBNull(8)
                        ? "ShoppetCare User"
                        : reader.GetString(8),

                PetName =
                    reader.IsDBNull(9)
                        ? string.Empty
                        : reader.GetString(9)
            };
        }
    }
}
