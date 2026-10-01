using Shoppet_VetClinic.Models;
using Shoppet_VetClinic.Services;

namespace Shoppet_VetClinic.Api;

public static class MobileApiEndpoints
{
    public static void MapMobileApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");

        api.MapPost("/auth/token", (MobileLoginRequest request, MobileTokenService tokens) =>
        {
            var user = tokens.Authenticate(request.Email, request.Password);
            if (user is null)
                return Results.Unauthorized();

            var token = tokens.Issue(user);
            return Results.Ok(new MobileTokenResponse(
                token,
                DateTimeOffset.UtcNow.AddDays(30),
                ToUser(user)));
        });

        api.MapPost("/auth/revoke", (HttpContext context, MobileTokenService tokens) =>
        {
            var user = GetUser(context, tokens);
            return user is null
                ? Results.Unauthorized()
                : tokens.Revoke(user) ? Results.NoContent() : Results.NotFound();
        });

        var community = api.MapGroup("/community");

        community.MapGet("/posts", (HttpContext context, MobileTokenService tokens, CommunityService service, int? count) =>
        {
            var requested = Math.Clamp(count ?? 50, 1, 100);
            return Results.Ok(service.GetRecentPosts(requested));
        });

        community.MapGet("/posts/{postId:int}/comments", (int postId, CommunityService service) =>
            postId <= 0
                ? Results.BadRequest("A valid post ID is required.")
                : Results.Ok(service.GetComments(postId)));

        community.MapPost("/posts", (HttpContext context, MobileTokenService tokens, CommunityService service, MobilePostRequest request) =>
        {
            var user = GetUser(context, tokens);
            var denied = RequirePetOwner(user);
            if (denied is not null)
                return denied;
            if (string.IsNullOrWhiteSpace(request.Caption) || request.Caption.Trim().Length > 500)
                return Results.BadRequest("Caption must contain between 1 and 500 characters.");

            var postId = service.CreatePost(user!.Id, request.PetId, request.Caption.Trim());
            return Results.Created($"/api/v1/community/posts/{postId}", new { id = postId });
        });

        community.MapPost("/posts/{postId:int}/comments", (HttpContext context, MobileTokenService tokens, CommunityService service, int postId, MobileCommentRequest request) =>
        {
            var user = GetUser(context, tokens);
            var denied = RequirePetOwner(user);
            if (denied is not null)
                return denied;

            var saved = service.AddComment(
                postId,
                user!.Id,
                user.FullName,
                request.Body,
                isGuest: false,
                request.ParentCommentId);

            return saved ? Results.Ok() : Results.BadRequest("The comment or reply could not be saved.");
        });

        community.MapPost("/comments/{commentId:int}/like", (HttpContext context, MobileTokenService tokens, CommunityService service, int commentId) =>
        {
            var user = GetUser(context, tokens);
            var denied = RequirePetOwner(user);
            if (denied is not null)
                return denied;

            service.ToggleCommentLike(commentId, user!.Id);
            return Results.NoContent();
        });

        var marketplace = api.MapGroup("/marketplace");

        marketplace.MapGet("/listings", (MarketplaceService service) =>
            Results.Ok(service.GetAllListings()));

        marketplace.MapGet("/listings/{listingId:int}", (MarketplaceService service, int listingId) =>
        {
            var listing = service.GetListingById(listingId);
            return listing is null || string.Equals(listing.Status, "Deleted", StringComparison.OrdinalIgnoreCase)
                ? Results.NotFound()
                : Results.Ok(listing);
        });

        marketplace.MapPost("/listings", (HttpContext context, MobileTokenService tokens, CommerceService commerce, MarketplaceService service, MarketplaceListingRequest request) =>
        {
            var user = GetUser(context, tokens);
            var denied = RequirePetOwner(user);
            if (denied is not null)
                return denied;
            if (!commerce.IsSellerActive(user!.Id))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (string.IsNullOrWhiteSpace(request.Title) || request.Price < 0)
                return Results.BadRequest("A valid title and non-negative price are required.");

            var id = service.CreateListing(new MarketplaceListing
            {
                SellerUserId = user.Id,
                Title = request.Title.Trim(),
                Category = request.Category.Trim(),
                ItemCondition = request.ItemCondition.Trim(),
                Price = request.Price,
                Description = request.Description?.Trim() ?? string.Empty,
                Location = request.Location?.Trim() ?? string.Empty
            }, user.Id);

            return Results.Created($"/api/v1/marketplace/listings/{id}", new { id });
        });

