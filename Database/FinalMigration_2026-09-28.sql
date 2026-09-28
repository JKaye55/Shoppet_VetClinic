/*
    ShoppetCare Final Web Migration
    Branch: final-website-2026-09-28
    Database: Shoppet_VetClinic_DB

    Safe re-runnable migration. Existing data is preserved.
*/

USE [Shoppet_VetClinic_DB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* ============================================================
       1. USER ACCOUNT FIELDS
       ============================================================ */

    IF COL_LENGTH('dbo.UserAccounts', 'MobileNumber') IS NULL
        ALTER TABLE dbo.UserAccounts ADD MobileNumber NVARCHAR(30) NULL;

    IF COL_LENGTH('dbo.UserAccounts', 'IsPremium') IS NULL
        ALTER TABLE dbo.UserAccounts
        ADD IsPremium BIT NOT NULL
            CONSTRAINT DF_UserAccounts_IsPremium_Final DEFAULT (0) WITH VALUES;

    IF COL_LENGTH('dbo.UserAccounts', 'PremiumActivatedAt') IS NULL
        ALTER TABLE dbo.UserAccounts ADD PremiumActivatedAt DATETIME2 NULL;

    IF COL_LENGTH('dbo.UserAccounts', 'PremiumReference') IS NULL
        ALTER TABLE dbo.UserAccounts ADD PremiumReference NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.UserAccounts', 'ApiToken') IS NULL
        ALTER TABLE dbo.UserAccounts ADD ApiToken NVARCHAR(250) NULL;

    IF COL_LENGTH('dbo.UserAccounts', 'ApiTokenExpiresAt') IS NULL
        ALTER TABLE dbo.UserAccounts ADD ApiTokenExpiresAt DATETIME2 NULL;


    /* ============================================================
       2. PET PROFILE
       ============================================================ */

    IF COL_LENGTH('dbo.PetProfiles', 'BirthDate') IS NULL
        ALTER TABLE dbo.PetProfiles ADD BirthDate DATE NULL;


    /* ============================================================
       3. CLINIC VERIFICATION FIELDS
       ============================================================ */

    IF COL_LENGTH('dbo.ClinicTenants', 'SubscriptionPlan') IS NULL
        ALTER TABLE dbo.ClinicTenants ADD SubscriptionPlan NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.ClinicTenants', 'IsVerified') IS NULL
        ALTER TABLE dbo.ClinicTenants
        ADD IsVerified BIT NOT NULL
            CONSTRAINT DF_ClinicTenants_IsVerified_Final DEFAULT (0) WITH VALUES;

    IF COL_LENGTH('dbo.ClinicTenants', 'VerifiedAt') IS NULL
        ALTER TABLE dbo.ClinicTenants ADD VerifiedAt DATETIME2 NULL;

    IF COL_LENGTH('dbo.ClinicTenants', 'VerificationFee') IS NULL
        ALTER TABLE dbo.ClinicTenants ADD VerificationFee DECIMAL(10,2) NULL;

    IF COL_LENGTH('dbo.ClinicTenants', 'VerificationReference') IS NULL
        ALTER TABLE dbo.ClinicTenants ADD VerificationReference NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.ClinicTenants', 'VerificationStatus') IS NULL
        ALTER TABLE dbo.ClinicTenants
        ADD VerificationStatus NVARCHAR(30) NOT NULL
            CONSTRAINT DF_ClinicTenants_VerificationStatus_Final
            DEFAULT ('Unverified') WITH VALUES;


    /* ============================================================
       4. COMMUNITY EDITED STATE
       ============================================================ */

    IF COL_LENGTH('dbo.CommunityPosts', 'IsEdited') IS NULL
        ALTER TABLE dbo.CommunityPosts
        ADD IsEdited BIT NOT NULL
            CONSTRAINT DF_CommunityPosts_IsEdited_Final DEFAULT (0) WITH VALUES;


    /* ============================================================
       5. TRANSACTIONS / MOCK PAYMENT AUDIT
       ============================================================ */

    IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Transactions
        (
            Id              INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_Transactions PRIMARY KEY,
            UserId          INT NULL,
            ClinicId        INT NULL,
            Type            NVARCHAR(60) NOT NULL,
            Amount          DECIMAL(10,2) NOT NULL,
            Reference       NVARCHAR(100) NOT NULL,
            PaymentMethod   NVARCHAR(30) NULL,
            Status          NVARCHAR(30) NOT NULL
                CONSTRAINT DF_Transactions_Status_Final DEFAULT ('Paid'),
            PaidAt          DATETIME2 NOT NULL
                CONSTRAINT DF_Transactions_PaidAt_Final DEFAULT (SYSDATETIME())
        );
    END
    ELSE
    BEGIN
        IF COL_LENGTH('dbo.Transactions', 'PaymentMethod') IS NULL
            ALTER TABLE dbo.Transactions ADD PaymentMethod NVARCHAR(30) NULL;

        IF COL_LENGTH('dbo.Transactions', 'Status') IS NULL
            ALTER TABLE dbo.Transactions
            ADD Status NVARCHAR(30) NOT NULL
                CONSTRAINT DF_Transactions_Status_Final DEFAULT ('Paid') WITH VALUES;

        IF COL_LENGTH('dbo.Transactions', 'PaidAt') IS NULL
            ALTER TABLE dbo.Transactions
            ADD PaidAt DATETIME2 NOT NULL
                CONSTRAINT DF_Transactions_PaidAt_Final DEFAULT (SYSDATETIME()) WITH VALUES;
    END;

    IF COL_LENGTH('dbo.Transactions', 'Status') IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM sys.default_constraints dc
           INNER JOIN sys.columns c
               ON c.object_id = dc.parent_object_id
              AND c.column_id = dc.parent_column_id
           WHERE dc.parent_object_id = OBJECT_ID('dbo.Transactions')
             AND c.name = 'Status'
       )
    BEGIN
        ALTER TABLE dbo.Transactions
        ADD CONSTRAINT DF_Transactions_Status_Final_Existing
            DEFAULT ('Paid') FOR Status;
    END;

    IF COL_LENGTH('dbo.Transactions', 'PaidAt') IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM sys.default_constraints dc
           INNER JOIN sys.columns c
               ON c.object_id = dc.parent_object_id
              AND c.column_id = dc.parent_column_id
           WHERE dc.parent_object_id = OBJECT_ID('dbo.Transactions')
             AND c.name = 'PaidAt'
       )
    BEGIN
        ALTER TABLE dbo.Transactions
        ADD CONSTRAINT DF_Transactions_PaidAt_Final_Existing
            DEFAULT (SYSDATETIME()) FOR PaidAt;
    END;

    /*
       Dynamic SQL is intentional here. Status may have been added earlier in
       this same batch, and SQL Server can otherwise raise "Invalid column name"
       during batch compilation before ALTER TABLE gets a chance to run.
    */
    EXEC sys.sp_executesql N'
        UPDATE dbo.Transactions
           SET Status = ''Paid''
         WHERE Status IS NULL OR LTRIM(RTRIM(Status)) = '''';
    ';


    /* ============================================================
       6. FINAL SUBSCRIPTIONS
          Premium         = 150 / 3 months
          Verified Seller = 50 / month
          Verified Clinic = 50 / month
       ============================================================ */

    IF OBJECT_ID('dbo.Subscriptions', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Subscriptions
        (
            Id                  INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_Subscriptions PRIMARY KEY,

            UserId              INT NULL,
            ClinicId            INT NULL,

            SubscriptionType    NVARCHAR(60) NOT NULL,
            Status              NVARCHAR(30) NOT NULL
                CONSTRAINT DF_Subscriptions_Status DEFAULT ('Active'),

            Reference           NVARCHAR(100) NOT NULL,
            StartsAt            DATETIME2 NOT NULL,
            ExpiresAt           DATETIME2 NOT NULL,
            CreatedAt           DATETIME2 NOT NULL
                CONSTRAINT DF_Subscriptions_CreatedAt DEFAULT (SYSDATETIME()),

            CONSTRAINT CK_Subscriptions_Subject
                CHECK (UserId IS NOT NULL OR ClinicId IS NOT NULL),

            CONSTRAINT CK_Subscriptions_Dates
                CHECK (ExpiresAt > StartsAt),

            CONSTRAINT CK_Subscriptions_Type
                CHECK
                (
                    SubscriptionType IN
                    (
                        'PremiumSubscription',
                        'SellerSubscription',
                        'ClinicSubscription'
                    )
                ),

            CONSTRAINT FK_Subscriptions_UserAccounts
                FOREIGN KEY (UserId)
                REFERENCES dbo.UserAccounts(Id),

            CONSTRAINT FK_Subscriptions_ClinicTenants
                FOREIGN KEY (ClinicId)
                REFERENCES dbo.ClinicTenants(Id)
        );
    END;


    /* ============================================================
       6B. BACKFILL ACTIVE LEGACY PREMIUM INTO SUBSCRIPTIONS
       Preserves historical transaction rows exactly as they are.
       This only mirrors currently-active legacy Premium entitlement
       into the new subscription table so Web/Admin status stays
       synchronized.
       ============================================================ */

    INSERT INTO dbo.Subscriptions
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
    SELECT
        u.Id,
        NULL,
        'PremiumSubscription',
        'Active',
        CASE
            WHEN NULLIF(LTRIM(RTRIM(u.PremiumReference)), '') IS NOT NULL
                THEN u.PremiumReference
            ELSE CONCAT('LEGACY-PREM-', u.Id, '-', FORMAT(u.PremiumActivatedAt, 'yyyyMMddHHmmss'))
        END,
        u.PremiumActivatedAt,
        DATEADD(MONTH, 3, u.PremiumActivatedAt),
        u.PremiumActivatedAt
    FROM dbo.UserAccounts u
    WHERE ISNULL(u.IsPremium, 0) = 1
      AND u.PremiumActivatedAt IS NOT NULL
      AND DATEADD(MONTH, 3, u.PremiumActivatedAt) >= SYSDATETIME()
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Subscriptions s
          WHERE s.UserId = u.Id
            AND s.SubscriptionType = 'PremiumSubscription'
            AND s.Status = 'Active'
            AND s.ExpiresAt >= SYSDATETIME()
      )
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Subscriptions s2
          WHERE s2.Reference =
              CASE
                  WHEN NULLIF(LTRIM(RTRIM(u.PremiumReference)), '') IS NOT NULL
                      THEN u.PremiumReference
                  ELSE CONCAT('LEGACY-PREM-', u.Id, '-', FORMAT(u.PremiumActivatedAt, 'yyyyMMddHHmmss'))
              END
      );


    /* ============================================================
       7. CLINIC LISTING REQUESTS
       ============================================================ */

    IF OBJECT_ID('dbo.ClinicListingRequests', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ClinicListingRequests
        (
            Id                      INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_ClinicListingRequests PRIMARY KEY,

            UserId                  INT NOT NULL,
            RepresentativeName      NVARCHAR(150) NOT NULL,
            RelationshipToClinic    NVARCHAR(80) NOT NULL,
            ClinicName              NVARCHAR(180) NOT NULL,
            Address                 NVARCHAR(250) NOT NULL,
            City                    NVARCHAR(120) NOT NULL,
            ContactNumber           NVARCHAR(30) NOT NULL,
            ClinicEmail             NVARCHAR(254) NOT NULL,
            ServicesOffered         NVARCHAR(1000) NOT NULL,
            Message                 NVARCHAR(1000) NOT NULL
                CONSTRAINT DF_ClinicListingRequests_Message DEFAULT (''),

            Status                  NVARCHAR(30) NOT NULL
                CONSTRAINT DF_ClinicListingRequests_Status DEFAULT ('Pending'),

            CreatedClinicId         INT NULL,
            ReviewedByUserId        INT NULL,
            ReviewNote              NVARCHAR(1000) NOT NULL
                CONSTRAINT DF_ClinicListingRequests_ReviewNote DEFAULT (''),

            CreatedAt               DATETIME2 NOT NULL
                CONSTRAINT DF_ClinicListingRequests_CreatedAt DEFAULT (SYSDATETIME()),
            ReviewedAt              DATETIME2 NULL,

            CONSTRAINT FK_ClinicListingRequests_UserAccounts
                FOREIGN KEY (UserId)
                REFERENCES dbo.UserAccounts(Id),

            CONSTRAINT FK_ClinicListingRequests_CreatedClinic
                FOREIGN KEY (CreatedClinicId)
                REFERENCES dbo.ClinicTenants(Id),

            CONSTRAINT FK_ClinicListingRequests_ReviewedBy
                FOREIGN KEY (ReviewedByUserId)
                REFERENCES dbo.UserAccounts(Id),

            CONSTRAINT CK_ClinicListingRequests_Status
                CHECK (Status IN ('Pending', 'Approved', 'Rejected'))
        );
    END;


    /* ============================================================
       8. CLINIC CLAIM REQUESTS
       ============================================================ */

    IF OBJECT_ID('dbo.ClinicClaimRequests', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ClinicClaimRequests
        (
            Id                      INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_ClinicClaimRequests PRIMARY KEY,

            UserId                  INT NOT NULL,
            ClinicId                INT NOT NULL,

            RepresentativeName      NVARCHAR(150) NOT NULL,
            RelationshipToClinic    NVARCHAR(80) NOT NULL,
            ContactNumber           NVARCHAR(30) NOT NULL,
            SupportingDetails       NVARCHAR(1200) NOT NULL,

            Status                  NVARCHAR(30) NOT NULL
                CONSTRAINT DF_ClinicClaimRequests_Status DEFAULT ('Pending'),

            ReviewedByUserId        INT NULL,
            ReviewNote              NVARCHAR(1000) NOT NULL
                CONSTRAINT DF_ClinicClaimRequests_ReviewNote DEFAULT (''),

            CreatedAt               DATETIME2 NOT NULL
                CONSTRAINT DF_ClinicClaimRequests_CreatedAt DEFAULT (SYSDATETIME()),
            ReviewedAt              DATETIME2 NULL,

            CONSTRAINT FK_ClinicClaimRequests_UserAccounts
                FOREIGN KEY (UserId)
                REFERENCES dbo.UserAccounts(Id),

            CONSTRAINT FK_ClinicClaimRequests_ClinicTenants
                FOREIGN KEY (ClinicId)
                REFERENCES dbo.ClinicTenants(Id),

            CONSTRAINT FK_ClinicClaimRequests_ReviewedBy
                FOREIGN KEY (ReviewedByUserId)
                REFERENCES dbo.UserAccounts(Id),

            CONSTRAINT CK_ClinicClaimRequests_Status
                CHECK (Status IN ('Pending', 'Approved', 'Rejected'))
        );
    END;


    /* ============================================================
       9. INDEXES
       ============================================================ */

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Subscriptions')
          AND name = 'IX_Subscriptions_User_Type_Status_Expiry'
    )
    BEGIN
        CREATE INDEX IX_Subscriptions_User_Type_Status_Expiry
        ON dbo.Subscriptions
        (
            UserId,
            SubscriptionType,
            Status,
            ExpiresAt
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Subscriptions')
          AND name = 'IX_Subscriptions_Clinic_Type_Status_Expiry'
    )
    BEGIN
        CREATE INDEX IX_Subscriptions_Clinic_Type_Status_Expiry
        ON dbo.Subscriptions
        (
            ClinicId,
            SubscriptionType,
            Status,
            ExpiresAt
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.Subscriptions')
          AND name = 'UX_Subscriptions_Reference'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_Subscriptions_Reference
        ON dbo.Subscriptions(Reference);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ClinicListingRequests')
          AND name = 'IX_ClinicListingRequests_Status_CreatedAt'
    )
    BEGIN
        CREATE INDEX IX_ClinicListingRequests_Status_CreatedAt
        ON dbo.ClinicListingRequests(Status, CreatedAt DESC);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.ClinicClaimRequests')
          AND name = 'IX_ClinicClaimRequests_Status_CreatedAt'
    )
    BEGIN
        CREATE INDEX IX_ClinicClaimRequests_Status_CreatedAt
        ON dbo.ClinicClaimRequests(Status, CreatedAt DESC);
    END;


    /* ============================================================
       10. NORMALIZE EXISTING DATA
       ============================================================ */

    /*
       These columns can also be introduced by this migration, so use dynamic
       SQL to avoid SQL Server compile-time column resolution in the same batch.
    */
    EXEC sys.sp_executesql N'
        UPDATE dbo.ClinicTenants
           SET VerificationStatus = ''Unverified''
         WHERE VerificationStatus IS NULL
            OR LTRIM(RTRIM(VerificationStatus)) = '''';
    ';

    EXEC sys.sp_executesql N'
        UPDATE dbo.CommunityPosts
           SET IsEdited = 0
         WHERE IsEdited IS NULL;
    ';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO


