using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;
namespace Shoppet_VetClinic.Services;
public class MarketplaceCartService(IConfiguration configuration,AuthService auth)
{
    private string ConnectionString=>configuration.GetConnectionString("ShoppetDb")!;
    private int Actor=>auth.IsPetOwner?auth.CurrentUser?.Id??0:0;
    private readonly List<MarketplaceCartItem> _guestItems = new();
    public event Action? Changed;
    public IReadOnlyList<MarketplaceCartItem> Items
    {
        get
        {
            if (Actor == 0)
                return _guestItems.ToList();

            var list=new List<MarketplaceCartItem>();
            using var c=new SqlConnection(ConnectionString);c.Open();
            using var q=new SqlCommand(@"SELECT m.Id,m.SellerUserId,u.FullName,m.Title,m.Price,ISNULL(m.ImageUrl,''),m.Status
            FROM MarketplaceCartItems i JOIN MarketplaceCart cart ON cart.Id=i.CartId
            JOIN MarketplaceListings m ON m.Id=i.MarketplaceListingId JOIN UserAccounts u ON u.Id=m.SellerUserId
            WHERE cart.UserId=@User AND m.Status<>'Deleted' ORDER BY i.Id DESC",c);
            q.Parameters.AddWithValue("@User",Actor);using var r=q.ExecuteReader();
            while(r.Read())list.Add(new MarketplaceCartItem {ListingId=r.GetInt32(0),SellerUserId=r.GetInt32(1),SellerName=r.GetString(2),Title=r.GetString(3),Price=r.GetDecimal(4),ImageUrl=r.GetString(5),Status=r.GetString(6)});
            return list;
        }
    }
    public int Count=>Items.Count;
    public decimal Total=>Items.Sum(x=>x.Price);
    public bool Contains(int id)=>Items.Any(x=>x.ListingId==id);
    public bool Add(MarketplaceListing listing)
    {
        if (string.Equals(listing.Status, "Deleted", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(listing.Status, "Available", StringComparison.OrdinalIgnoreCase))
            return false;

        if (Actor == 0)
        {
            if (_guestItems.Any(item => item.ListingId == listing.Id))
                return false;

            _guestItems.Add(new MarketplaceCartItem
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

        if(listing.SellerUserId==Actor)return false;
        using var c=new SqlConnection(ConnectionString);c.Open();using var tx=c.BeginTransaction(System.Data.IsolationLevel.Serializable);
        using var q=new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM MarketplaceCart WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@User) INSERT INTO MarketplaceCart(UserId) VALUES(@User);
            DECLARE @Cart INT=(SELECT Id FROM MarketplaceCart WHERE UserId=@User);
            IF EXISTS(SELECT 1 FROM MarketplaceListings WHERE Id=@Listing AND SellerUserId<>@User AND Status IN('Available','Active'))
               AND NOT EXISTS(SELECT 1 FROM MarketplaceCartItems WHERE CartId=@Cart AND MarketplaceListingId=@Listing)
                INSERT INTO MarketplaceCartItems(CartId,MarketplaceListingId,Quantity,UserId,ListingId,AddedAt) VALUES(@Cart,@Listing,1,@User,@Listing,SYSDATETIME());",c,tx);
        q.Parameters.AddWithValue("@User",Actor);q.Parameters.AddWithValue("@Listing",listing.Id);var changed=q.ExecuteNonQuery()>0;tx.Commit();Changed?.Invoke();return changed;
    }
    public void Remove(int id)
    {
        if (Actor == 0)
        {
            if (_guestItems.RemoveAll(item => item.ListingId == id) > 0)
                Changed?.Invoke();
            return;
        }

        Mutate("DELETE i FROM MarketplaceCartItems i JOIN MarketplaceCart c ON c.Id=i.CartId WHERE c.UserId=@User AND i.MarketplaceListingId=@Id",id);
    }
    public void RemoveMany(IEnumerable<int> ids){foreach(var id in ids)Remove(id);}
    public void Clear()
    {
        if (Actor == 0)
        {
            if (_guestItems.Count == 0)
                return;

            _guestItems.Clear();
            Changed?.Invoke();
            return;
        }

        Mutate("DELETE i FROM MarketplaceCartItems i JOIN MarketplaceCart c ON c.Id=i.CartId WHERE c.UserId=@User",0);
    }
    private void Mutate(string sql,int id){if(Actor==0)return;using var c=new SqlConnection(ConnectionString);c.Open();using var q=new SqlCommand(sql,c);q.Parameters.AddWithValue("@User",Actor);q.Parameters.AddWithValue("@Id",id);q.ExecuteNonQuery();Changed?.Invoke();}
}
