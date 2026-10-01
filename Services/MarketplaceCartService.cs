using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;
namespace Shoppet_VetClinic.Services;
public class MarketplaceCartService(IConfiguration configuration,AuthService auth)
{
    private string ConnectionString=>configuration.GetConnectionString("ShoppetDb")!;
    private int Actor=>auth.IsPetOwner?auth.CurrentUser?.Id??0:0;
    public event Action? Changed;
    public IReadOnlyList<MarketplaceCartItem> Items
    {
        get
        {
            var list=new List<MarketplaceCartItem>();if(Actor==0)return list;
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
        if(Actor==0 || listing.SellerUserId==Actor)return false;
        using var c=new SqlConnection(ConnectionString);c.Open();using var tx=c.BeginTransaction(System.Data.IsolationLevel.Serializable);
        using var q=new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM MarketplaceCart WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@User) INSERT INTO MarketplaceCart(UserId) VALUES(@User);
            DECLARE @Cart INT=(SELECT Id FROM MarketplaceCart WHERE UserId=@User);
            IF EXISTS(SELECT 1 FROM MarketplaceListings WHERE Id=@Listing AND SellerUserId<>@User AND Status IN('Available','Active'))
               AND NOT EXISTS(SELECT 1 FROM MarketplaceCartItems WHERE CartId=@Cart AND MarketplaceListingId=@Listing)
                INSERT INTO MarketplaceCartItems(CartId,MarketplaceListingId,Quantity) VALUES(@Cart,@Listing,1);",c,tx);
        q.Parameters.AddWithValue("@User",Actor);q.Parameters.AddWithValue("@Listing",listing.Id);var changed=q.ExecuteNonQuery()>0;tx.Commit();Changed?.Invoke();return changed;
    }
    public void Remove(int id)=>Mutate("DELETE i FROM MarketplaceCartItems i JOIN MarketplaceCart c ON c.Id=i.CartId WHERE c.UserId=@User AND i.MarketplaceListingId=@Id",id);
    public void RemoveMany(IEnumerable<int> ids){foreach(var id in ids)Remove(id);}
    public void Clear()=>Mutate("DELETE i FROM MarketplaceCartItems i JOIN MarketplaceCart c ON c.Id=i.CartId WHERE c.UserId=@User",0);
    private void Mutate(string sql,int id){if(Actor==0)return;using var c=new SqlConnection(ConnectionString);c.Open();using var q=new SqlCommand(sql,c);q.Parameters.AddWithValue("@User",Actor);q.Parameters.AddWithValue("@Id",id);q.ExecuteNonQuery();Changed?.Invoke();}
}