/* ================================================================
   11. ROLE CONSTRAINT DIAGNOSTIC
   The final claim workflow promotes an approved claimant to
   Role = 'Clinic Owner'. If this returns a role CHECK constraint,
   send the result before testing claim approval.
   ================================================================ */

SELECT
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('dbo.UserAccounts')
  AND cc.definition LIKE '%Role%';
GO


/* ================================================================
   12. VERIFY FINAL STRUCTURES
   ================================================================ */

SELECT
    'Subscriptions' AS Item,
    CASE WHEN OBJECT_ID('dbo.Subscriptions', 'U') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END AS Result
UNION ALL
SELECT
    'ClinicListingRequests',
    CASE WHEN OBJECT_ID('dbo.ClinicListingRequests', 'U') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT
    'ClinicClaimRequests',
    CASE WHEN OBJECT_ID('dbo.ClinicClaimRequests', 'U') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT
    'Transactions.PaymentMethod',
    CASE WHEN COL_LENGTH('dbo.Transactions', 'PaymentMethod') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT
    'Transactions.Status',
    CASE WHEN COL_LENGTH('dbo.Transactions', 'Status') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT
    'CommunityPosts.IsEdited',
    CASE WHEN COL_LENGTH('dbo.CommunityPosts', 'IsEdited') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT
    'UserAccounts.MobileNumber',
    CASE WHEN COL_LENGTH('dbo.UserAccounts', 'MobileNumber') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT
    'PetProfiles.BirthDate',
    CASE WHEN COL_LENGTH('dbo.PetProfiles', 'BirthDate') IS NOT NULL
         THEN 'OK' ELSE 'MISSING' END;
