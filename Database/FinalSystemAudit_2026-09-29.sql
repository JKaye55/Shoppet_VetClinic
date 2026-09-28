/*
    ShoppetCare Final System Audit
    Date: 2026-09-29

    PURPOSE
    -------
    Read-only finals checklist for the database schema that supports the
    current Web scope. It does not delete, update, or insert production data.

    Run this after Database/FinalMigration_2026-09-28.sql.
*/

SET NOCOUNT ON;

PRINT '============================================================';
PRINT 'SHOPPETCARE FINAL DATABASE AUDIT';
PRINT '============================================================';

/* ============================================================
   1. REQUIRED TABLES
   ============================================================ */

DECLARE @RequiredTables TABLE
(
    TableName SYSNAME NOT NULL
);

INSERT INTO @RequiredTables (TableName)
VALUES
('UserAccounts'),
('PetProfiles'),
('PetHealthRecords'),
('ClinicTenants'),
('CommunityPosts'),
('CommunityLikes'),
('CommunityComments'),
('MarketplaceListings'),
('MarketplaceOrders'),
('MarketplaceOrderItems'),
('MarketplaceReviews'),
('MarketplaceVouchers'),
('Subscriptions'),
('Transactions'),
('ClinicListingRequests'),
('ClinicClaimRequests'),
('Notifications');

SELECT
    r.TableName,
    CASE
        WHEN OBJECT_ID('dbo.' + r.TableName, 'U') IS NOT NULL THEN 'OK'
        ELSE 'MISSING'
    END AS Result
FROM @RequiredTables r
ORDER BY r.TableName;


/* ============================================================
   2. REQUIRED COLUMNS
   ============================================================ */

DECLARE @RequiredColumns TABLE
(
    TableName SYSNAME NOT NULL,
    ColumnName SYSNAME NOT NULL
);

INSERT INTO @RequiredColumns (TableName, ColumnName)
VALUES
('UserAccounts','Id'),
('UserAccounts','FullName'),
('UserAccounts','Email'),
('UserAccounts','Role'),
('UserAccounts','MobileNumber'),
('UserAccounts','IsPremium'),
('UserAccounts','PremiumActivatedAt'),
('UserAccounts','PremiumReference'),
('PetProfiles','Id'),
('PetProfiles','UserId'),
('PetProfiles','PetName'),
('PetProfiles','BirthDate'),
('PetProfiles','CardId'),
('ClinicTenants','Id'),
('ClinicTenants','ClinicName'),
('ClinicTenants','IsVerified'),
('ClinicTenants','VerificationStatus'),
('CommunityPosts','Id'),
('CommunityPosts','UserId'),
('CommunityPosts','IsEdited'),
('CommunityComments','PostId'),
('CommunityComments','UserId'),
('CommunityComments','AuthorName'),
('CommunityComments','IsGuest'),
('MarketplaceListings','Id'),
('MarketplaceListings','SellerUserId'),
('MarketplaceListings','Status'),
('MarketplaceOrders','BuyerUserId'),
('MarketplaceOrders','SellerUserId'),
('MarketplaceOrders','Reference'),
('MarketplaceOrders','PaymentMethod'),
('MarketplaceOrders','Status'),
('MarketplaceOrderItems','OrderId'),
('MarketplaceOrderItems','ListingId'),
('MarketplaceReviews','OrderItemId'),
('MarketplaceReviews','ListingId'),
('MarketplaceReviews','BuyerUserId'),
('MarketplaceReviews','Rating'),
('MarketplaceVouchers','UserId'),
('MarketplaceVouchers','Code'),
('MarketplaceVouchers','Amount'),
('MarketplaceVouchers','Status'),
('Subscriptions','UserId'),
('Subscriptions','Type'),
('Subscriptions','Status'),
('Subscriptions','Reference'),
('Transactions','UserId'),
('Transactions','TransactionType'),
('Transactions','Amount'),
('Transactions','Reference'),
('Transactions','PaymentMethod'),
('Transactions','Status'),
('ClinicListingRequests','RequestedByUserId'),
('ClinicListingRequests','Status'),
('ClinicClaimRequests','RequestedByUserId'),
('ClinicClaimRequests','ClinicId'),
('ClinicClaimRequests','Status');

SELECT
    r.TableName,
    r.ColumnName,
    CASE
        WHEN COL_LENGTH('dbo.' + r.TableName, r.ColumnName) IS NOT NULL THEN 'OK'
        ELSE 'MISSING'
    END AS Result
FROM @RequiredColumns r
ORDER BY r.TableName, r.ColumnName;


/* ============================================================
   3. DUPLICATE / UNIQUENESS CHECKS
   ============================================================ */

IF OBJECT_ID('dbo.UserAccounts','U') IS NOT NULL
BEGIN
    PRINT 'Duplicate user emails (expect 0 rows):';
    SELECT Email, COUNT(*) AS DuplicateCount
    FROM dbo.UserAccounts
    GROUP BY Email
    HAVING COUNT(*) > 1;
END;

IF OBJECT_ID('dbo.MarketplaceOrders','U') IS NOT NULL
BEGIN
    PRINT 'Duplicate marketplace order references (expect 0 rows):';
    SELECT Reference, COUNT(*) AS DuplicateCount
    FROM dbo.MarketplaceOrders
    GROUP BY Reference
    HAVING COUNT(*) > 1;
END;

