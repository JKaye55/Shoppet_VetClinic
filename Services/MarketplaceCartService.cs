using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services
{
    public class MarketplaceCartService
    {
        private readonly List<MarketplaceCartItem> _items = new();

        public event Action? Changed;

        public IReadOnlyList<MarketplaceCartItem> Items => _items;

        public int Count => _items.Count;

        public decimal Total => _items.Sum(x => x.Price);

        public bool Contains(int listingId) =>
            _items.Any(x => x.ListingId == listingId);

        public bool Add(MarketplaceListing listing)
        {
            if (!string.Equals(listing.Status, "Available", StringComparison.OrdinalIgnoreCase))
                return false;

            if (Contains(listing.Id))
                return false;

            _items.Add(new MarketplaceCartItem
            {
                ListingId = listing.Id,
                SellerUserId = listing.SellerUserId,
                SellerName = listing.SellerName,
                Title = listing.Title,
                Price = listing.Price,
                ImageUrl = listing.ImageUrl,
                Status = listing.Status
            });

            Changed?.Invoke();
            return true;
        }

        public void Remove(int listingId)
        {
            _items.RemoveAll(x => x.ListingId == listingId);
            Changed?.Invoke();
        }

        public void RemoveMany(IEnumerable<int> listingIds)
        {
            var ids = listingIds.ToHashSet();
            _items.RemoveAll(x => ids.Contains(x.ListingId));
            Changed?.Invoke();
        }

        public void Clear()
        {
            _items.Clear();
            Changed?.Invoke();
        }
    }
}
