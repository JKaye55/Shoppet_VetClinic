namespace Shoppet_VetClinic.Api;

public sealed record MarketplaceListingRequest(
    string Title,
    string Category,
    string ItemCondition,
    decimal Price,
    string Description,
    string Location);

public sealed record MarketplaceOrderRequest(
    int SellerUserId,
    IReadOnlyCollection<int> ListingIds,
    string PaymentMethod,
    int? VoucherId);