IF OBJECT_ID('dbo.MarketplaceVouchers','U') IS NOT NULL
BEGIN
    PRINT 'Duplicate marketplace voucher codes (expect 0 rows):';
    SELECT Code, COUNT(*) AS DuplicateCount
    FROM dbo.MarketplaceVouchers
    GROUP BY Code
    HAVING COUNT(*) > 1;
END;


/* ============================================================
   4. ORPHAN CHECKS
   ============================================================ */

IF OBJECT_ID('dbo.PetProfiles','U') IS NOT NULL
   AND OBJECT_ID('dbo.UserAccounts','U') IS NOT NULL
BEGIN
    PRINT 'PetProfiles without users (expect 0):';
    SELECT COUNT(*) AS OrphanPetProfiles
    FROM dbo.PetProfiles p
    LEFT JOIN dbo.UserAccounts u ON u.Id = p.UserId
    WHERE u.Id IS NULL;
END;

IF OBJECT_ID('dbo.CommunityComments','U') IS NOT NULL
   AND OBJECT_ID('dbo.CommunityPosts','U') IS NOT NULL
BEGIN
    PRINT 'CommunityComments without posts (expect 0):';
    SELECT COUNT(*) AS OrphanComments
    FROM dbo.CommunityComments c
    LEFT JOIN dbo.CommunityPosts p ON p.Id = c.PostId
    WHERE p.Id IS NULL;
END;

IF OBJECT_ID('dbo.MarketplaceOrderItems','U') IS NOT NULL
   AND OBJECT_ID('dbo.MarketplaceOrders','U') IS NOT NULL
BEGIN
    PRINT 'MarketplaceOrderItems without orders (expect 0):';
    SELECT COUNT(*) AS OrphanOrderItems
    FROM dbo.MarketplaceOrderItems i
    LEFT JOIN dbo.MarketplaceOrders o ON o.Id = i.OrderId
    WHERE o.Id IS NULL;
END;


/* ============================================================
   5. GUEST COMMENT IDENTITY CHECK
   Guest comments should be stored as AuthorName = Guest
   and should not point to a registered user.
   ============================================================ */

IF OBJECT_ID('dbo.CommunityComments','U') IS NOT NULL
BEGIN
    PRINT 'Invalid guest comment identities (expect 0 rows):';
    SELECT Id, PostId, UserId, AuthorName, IsGuest, CreatedAt
    FROM dbo.CommunityComments
    WHERE IsGuest = 1
      AND
      (
          UserId IS NOT NULL
          OR LTRIM(RTRIM(ISNULL(AuthorName,''))) <> 'Guest'
      );
END;


/* ============================================================
   6. MARKETPLACE REVIEW ELIGIBILITY / DUPLICATES
   ============================================================ */

IF OBJECT_ID('dbo.MarketplaceReviews','U') IS NOT NULL
BEGIN
    PRINT 'Review ratings outside 1-5 (expect 0 rows):';
    SELECT Id, OrderItemId, BuyerUserId, Rating
    FROM dbo.MarketplaceReviews
    WHERE Rating < 1 OR Rating > 5;

    PRINT 'Duplicate reviews per purchased item + buyer (expect 0 rows):';
    SELECT OrderItemId, BuyerUserId, COUNT(*) AS ReviewCount
    FROM dbo.MarketplaceReviews
    GROUP BY OrderItemId, BuyerUserId
    HAVING COUNT(*) > 1;
END;


/* ============================================================
   7. ACTIVE SUBSCRIPTIONS
   ============================================================ */

IF OBJECT_ID('dbo.Subscriptions','U') IS NOT NULL
BEGIN
    PRINT 'Current active subscriptions:';
    EXEC sp_executesql N'
        SELECT
            Id,
            UserId,
            ClinicId,
            Type,
            Status,
            Reference,
            StartsAt,
            ExpiresAt
        FROM dbo.Subscriptions
        WHERE Status = ''Active''
        ORDER BY ExpiresAt DESC;';
END;


/* ============================================================
   8. FINAL COUNTS
   Dynamic SQL is used so a missing optional table does not
   prevent the audit from finishing.
   ============================================================ */

DECLARE @CountSql NVARCHAR(MAX) = N'';

SELECT @CountSql = @CountSql +
    CASE WHEN LEN(@CountSql) > 0 THEN N' UNION ALL ' ELSE N'' END +
    N'SELECT N''' + REPLACE(TableName,'''','''''') + N''' AS Entity, COUNT(*) AS TotalRows FROM dbo.' +
    QUOTENAME(TableName)
FROM @RequiredTables
WHERE OBJECT_ID('dbo.' + TableName, 'U') IS NOT NULL;

IF LEN(@CountSql) > 0
    EXEC sp_executesql @CountSql;


/* ============================================================
   9. EXPECTED FINAL TRANSACTION TYPES
   Historical PremiumUpgrade rows may remain and are valid history.
   New flows should use the final transaction types below.
   ============================================================ */

IF OBJECT_ID('dbo.Transactions','U') IS NOT NULL
BEGIN
    PRINT 'Transaction types currently present:';
    SELECT TransactionType, COUNT(*) AS TotalRows
    FROM dbo.Transactions
    GROUP BY TransactionType
    ORDER BY TransactionType;
END;

PRINT '============================================================';
PRINT 'AUDIT COMPLETE - review any MISSING results or non-zero';
PRINT 'orphan/duplicate/invalid checks before final documentation.';
PRINT '============================================================';