        marketplace.MapPut("/listings/{listingId:int}", (HttpContext context, MobileTokenService tokens, CommerceService commerce, MarketplaceService service, int listingId, MarketplaceListingRequest request) =>
        {
            var user = GetUser(context, tokens);
            var denied = RequirePetOwner(user);
            if (denied is not null)
                return denied;
            if (!commerce.IsSellerActive(user!.Id))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var existing = service.GetListingById(listingId);
            if (existing is null || existing.SellerUserId != user.Id)
                return Results.NotFound();

            existing.Title = request.Title.Trim();
            existing.Category = request.Category.Trim();
            existing.ItemCondition = request.ItemCondition.Trim();
            existing.Price = request.Price;
            existing.Description = request.Description?.Trim() ?? string.Empty;
            existing.Location = request.Location?.Trim() ?? string.Empty;

            return service.UpdateListing(existing, user.Id)
                ? Results.NoContent()
                : Results.Conflict("The listing could not be updated.");
        });

        marketplace.MapDelete("/listings/{listingId:int}", (HttpContext context, MobileTokenService tokens, CommerceService commerce, MarketplaceService service, int listingId) =>
        {
            var user = GetUser(context, tokens);
            var denied = RequirePetOwner(user);
            if (denied is not null)
                return denied;
            if (!commerce.IsSellerActive(user!.Id))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            return service.DeleteListing(listingId, user.Id, user.Id)
                ? Results.NoContent()
                : Results.NotFound();
        });

        marketplace.MapPost("/orders", (HttpContext context, MobileTokenService tokens, MarketplaceService listings, MarketplaceOrderService orders, MarketplaceOrderRequest request) =>
        {
            var user = GetUser(context, tokens);
            var denied = RequirePetOwner(user);
            if (denied is not null)
                return denied;
            if (request.ListingIds is null || request.ListingIds.Count == 0 || request.ListingIds.Distinct().Count() != request.ListingIds.Count)
                return Results.BadRequest("At least one unique listing is required.");

            var selected = request.ListingIds
                .Select(listings.GetListingById)
                .ToList();

            if (selected.Any(x => x is null || !string.Equals(x.Status, "Available", StringComparison.OrdinalIgnoreCase)))
                return Results.Conflict("One or more listings are no longer available.");

            var sellerItems = selected!
                .Select(x => new MarketplaceCartItem
                {
                    ListingId = x!.Id,
                    SellerUserId = x.SellerUserId,
                    SellerName = x.SellerName,
                    Title = x.Title,
                    Price = x.Price,
                    ImageUrl = x.ImageUrl,
                    Status = x.Status
                })
                .ToList();

            if (sellerItems.Select(x => x.SellerUserId).Distinct().Count() != 1 || sellerItems[0].SellerUserId != request.SellerUserId)
                return Results.BadRequest("All order items must belong to the selected seller.");

            var reference = orders.CreateOrder(
                user!.Id,
                request.SellerUserId,
                sellerItems,
                request.PaymentMethod,
                request.VoucherId);

            return Results.Created($"/api/v1/marketplace/orders/{reference}", new { reference });
        });

        marketplace.MapGet("/orders", (HttpContext context, MobileTokenService tokens, MarketplaceOrderService orders) =>
        {
            var user = GetUser(context, tokens);
            var denied = RequirePetOwner(user);
            return denied is not null
                ? denied
                : Results.Ok(orders.GetBuyerOrders(user!.Id));
        });
    }

    private static UserAccount? GetUser(HttpContext context, MobileTokenService tokens)
    {
        var header = context.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header[7..].Trim()
            : null;
        return tokens.Validate(token);
    }

    private static bool IsPetOwner(UserAccount? user) =>
        user is not null &&
        (string.Equals(user.Role.Trim(), "Pet Owner", StringComparison.OrdinalIgnoreCase)
         || string.Equals(user.Role.Trim(), "PetOwner", StringComparison.OrdinalIgnoreCase));

    private static IResult? RequirePetOwner(UserAccount? user) =>
        user is null
            ? Results.Unauthorized()
            : IsPetOwner(user)
                ? null
                : Results.Forbid();

    private static MobileUser ToUser(UserAccount user) =>
        new(user.Id, user.FullName, user.Role, user.IsPremium);
}

public sealed record MobileLoginRequest(string Email, string Password);
public sealed record MobileTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, MobileUser User);
public sealed record MobileUser(int Id, string FullName, string Role, bool IsPremium);
public sealed record MobilePostRequest(int? PetId, string Caption);
public sealed record MobileCommentRequest(string Body, int? ParentCommentId);