GO


/* ================================================================
   13. QUICK DATA CHECKS
   ================================================================ */

SELECT TOP (20)
    Id,
    SubscriptionType,
    UserId,
    ClinicId,
    Status,
    Reference,
    StartsAt,
    ExpiresAt
FROM dbo.Subscriptions
ORDER BY CreatedAt DESC;

SELECT TOP (20)
    Id,
    Type,
    Amount,
    Reference,
    PaymentMethod,
    Status,
    PaidAt
FROM dbo.Transactions
ORDER BY PaidAt DESC;
GO


/* ============================================================
   FINAL MARKETPLACE E-COMMERCE ADD-ON
   Cart itself is session-scoped in the web app.
   These tables persist demo orders, verified reviews and rewards.
   Safe to re-run.
   ============================================================ */

IF OBJECT_ID('dbo.MarketplaceOrders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MarketplaceOrders
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BuyerUserId INT NOT NULL,
        SellerUserId INT NOT NULL,
        Reference NVARCHAR(80) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_MarketplaceOrders_Status DEFAULT ('Purchase Requested'),
        Subtotal DECIMAL(10,2) NOT NULL,
        VoucherDiscount DECIMAL(10,2) NOT NULL CONSTRAINT DF_MarketplaceOrders_Discount DEFAULT (0),
        Total DECIMAL(10,2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MarketplaceOrders_CreatedAt DEFAULT (SYSDATETIME()),
        CompletedAt DATETIME2 NULL,
        CONSTRAINT FK_MarketplaceOrders_Buyer FOREIGN KEY (BuyerUserId) REFERENCES dbo.UserAccounts(Id),
        CONSTRAINT FK_MarketplaceOrders_Seller FOREIGN KEY (SellerUserId) REFERENCES dbo.UserAccounts(Id)
    );
END;
GO

IF OBJECT_ID('dbo.MarketplaceOrderItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MarketplaceOrderItems
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderId INT NOT NULL,
        ListingId INT NOT NULL,
        ListingTitle NVARCHAR(150) NOT NULL,
        Price DECIMAL(10,2) NOT NULL,
        CONSTRAINT FK_MarketplaceOrderItems_Order FOREIGN KEY (OrderId) REFERENCES dbo.MarketplaceOrders(Id),
        CONSTRAINT FK_MarketplaceOrderItems_Listing FOREIGN KEY (ListingId) REFERENCES dbo.MarketplaceListings(Id)
    );
END;
GO

IF OBJECT_ID('dbo.MarketplaceReviews', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MarketplaceReviews
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderItemId INT NOT NULL,
        ListingId INT NOT NULL,
        BuyerUserId INT NOT NULL,
        Rating INT NOT NULL,
        Comment NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MarketplaceReviews_CreatedAt DEFAULT (SYSDATETIME()),
        RewardIssued BIT NOT NULL CONSTRAINT DF_MarketplaceReviews_RewardIssued DEFAULT (0),
        CONSTRAINT CK_MarketplaceReviews_Rating CHECK (Rating BETWEEN 1 AND 5),
        CONSTRAINT FK_MarketplaceReviews_OrderItem FOREIGN KEY (OrderItemId) REFERENCES dbo.MarketplaceOrderItems(Id),
        CONSTRAINT FK_MarketplaceReviews_Listing FOREIGN KEY (ListingId) REFERENCES dbo.MarketplaceListings(Id),
        CONSTRAINT FK_MarketplaceReviews_Buyer FOREIGN KEY (BuyerUserId) REFERENCES dbo.UserAccounts(Id)
    );
END;
GO

IF OBJECT_ID('dbo.MarketplaceVouchers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MarketplaceVouchers
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        Code NVARCHAR(60) NOT NULL,
        Amount DECIMAL(10,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_MarketplaceVouchers_Status DEFAULT ('Active'),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MarketplaceVouchers_CreatedAt DEFAULT (SYSDATETIME()),
        ExpiresAt DATETIME2 NOT NULL,
        UsedAt DATETIME2 NULL,
        UsedOrderId INT NULL,
        CONSTRAINT FK_MarketplaceVouchers_User FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts(Id),
        CONSTRAINT FK_MarketplaceVouchers_Order FOREIGN KEY (UsedOrderId) REFERENCES dbo.MarketplaceOrders(Id)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_MarketplaceOrders_Reference'
      AND object_id = OBJECT_ID('dbo.MarketplaceOrders')
)
    CREATE UNIQUE INDEX UX_MarketplaceOrders_Reference ON dbo.MarketplaceOrders(Reference);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_MarketplaceReviews_OrderItem_Buyer'
      AND object_id = OBJECT_ID('dbo.MarketplaceReviews')
)
    CREATE UNIQUE INDEX UX_MarketplaceReviews_OrderItem_Buyer ON dbo.MarketplaceReviews(OrderItemId, BuyerUserId);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_MarketplaceVouchers_Code'
      AND object_id = OBJECT_ID('dbo.MarketplaceVouchers')
)
    CREATE UNIQUE INDEX UX_MarketplaceVouchers_Code ON dbo.MarketplaceVouchers(Code);
