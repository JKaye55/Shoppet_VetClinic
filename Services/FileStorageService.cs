using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.SqlClient;

namespace Shoppet_VetClinic.Services
{
    public class FileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        public string PublicBaseUrl => _config["PublicWebBaseUrl"] ?? "http://localhost:5253";

        private const long MaxFileSize =
            5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };


        public FileStorageService(
            IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _config=configuration;
        }


        private string GetUploadFolder(string category)
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var folder = Path.Combine(webRoot, "uploads", category);
            Directory.CreateDirectory(folder);
            return folder;
        }

        public async Task<string> SaveImageAsync(
            string category,
            int id,
            IBrowserFile file)
        {
            if (file is null)
                throw new ArgumentNullException(
                    nameof(file));

            if (file.Size > MaxFileSize)
            {
                throw new InvalidOperationException(
                    "The image must not exceed 5 MB.");
            }

            var extension =
                Path.GetExtension(file.Name)
                    .ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG, and WEBP images are allowed.");
            }

            var folder = GetUploadFolder(category);

            // Remove old versions.
            foreach (var oldExtension in AllowedExtensions)
            {
                var oldFile = Path.Combine(folder, $"{id}{oldExtension}");
                if (File.Exists(oldFile))
                {
                    try { File.Delete(oldFile); } catch { }
                }
            }

            var filePath = Path.Combine(folder, $"{id}{extension}");

            await using var inputStream = file.OpenReadStream(MaxFileSize);
            await using var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await inputStream.CopyToAsync(outputStream);

            var url = $"/uploads/{category}/{id}{extension}";
            if (category == "pets" || category == "users")
            {
                using var c = new SqlConnection(_config.GetConnectionString("ShoppetDb") ?? _config.GetConnectionString("DefaultConnection"));
                c.Open();
                using var q = new SqlCommand(category == "pets" ? "UPDATE PetProfiles SET PhotoUrl=@Url WHERE Id=@Id" : "UPDATE UserAccounts SET ProfilePicture=@Url WHERE Id=@Id", c);
                q.Parameters.AddWithValue("@Id", id);
                q.Parameters.AddWithValue("@Url", url);
                q.ExecuteNonQuery();
            }
            return url;
        }

        public async Task<string> SaveImageBytesAsync(
            string category,
            int id,
            byte[] bytes,
            string extension)
        {
            if (bytes is null || bytes.Length == 0)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length > MaxFileSize)
                throw new InvalidOperationException("The image must not exceed 5 MB.");

            extension = (extension ?? ".jpg").ToLowerInvariant();
            if (!extension.StartsWith(".")) extension = "." + extension;
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException("Only JPG, JPEG, PNG, and WEBP images are allowed.");

            var folder = GetUploadFolder(category);

            foreach (var oldExt in AllowedExtensions)
            {
                var oldFile = Path.Combine(folder, $"{id}{oldExt}");
                if (File.Exists(oldFile))
                {
                    try { File.Delete(oldFile); } catch { }
                }
            }

            var filePath = Path.Combine(folder, $"{id}{extension}");
            await File.WriteAllBytesAsync(filePath, bytes);

            var url = $"/uploads/{category}/{id}{extension}";
            if (category == "pets" || category == "users")
            {
                using var c = new SqlConnection(_config.GetConnectionString("ShoppetDb") ?? _config.GetConnectionString("DefaultConnection"));
                c.Open();
                using var q = new SqlCommand(category == "pets" ? "UPDATE PetProfiles SET PhotoUrl=@Url WHERE Id=@Id" : "UPDATE UserAccounts SET ProfilePicture=@Url WHERE Id=@Id", c);
                q.Parameters.AddWithValue("@Id", id);
                q.Parameters.AddWithValue("@Url", url);
                q.ExecuteNonQuery();
            }
            return url;
        }


        public string? GetImageUrl(
            string category,
            int id)
        {
            if(category=="pets" || category=="users") {
                using var c=new SqlConnection(_config.GetConnectionString("ShoppetDb") ?? _config.GetConnectionString("DefaultConnection"));c.Open();
                using var q=new SqlCommand(category=="pets"?"SELECT PhotoUrl FROM PetProfiles WHERE Id=@Id":"SELECT ProfilePicture FROM UserAccounts WHERE Id=@Id",c);
                q.Parameters.AddWithValue("@Id",id);var url=q.ExecuteScalar() as string;
                if(!string.IsNullOrWhiteSpace(url)) return url.StartsWith("data:") || url.StartsWith("http") || url.StartsWith("/")?url:"data:image/jpeg;base64,"+url;
            }
            var folder = GetUploadFolder(category);

            foreach (var extension in AllowedExtensions)
            {
                var filePath = Path.Combine(folder, $"{id}{extension}");
                if (File.Exists(filePath))
                {
                    var timestamp = File.GetLastWriteTimeUtc(filePath).Ticks;
                    return $"/uploads/{category}/{id}{extension}?v={timestamp}";
                }
            }

            return null;
        }

        public void DeleteImage(
            string category,
            int id)
        {
            var folder = GetUploadFolder(category);

            foreach (var extension in AllowedExtensions)
            {
                var filePath = Path.Combine(folder, $"{id}{extension}");
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Ignore cleanup failure.
                    }
                }
            }
        }
    }
}
