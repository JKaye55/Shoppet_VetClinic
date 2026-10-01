using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;
using System.Data;

namespace Shoppet_VetClinic.Services
{
    public class MarketplaceOrderService
    {
        private readonly string _connectionString;
        private readonly NotificationService _notifications;

        public MarketplaceOrderService(IConfiguration configuration, NotificationService notifications)
        {
            _connectionString = configuration.GetConnectionString("ShoppetDb")
                ?? throw new InvalidOperationException("Connection string 'ShoppetDb' was not found.");
            _notifications = notifications;
        }

        public string CreateOrder(
            int buyerUserId,
            int sellerUserId,
            IReadOnlyCollection<MarketplaceCartItem> cartItems,
            string paymentMethod,
            int? voucherId = null)
        {
            if(buyerUserId==sellerUserId)throw new InvalidOperationException("You cannot buy your own listing.");
            if (cartItems.Select(x=>x.ListingId).Distinct().Count()!=cartItems.Count)throw new InvalidOperationException("A listing can only be purchased once.");
            if (cartItems.Count == 0)
                throw new InvalidOperationException("There are no items to checkout.");

            if (cartItems.Any(x => x.SellerUserId != sellerUserId))
                throw new InvalidOperationException("Checkout one seller at a time.");

            var allowed = new[] { "Cash on Meet-up", "GCash - Demo", "Maya - Demo", "Credit Card", "Card", "Mock Payment Gateway", "Debit Card" };
            if (!allowed.Contains(paymentMethod, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Choose a valid demo payment method.");

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                decimal subtotal = 0m;
                var verifiedItems = new List<(int Id, string Title, decimal Price)>();

                foreach (var cart in cartItems)
                {
                    using var check = new SqlCommand(@"
                        SELECT Id, Title, Price
                        FROM MarketplaceListings WITH (UPDLOCK, ROWLOCK)
                        WHERE Id = @Id
                          AND SellerUserId = @SellerUserId
                          AND Status = 'Available';", conn, tx);

                    check.Parameters.AddWithValue("@Id", cart.ListingId);
                    check.Parameters.AddWithValue("@SellerUserId", sellerUserId);

                    using var reader = check.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException($"{cart.Title} is no longer available.");

                    var id = reader.GetInt32(0);
                    var title = reader.GetString(1);
                    var price = reader.GetDecimal(2);
                    reader.Close();

                    verifiedItems.Add((id, title, price));
                    subtotal += price;
                }

                decimal discount = 0m;
                if (voucherId.HasValue)
                {
                    using var voucher = new SqlCommand(@"
                        SELECT Amount
                        FROM MarketplaceVouchers WITH (UPDLOCK, ROWLOCK)
                        WHERE Id = @Id
                          AND UserId = @UserId
                          AND Status = 'Active'
                          AND UsedAt IS NULL
                          AND ExpiresAt >= SYSDATETIME();", conn, tx);

                    voucher.Parameters.AddWithValue("@Id", voucherId.Value);
                    voucher.Parameters.AddWithValue("@UserId", buyerUserId);

                    var value = voucher.ExecuteScalar();
                    if (value is null)
                        throw new InvalidOperationException("The selected voucher is no longer available.");

                    discount = Math.Min(Convert.ToDecimal(value), subtotal);
                }

                var total = Math.Max(0m, subtotal - discount);
                var reference = $"MKT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

                using var order = new SqlCommand(@"
                    INSERT INTO MarketplaceOrders
                    (
                        BuyerUserId, SellerUserId, Reference, PaymentMethod,
                        Status, Subtotal, VoucherDiscount, Total, CreatedAt, CompletedAt
                    )
                    OUTPUT INSERTED.Id
                    VALUES
                    (
                        @BuyerUserId, @SellerUserId, @Reference, @PaymentMethod,
                        'Completed', @Subtotal, @Discount, @Total, SYSDATETIME(), SYSDATETIME()
                    );", conn, tx);

                order.Parameters.AddWithValue("@BuyerUserId", buyerUserId);
                order.Parameters.AddWithValue("@SellerUserId", sellerUserId);
                order.Parameters.AddWithValue("@Reference", reference);
                order.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                AddMoney(order, "@Subtotal", subtotal);
                AddMoney(order, "@Discount", discount);
                AddMoney(order, "@Total", total);

                var orderId = Convert.ToInt32(order.ExecuteScalar());

                foreach (var item in verifiedItems)
                {
                    using var line = new SqlCommand(@"
                        INSERT INTO MarketplaceOrderItems
                        (OrderId, ListingId, ListingTitle, Price)
                        VALUES (@OrderId, @ListingId, @Title, @Price);", conn, tx);

                    line.Parameters.AddWithValue("@OrderId", orderId);
                    line.Parameters.AddWithValue("@ListingId", item.Id);
                    line.Parameters.AddWithValue("@Title", item.Title);
                    AddMoney(line, "@Price", item.Price);
                    line.ExecuteNonQuery();

                    using var reserve = new SqlCommand(@"
                        UPDATE MarketplaceListings
                        SET Status = 'Sold', UpdatedAt=SYSDATETIME()
                        WHERE Id = @Id AND Status = 'Available';", conn, tx);
                    reserve.Parameters.AddWithValue("@Id", item.Id);
                    reserve.ExecuteNonQuery();
                    using var clear=new SqlCommand("DELETE i FROM MarketplaceCartItems i JOIN MarketplaceCart c ON c.Id=i.CartId WHERE c.UserId=@User AND i.MarketplaceListingId=@Id",conn,tx);
                    clear.Parameters.AddWithValue("@User",buyerUserId);clear.Parameters.AddWithValue("@Id",item.Id);clear.ExecuteNonQuery();
                }

                if (voucherId.HasValue)
                {
                    using var useVoucher = new SqlCommand(@"
                        UPDATE MarketplaceVouchers
                        SET Status = 'Used', UsedAt = SYSDATETIME(), UsedOrderId = @OrderId
                        WHERE Id = @Id AND UserId = @UserId;", conn, tx);
                    useVoucher.Parameters.AddWithValue("@OrderId", orderId);
                    useVoucher.Parameters.AddWithValue("@Id", voucherId.Value);
                    useVoucher.Parameters.AddWithValue("@UserId", buyerUserId);
                    useVoucher.ExecuteNonQuery();
                }

                using(var audit=new SqlCommand("INSERT INTO Transactions(UserId,Type,Amount,Reference,PaymentMethod,Status,PaidAt) VALUES(@User,'MarketplacePurchase',@Amount,@Ref,@Method,'SimulatedPaid',SYSDATETIME())",conn,tx))
                {audit.Parameters.AddWithValue("@User",buyerUserId);AddMoney(audit,"@Amount",total);audit.Parameters.AddWithValue("@Ref",reference);audit.Parameters.AddWithValue("@Method",paymentMethod);audit.ExecuteNonQuery();}
                tx.Commit();
                return reference;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public List<MarketplaceOrder> GetBuyerOrders(int buyerUserId) =>
            GetOrders("o.BuyerUserId = @UserId", buyerUserId);

        public List<MarketplaceOrder> GetSellerOrders(int sellerUserId) =>
            GetOrders("o.SellerUserId = @UserId", sellerUserId);

        private List<MarketplaceOrder> GetOrders(string predicate, int userId)
        {
            var orders = new List<MarketplaceOrder>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand($@"
                SELECT
                    o.Id, o.BuyerUserId, o.SellerUserId, o.Reference,
                    o.PaymentMethod, o.Status, o.Subtotal, o.VoucherDiscount,
                    o.Total, o.CreatedAt, o.CompletedAt,
                    ISNULL(b.FullName, 'Pet Owner') AS BuyerName,
                    ISNULL(s.FullName, 'Seller') AS SellerName,
                    ISNULL(b.Email, '') AS BuyerEmail,
                    ISNULL(b.Mobile, '') AS BuyerPhone
                FROM MarketplaceOrders o
                INNER JOIN UserAccounts b ON b.Id = o.BuyerUserId
                INNER JOIN UserAccounts s ON s.Id = o.SellerUserId
                WHERE {predicate}
                ORDER BY o.CreatedAt DESC;", conn);

            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                orders.Add(new MarketplaceOrder
                {
                    Id = reader.GetInt32(0),
                    BuyerUserId = reader.GetInt32(1),
                    SellerUserId = reader.GetInt32(2),
                    Reference = reader.GetString(3),
                    PaymentMethod = reader.GetString(4),
                    Status = reader.GetString(5),
                    Subtotal = reader.GetDecimal(6),
                    VoucherDiscount = reader.GetDecimal(7),
                    Total = reader.GetDecimal(8),
                    CreatedAt = reader.GetDateTime(9),
                    CompletedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    BuyerName = reader.GetString(11),
                    SellerName = reader.GetString(12),
                    BuyerEmail = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    BuyerPhone = reader.IsDBNull(14) ? string.Empty : reader.GetString(14)
                });
            }
            reader.Close();

            foreach (var order in orders)
                order.Items = GetOrderItems(conn, order.Id, order.BuyerUserId);

            return orders;
        }

        public (int TotalOrders, int UniqueBuyers, decimal TotalAmount) GetSellerOrderStats(int sellerUserId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    COUNT(1) AS TotalOrders,
                    COUNT(DISTINCT BuyerUserId) AS UniqueBuyers,
                    ISNULL(SUM(Total), 0) AS TotalAmount
                FROM MarketplaceOrders
                WHERE SellerUserId = @SellerUserId;", conn);
            cmd.Parameters.AddWithValue("@SellerUserId", sellerUserId);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return (
                    r.GetInt32(0),
                    r.GetInt32(1),
                    r.GetDecimal(2)
                );
            }
            return (0, 0, 0m);
        }

        public (int TotalOrders, int UniqueBuyers, decimal TotalAmount) GetPlatformOrderStats()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    COUNT(1) AS TotalOrders,
                    COUNT(DISTINCT BuyerUserId) AS UniqueBuyers,
                    ISNULL(SUM(Total), 0) AS TotalAmount
                FROM MarketplaceOrders;", conn);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return (
                    r.GetInt32(0),
                    r.GetInt32(1),
                    r.GetDecimal(2)
                );
            }
            return (0, 0, 0m);
        }

        private static List<MarketplaceOrderItem> GetOrderItems(SqlConnection conn, int orderId, int buyerUserId)
        {
            var items = new List<MarketplaceOrderItem>();
            using var cmd = new SqlCommand(@"
                SELECT
                    oi.Id, oi.OrderId, oi.ListingId, oi.ListingTitle, oi.Price,
                    ISNULL(m.ImageUrl, '') AS ImageUrl,
                    CAST(CASE WHEN EXISTS
                    (
                        SELECT 1 FROM MarketplaceReviews r
                        WHERE r.OrderItemId = oi.Id AND r.BuyerUserId = @BuyerUserId
                    ) THEN 1 ELSE 0 END AS bit) AS HasReview
                FROM MarketplaceOrderItems oi
                LEFT JOIN MarketplaceListings m ON m.Id = oi.ListingId
                WHERE oi.OrderId = @OrderId;", conn);

            cmd.Parameters.AddWithValue("@OrderId", orderId);
            cmd.Parameters.AddWithValue("@BuyerUserId", buyerUserId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new MarketplaceOrderItem
                {
                    Id = reader.GetInt32(0),
                    OrderId = reader.GetInt32(1),
                    ListingId = reader.GetInt32(2),
                    ListingTitle = reader.GetString(3),
                    Price = reader.GetDecimal(4),
                    ImageUrl = reader.GetString(5),
                    HasReview = reader.GetBoolean(6)
                });
            }

            return items;
        }

        public bool CompleteOrder(int orderId, int sellerUserId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                int buyerUserId;
                string reference;

                using (var find = new SqlCommand(@"
                    SELECT BuyerUserId, Reference
                    FROM MarketplaceOrders WITH (UPDLOCK, ROWLOCK)
                    WHERE Id = @Id AND SellerUserId = @SellerUserId
                      AND Status = 'Purchase Requested';", conn, tx))
                {
                    find.Parameters.AddWithValue("@Id", orderId);
                    find.Parameters.AddWithValue("@SellerUserId", sellerUserId);

                    using var reader = find.ExecuteReader();
                    if (!reader.Read())
                        return false;

                    buyerUserId = reader.GetInt32(0);
                    reference = reader.GetString(1);
                }

                using (var update = new SqlCommand(@"
                    UPDATE MarketplaceOrders
                    SET Status = 'Completed', CompletedAt = SYSDATETIME()
                    WHERE Id = @Id;", conn, tx))
                {
                    update.Parameters.AddWithValue("@Id", orderId);
                    update.ExecuteNonQuery();
                }

                using (var sold = new SqlCommand(@"
                    UPDATE MarketplaceListings
                    SET Status = 'Sold'
                    WHERE Id IN
                    (
                        SELECT ListingId FROM MarketplaceOrderItems WHERE OrderId = @OrderId
                    );", conn, tx))
                {
                    sold.Parameters.AddWithValue("@OrderId", orderId);
                    sold.ExecuteNonQuery();
                }

                tx.Commit();

                _notifications.Custom(
                    buyerUserId,
                    "Marketplace order completed",
                    $"Order {reference} is complete. You can now leave an honest verified-purchase review and earn a ₱20 demo voucher.",
                    "/marketplace/orders",
                    "bi-star-fill");

                return true;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public bool CancelOrder(int orderId, int actorUserId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                int buyerId;
                int sellerId;

                using (var find = new SqlCommand(@"
                    SELECT BuyerUserId, SellerUserId
                    FROM MarketplaceOrders WITH (UPDLOCK, ROWLOCK)
                    WHERE Id = @Id AND Status = 'Purchase Requested'
                      AND (BuyerUserId = @Actor OR SellerUserId = @Actor);", conn, tx))
                {
                    find.Parameters.AddWithValue("@Id", orderId);
                    find.Parameters.AddWithValue("@Actor", actorUserId);
                    using var reader = find.ExecuteReader();
                    if (!reader.Read())
                        return false;
                    buyerId = reader.GetInt32(0);
                    sellerId = reader.GetInt32(1);
                }

                using (var update = new SqlCommand(
                    "UPDATE MarketplaceOrders SET Status = 'Cancelled' WHERE Id = @Id;", conn, tx))
                {
                    update.Parameters.AddWithValue("@Id", orderId);
                    update.ExecuteNonQuery();
                }

                using (var release = new SqlCommand(@"
                    UPDATE MarketplaceListings
                    SET Status = 'Available'
                    WHERE Status = 'Reserved'
                      AND Id IN (SELECT ListingId FROM MarketplaceOrderItems WHERE OrderId = @OrderId);", conn, tx))
                {
                    release.Parameters.AddWithValue("@OrderId", orderId);
                    release.ExecuteNonQuery();
                }

                tx.Commit();

                var other = actorUserId == buyerId ? sellerId : buyerId;
                _notifications.Custom(other, "Marketplace order cancelled",
                    "A marketplace purchase request was cancelled and its item(s) are available again.",
                    "/marketplace/orders", "bi-x-circle");

                return true;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public List<MarketplaceVoucher> GetActiveVouchers(int userId)
        {
            var result = new List<MarketplaceVoucher>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var expire = new SqlCommand(@"
                UPDATE MarketplaceVouchers
                SET Status = 'Expired'
                WHERE UserId = @UserId AND Status = 'Active'
                  AND ExpiresAt < SYSDATETIME();", conn);
            expire.Parameters.AddWithValue("@UserId", userId);
            expire.ExecuteNonQuery();

            using var cmd = new SqlCommand(@"
                SELECT Id, UserId, Code, Amount, Status, CreatedAt, ExpiresAt, UsedAt, UsedOrderId
                FROM MarketplaceVouchers
                WHERE UserId = @UserId AND Status = 'Active'
                  AND UsedAt IS NULL AND ExpiresAt >= SYSDATETIME()
                ORDER BY ExpiresAt;", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new MarketplaceVoucher
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Code = reader.GetString(2),
                    Amount = reader.GetDecimal(3),
                    Status = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5),
                    ExpiresAt = reader.GetDateTime(6),
                    UsedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    UsedOrderId = reader.IsDBNull(8) ? null : reader.GetInt32(8)
                });
            }

            return result;
        }

        public MarketplaceVoucher ClaimPromoCode(int userId, string promoCode, decimal amount = 50.00m)
        {
            var code = (promoCode ?? string.Empty).Trim().ToUpperInvariant();
            var expiresAt = DateTime.Now.AddDays(14);
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Check if user already has an active voucher with this code
            using var check = new SqlCommand(@"
                SELECT Id, UserId, Code, Amount, Status, CreatedAt, ExpiresAt, UsedAt, UsedOrderId
                FROM MarketplaceVouchers
                WHERE UserId = @UserId AND Code = @Code AND Status = 'Active' AND UsedAt IS NULL AND ExpiresAt >= SYSDATETIME();", conn);
            check.Parameters.AddWithValue("@UserId", userId);
            check.Parameters.AddWithValue("@Code", code);
            using var r = check.ExecuteReader();
            if (r.Read())
            {
                return new MarketplaceVoucher
                {
                    Id = r.GetInt32(0),
                    UserId = r.GetInt32(1),
                    Code = r.GetString(2),
                    Amount = r.GetDecimal(3),
                    Status = r.GetString(4),
                    CreatedAt = r.GetDateTime(5),
                    ExpiresAt = r.GetDateTime(6)
                };
            }
            r.Close();

            using var cmd = new SqlCommand(@"
                INSERT INTO MarketplaceVouchers (UserId, Code, Amount, Status, CreatedAt, ExpiresAt)
                OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.Code, INSERTED.Amount, INSERTED.Status, INSERTED.CreatedAt, INSERTED.ExpiresAt
                VALUES (@UserId, @Code, @Amount, 'Active', SYSDATETIME(), @ExpiresAt);", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Code", code);
            AddMoney(cmd, "@Amount", amount);
            cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new MarketplaceVoucher
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Code = reader.GetString(2),
                    Amount = reader.GetDecimal(3),
                    Status = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5),
                    ExpiresAt = reader.GetDateTime(6)
                };
            }
            throw new InvalidOperationException("Unable to create promo code voucher.");
        }

        public ReviewRewardResult AddReview(int orderItemId, int buyerUserId, int rating, string comment)
        {
            if (rating < 1 || rating > 5)
                return new ReviewRewardResult { Message = "Choose a rating from 1 to 5 stars." };

            comment = (comment ?? string.Empty).Trim();
            if (comment.Length > 500)
                return new ReviewRewardResult { Message = "Review comments are limited to 500 characters." };

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                int listingId;

                using (var verify = new SqlCommand(@"
                    SELECT oi.ListingId
                    FROM MarketplaceOrderItems oi
                    INNER JOIN MarketplaceOrders o ON o.Id = oi.OrderId
                    WHERE oi.Id = @OrderItemId
                      AND o.BuyerUserId = @BuyerUserId
                      AND o.Status = 'Completed'
                      AND NOT EXISTS
                      (
                          SELECT 1 FROM MarketplaceReviews r
                          WHERE r.OrderItemId = oi.Id AND r.BuyerUserId = @BuyerUserId
                      );", conn, tx))
                {
                    verify.Parameters.AddWithValue("@OrderItemId", orderItemId);
                    verify.Parameters.AddWithValue("@BuyerUserId", buyerUserId);
                    var value = verify.ExecuteScalar();
                    if (value is null)
                        return new ReviewRewardResult { Message = "This item is not eligible for another verified review." };
                    listingId = Convert.ToInt32(value);
                }

                using (var review = new SqlCommand(@"
                    INSERT INTO MarketplaceReviews
                    (OrderItemId, ListingId, BuyerUserId, Rating, Comment, CreatedAt, RewardIssued)
                    VALUES
                    (@OrderItemId, @ListingId, @BuyerUserId, @Rating, @Comment, SYSDATETIME(), 1);", conn, tx))
                {
                    review.Parameters.AddWithValue("@OrderItemId", orderItemId);
                    review.Parameters.AddWithValue("@ListingId", listingId);
                    review.Parameters.AddWithValue("@BuyerUserId", buyerUserId);
                    review.Parameters.AddWithValue("@Rating", rating);
                    review.Parameters.AddWithValue("@Comment",
                        string.IsNullOrWhiteSpace(comment) ? DBNull.Value : comment);
                    review.ExecuteNonQuery();
                }

                var code = $"SC-REVIEW-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
                var expiresAt = DateTime.Now.AddDays(30);

                using (var voucher = new SqlCommand(@"
                    INSERT INTO MarketplaceVouchers
                    (UserId, Code, Amount, Status, CreatedAt, ExpiresAt)
                    VALUES
                    (@UserId, @Code, 20.00, 'Active', SYSDATETIME(), @ExpiresAt);", conn, tx))
                {
                    voucher.Parameters.AddWithValue("@UserId", buyerUserId);
                    voucher.Parameters.AddWithValue("@Code", code);
                    voucher.Parameters.AddWithValue("@ExpiresAt", expiresAt);
                    voucher.ExecuteNonQuery();
                }

                tx.Commit();

                _notifications.Custom(
                    buyerUserId,
                    "₱20 review voucher earned",
                    $"Thanks for your honest review. Voucher {code} is valid for 30 days. Your rating did not affect eligibility.",
                    "/marketplace/orders",
                    "bi-ticket-perforated-fill");

                return new ReviewRewardResult
                {
                    Success = true,
                    Message = "Review submitted. Your ₱20 demo voucher is ready.",
                    VoucherCode = code,
                    VoucherAmount = 20m,
                    VoucherExpiresAt = expiresAt
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public List<MarketplaceReview> GetListingReviews(int listingId)
        {
            var result = new List<MarketplaceReview>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(@"
                SELECT r.Id, r.OrderItemId, r.ListingId, r.BuyerUserId,
                       ISNULL(u.FullName, 'Pet Owner') AS BuyerName,
                       r.Rating, ISNULL(r.Comment, ''), r.CreatedAt, r.RewardIssued
                FROM MarketplaceReviews r
                INNER JOIN UserAccounts u ON u.Id = r.BuyerUserId
                WHERE r.ListingId = @ListingId
                ORDER BY r.CreatedAt DESC;", conn);
            cmd.Parameters.AddWithValue("@ListingId", listingId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new MarketplaceReview
                {
                    Id = reader.GetInt32(0),
                    OrderItemId = reader.GetInt32(1),
                    ListingId = reader.GetInt32(2),
                    BuyerUserId = reader.GetInt32(3),
                    BuyerName = reader.GetString(4),
                    Rating = reader.GetInt32(5),
                    Comment = reader.GetString(6),
                    CreatedAt = reader.GetDateTime(7),
                    RewardIssued = reader.GetBoolean(8)
                });
            }
            return result;
        }

        private static void AddMoney(SqlCommand command, string name, decimal value)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal)
            {
                Precision = 10,
                Scale = 2,
                Value = value
            };
            command.Parameters.Add(p);
        }
    }
}