GO

/* ============================================================
   COMMUNITY COMMENTS
   Guests may comment with a session/display name; registered
   users are linked to UserAccounts.
   ============================================================ */

IF OBJECT_ID('dbo.CommunityComments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommunityComments
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PostId INT NOT NULL,
        UserId INT NULL,
        AuthorName NVARCHAR(120) NOT NULL,
        Body NVARCHAR(300) NOT NULL,
        IsGuest BIT NOT NULL CONSTRAINT DF_CommunityComments_IsGuest DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CommunityComments_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_CommunityComments_Post FOREIGN KEY (PostId) REFERENCES dbo.CommunityPosts(Id),
        CONSTRAINT FK_CommunityComments_User FOREIGN KEY (UserId) REFERENCES dbo.UserAccounts(Id)
    );
END;
GO

SELECT 'MarketplaceOrders' AS FinalFeature,
       CASE WHEN OBJECT_ID('dbo.MarketplaceOrders','U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS Result
UNION ALL
SELECT 'MarketplaceOrderItems',
       CASE WHEN OBJECT_ID('dbo.MarketplaceOrderItems','U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT 'MarketplaceReviews',
       CASE WHEN OBJECT_ID('dbo.MarketplaceReviews','U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT 'MarketplaceVouchers',
       CASE WHEN OBJECT_ID('dbo.MarketplaceVouchers','U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL
SELECT 'CommunityComments',
       CASE WHEN OBJECT_ID('dbo.CommunityComments','U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END;
GO
