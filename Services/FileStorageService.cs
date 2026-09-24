using Microsoft.AspNetCore.Components.Forms;

namespace Shoppet_VetClinic.Services
{
    public class FileStorageService
    {
        private readonly IWebHostEnvironment _environment;

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
            IWebHostEnvironment environment)
        {
            _environment = environment;
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

            if (!AllowedExtensions.Contains(
                extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG, and WEBP images are allowed.");
            }

            if (!AllowedContentTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The selected file is not a supported image.");
            }


            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    category);

            Directory.CreateDirectory(
                folder);


            // Remove old versions.
            foreach (var oldExtension
                in AllowedExtensions)
            {
                var oldFile =
                    Path.Combine(
                        folder,
                        $"{id}{oldExtension}");

                if (File.Exists(oldFile))
                {
                    try
                    {
                        File.Delete(oldFile);
                    }
                    catch
                    {
                        // Ignore old-file cleanup failure.
                    }
                }
            }


            var filePath =
                Path.Combine(
                    folder,
                    $"{id}{extension}");


            await using var inputStream =
                file.OpenReadStream(
                    MaxFileSize);

            await using var outputStream =
                new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);

            await inputStream.CopyToAsync(
                outputStream);


            return
                $"/uploads/{category}/{id}{extension}";
        }


        public string? GetImageUrl(
            string category,
            int id)
        {
            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    category);

            foreach (var extension
                in AllowedExtensions)
            {
                var filePath =
                    Path.Combine(
                        folder,
                        $"{id}{extension}");

                if (File.Exists(filePath))
                {
                    var timestamp =
                        File.GetLastWriteTimeUtc(
                            filePath)
                        .Ticks;

                    return
                        $"/uploads/{category}/{id}{extension}?v={timestamp}";
                }
            }

            return null;
        }


        public void DeleteImage(
            string category,
            int id)
        {
            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    category);

            foreach (var extension
                in AllowedExtensions)
            {
                var filePath =
                    Path.Combine(
                        folder,
                        $"{id}{extension}");

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