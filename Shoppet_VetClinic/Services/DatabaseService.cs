using Microsoft.Data.SqlClient;
using Shoppet_VetClinic.Models;

namespace Shoppet_VetClinic.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Server=LAPTOP-UA0MGH5P;Database=Shoppet_VetClinic_DB;Trusted_Connection=True;TrustServerCertificate=True;";
        private List<CartItem> _cart = new();

        public List<UserAccount> GetAllUsers()
        {
            var users = new List<UserAccount>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT Id, FullName, Email, PasswordHash, Role, CreatedAt FROM UserAccounts", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new UserAccount
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    Password = reader.GetString(3),
                    Role = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5)
                });
            }
            return users;
        }

        public UserAccount? RegisterUser(string fullName, string email, string password, string role)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var checkCmd = new SqlCommand("SELECT COUNT(1) FROM UserAccounts WHERE Email = @Email", conn);
            checkCmd.Parameters.AddWithValue("@Email", email);
            if ((int)checkCmd.ExecuteScalar() > 0) return null;

            var insertCmd = new SqlCommand("INSERT INTO UserAccounts (FullName, Email, PasswordHash, Role) OUTPUT INSERTED.Id, INSERTED.CreatedAt VALUES (@FullName, @Email, @Password, @Role)", conn);
            insertCmd.Parameters.AddWithValue("@FullName", fullName);
            insertCmd.Parameters.AddWithValue("@Email", email);
            insertCmd.Parameters.AddWithValue("@Password", password);
            insertCmd.Parameters.AddWithValue("@Role", role);

            using var reader = insertCmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserAccount { Id = reader.GetInt32(0), FullName = fullName, Email = email, Password = password, Role = role, CreatedAt = reader.GetDateTime(1) };
            }
            return null;
        }

        public UserAccount? AuthenticateUser(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT Id, FullName, Email, PasswordHash, Role, CreatedAt FROM UserAccounts WHERE Email = @Email AND PasswordHash = @Password", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Password", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserAccount
                {
                    Id = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    Email = reader.GetString(2),
                    Password = reader.GetString(3),
                    Role = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5)
                };
            }
            return null;
        }

        public List<ClinicTenant> GetAllClinics()
        {
            var clinics = new List<ClinicTenant>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT Id, ClinicName, Branch, Address, PrcLicenseNo, PtrNumber, SubscriptionPlan FROM ClinicTenants", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                clinics.Add(new ClinicTenant
                {
                    Id = reader.GetInt32(0),
                    ClinicName = reader.GetString(1),
                    Branch = reader.GetString(2),
                    Address = reader.GetString(3),
                    PrcLicenseNo = reader.GetString(4),
                    PtrNumber = reader.GetString(5),
                    SubscriptionPlan = reader.GetString(6)
                });
            }
            return clinics;
        }

        public List<PetProduct> GetAllProducts()
        {
            var products = new List<PetProduct>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT Id, Name, Category, Price, Stock, PetSize, Description, ImageSource FROM PetProducts", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add(new PetProduct
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Category = reader.GetString(2),
                    Price = reader.GetDecimal(3),
                    Stock = reader.GetInt32(4),
                    PetSize = reader.GetString(5),
                    Description = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    ImageSource = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                });
            }
            return products;
        }

        public void AddProduct(PetProduct product)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("INSERT INTO PetProducts (Name, Category, Price, Stock, PetSize, Description, ImageSource) VALUES (@Name, @Category, @Price, @Stock, @PetSize, @Description, @ImageSource)", conn);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@Stock", product.Stock);
            cmd.Parameters.AddWithValue("@PetSize", product.PetSize);
            cmd.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ImageSource", product.ImageSource ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void UpdateProduct(PetProduct product)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("UPDATE PetProducts SET Name=@Name, Category=@Category, Price=@Price, Stock=@Stock, PetSize=@PetSize, Description=@Description WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", product.Id);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@Stock", product.Stock);
            cmd.Parameters.AddWithValue("@PetSize", product.PetSize);
            cmd.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void DeleteProduct(int productId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("DELETE FROM PetProducts WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", productId);
            cmd.ExecuteNonQuery();
        }

        public void BookAppointment(ClinicAppointment appointment)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("INSERT INTO ClinicAppointments (ClinicId, OwnerName, PetName, ServiceType, AppointmentDate, ContactNumber) VALUES (@ClinicId, @OwnerName, @PetName, @ServiceType, @AppointmentDate, @ContactNumber)", conn);
            cmd.Parameters.AddWithValue("@ClinicId", appointment.ClinicId);
            cmd.Parameters.AddWithValue("@OwnerName", appointment.OwnerName);
            cmd.Parameters.AddWithValue("@PetName", appointment.PetName);
            cmd.Parameters.AddWithValue("@ServiceType", appointment.ServiceType);
            cmd.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
            cmd.Parameters.AddWithValue("@ContactNumber", appointment.ContactNumber);
            cmd.ExecuteNonQuery();
        }

        public List<ClinicAppointment> GetAppointments()
        {
            var appointments = new List<ClinicAppointment>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT Id, ClinicId, OwnerName, PetName, ServiceType, AppointmentDate, ContactNumber FROM ClinicAppointments", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                appointments.Add(new ClinicAppointment
                {
                    Id = reader.GetInt32(0),
                    ClinicId = reader.GetInt32(1),
                    OwnerName = reader.GetString(2),
                    PetName = reader.GetString(3),
                    ServiceType = reader.GetString(4),
                    AppointmentDate = reader.GetDateTime(5),
                    ContactNumber = reader.GetString(6)
                });
            }
            return appointments;
        }

        public void CancelAppointment(int appointmentId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("DELETE FROM ClinicAppointments WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", appointmentId);
            cmd.ExecuteNonQuery();
        }

        public List<CartItem> GetCartItems() => _cart;

        public void AddOrIncrementCart(int productId)
        {
            var products = GetAllProducts();
            var product = products.FirstOrDefault(p => p.Id == productId);
            if (product == null || product.Stock <= 0) return;

            var existing = _cart.FirstOrDefault(c => c.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _cart.Add(new CartItem { ProductId = product.Id, Name = product.Name, Price = product.Price, Quantity = 1, ImageSource = product.ImageSource });
            }
        }

        public void RemoveFromCart(int productId)
        {
            var item = _cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null) _cart.Remove(item);
        }

        public string CreateClickAndCollectOrder(int clinicId, string customerName, string contact, string fulfillment, List<CartItem> items)
        {
            string token = "CLAIM-" + new Random().Next(1000, 9999);
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            decimal totalAmount = items.Sum(i => i.Subtotal);
            var orderCmd = new SqlCommand("INSERT INTO Orders (ClinicId, CustomerName, ContactNumber, FulfillmentMethod, ClaimToken, TotalAmount) VALUES (@ClinicId, @CustomerName, @Contact, @Fulfillment, @Token, @Total)", conn);
            orderCmd.Parameters.AddWithValue("@ClinicId", clinicId);
            orderCmd.Parameters.AddWithValue("@CustomerName", customerName);
            orderCmd.Parameters.AddWithValue("@Contact", contact);
            orderCmd.Parameters.AddWithValue("@Fulfillment", fulfillment);
            orderCmd.Parameters.AddWithValue("@Token", token);
            orderCmd.Parameters.AddWithValue("@Total", totalAmount);
            orderCmd.ExecuteNonQuery();

            foreach (var item in items)
            {
                var stockCmd = new SqlCommand("UPDATE PetProducts SET Stock = Stock - @Qty WHERE Id = @Id", conn);
                stockCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                stockCmd.Parameters.AddWithValue("@Id", item.ProductId);
                stockCmd.ExecuteNonQuery();
            }

            _cart.Clear();
            return token;
        }
    }
}