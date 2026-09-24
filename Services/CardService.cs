namespace Shoppet_VetClinic.Services
{
    /// <summary>
    /// Handles Pet ID Virtual Card utilities:
    ///  - Generates unique CardId codes (e.g. PET-A1B2C3)
    ///  - Builds QR codes for the public card viewer (offline)
    ///  - Provides available card themes for premium users
    /// </summary>
    public class CardService
    {
        public static readonly string[] FreeThemes =
        {
            "Classic"
        };

        public static readonly string[] PremiumThemes =
        {
            "Classic",
            "Ocean",
            "Sunset",
            "Mint",
            "Royal"
        };

        /// <summary>
        /// Generates a new unique Pet ID Card number.
        /// Format: PET-XXXXXX where X is uppercase hex.
        /// </summary>
        public string GenerateCardId()
        {
            var code = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 6)
                .ToUpperInvariant();

            return $"PET-{code}";
        }

        /// <summary>
        /// Builds a public QR code image URL (online service, requires internet).
        /// Kept for backward compatibility.
        /// </summary>
        public string BuildQrUrl(string baseUrl, string cardId)
        {
            var target = $"{baseUrl.TrimEnd('/')}/pet/card/{cardId}";
            var encoded = Uri.EscapeDataString(target);

            return $"https://api.qrserver.com/v1/create-qr-code/" +
                   $"?size=200x200&data={encoded}";
        }

        /// <summary>
        /// Generates a QR code as a base64 data URI (offline — no internet needed).
        /// Use directly in: &lt;img src="@dataUri" /&gt;
        /// </summary>
        public string GenerateQrDataUri(string cardId, string baseUrl = "https://shoppetcare.app")
        {
            var target = $"{baseUrl.TrimEnd('/')}/pet/card/{cardId}";

            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(
                target,
                QRCoder.QRCodeGenerator.ECCLevel.Q);

            var pngQr = new QRCoder.PngByteQRCode(qrData);
            var bytes = pngQr.GetGraphic(10);

            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Returns the CSS class name for a card theme.
        /// </summary>
        public string GetThemeClass(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
                return "theme-classic";

            var normalized = theme.Trim().ToLowerInvariant();

            return normalized switch
            {
                "classic" => "theme-classic",
                "ocean" => "theme-ocean",
                "sunset" => "theme-sunset",
                "mint" => "theme-mint",
                "royal" => "theme-royal",
                _ => "theme-classic"
            };
        }

        /// <summary>
        /// Alias for GetThemeClass — used by the Pet ID Card razor page.
        /// </summary>
        public string GetThemeCssClass(string? theme)
        {
            return GetThemeClass(theme);
        }

        /// <summary>
        /// Validates that the given theme is available to the user
        /// based on their premium status.
        /// </summary>
        public bool IsThemeAllowed(string? theme, bool isPremium)
        {
            if (string.IsNullOrWhiteSpace(theme)) return true;

            var allowed = isPremium ? PremiumThemes : FreeThemes;

            return allowed.Any(t =>
                string.Equals(t, theme, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns feeding status for a pet based on the last time it was fed.
        /// "happy" (&lt;6h), "hungry" (6–12h), "overdue" (12h+), "never"
        /// </summary>
        public string GetFeedingStatus(DateTime? lastFedAt)
        {
            if (lastFedAt == null) return "never";

            var hours = (DateTime.Now - lastFedAt.Value).TotalHours;

            if (hours < 6) return "happy";
            if (hours < 12) return "hungry";
            return "overdue";
        }
    }
}