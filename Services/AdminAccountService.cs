using Microsoft.Data.SqlClient;
namespace Shoppet_VetClinic.Services;
public class AdminAccountService(IConfiguration config,AuthService auth,DatabaseService db)
{
 SqlConnection Open(){if(!auth.IsAdmin)throw new UnauthorizedAccessException();var c=new SqlConnection(config.GetConnectionString("ShoppetDb"));c.Open();return c;}
 public List<AdminAccount> Users(){using var c=Open();using var q=new SqlCommand("SELECT Id,FullName,Email,Role,ISNULL(MobileNumber,''),IsDisabled,ISNULL(IsPremium,0) FROM UserAccounts ORDER BY FullName",c);using var r=q.ExecuteReader();var rows=new List<AdminAccount>();while(r.Read())rows.Add(new(){Id=r.GetInt32(0),Name=r.GetString(1),Email=r.GetString(2),Role=r.GetString(3),Phone=r.GetString(4),Disabled=r.GetBoolean(5),Premium=r.GetBoolean(6)});return rows;}
 public void Create(string name,string email,string phone,string password,string role){if(!auth.IsAdmin)throw new UnauthorizedAccessException();role=role?.Trim()??string.Empty;if(role is not ("Pet Owner" or "Admin"))throw new ArgumentException("Choose Pet Owner or Admin.");var error=AuthService.ValidateFullName(name)??AuthService.ValidateNewPassword(password);if(error is not null)throw new ArgumentException(error);if(!AuthService.IsValidEmailAddress(email))throw new ArgumentException("Enter a valid email.");if(db.RegisterUser(name,phone,AuthService.NormalizeEmail(email),password,role) is null)throw new ArgumentException("This email is already registered.");}
 public void Save(AdminAccount x)
 {
  if(x.Id==auth.CurrentUser?.Id)throw new ArgumentException("Use your account page to change your own profile. Your own role and active status are protected.");
  if(x.Role is not ("Pet Owner" or "Admin")||string.IsNullOrWhiteSpace(x.Name)||x.Name.Length>150||x.Phone.Length>30)throw new ArgumentException("Enter valid account details and choose Pet Owner or Admin.");
  using var c=Open();using var tx=c.BeginTransaction(System.Data.IsolationLevel.Serializable);
  using var q=new SqlCommand("UPDATE UserAccounts SET FullName=@Name,MobileNumber=@Phone,Role=@Role,IsDisabled=@Disabled,ApiToken=NULL,ApiTokenExpiresAt=NULL WHERE Id=@Id",c,tx);q.Parameters.AddWithValue("@Id",x.Id);q.Parameters.AddWithValue("@Name",x.Name.Trim());q.Parameters.AddWithValue("@Phone",x.Phone);q.Parameters.AddWithValue("@Role",x.Role);q.Parameters.AddWithValue("@Disabled",x.Disabled);q.ExecuteNonQuery();tx.Commit();
 }
}
public class AdminAccount {public int Id{get;set;}public string Name{get;set;}="";public string Email{get;set;}="";public string Role{get;set;}="Pet Owner";public string Phone{get;set;}="";public bool Disabled{get;set;}public bool Premium{get;set;}}
