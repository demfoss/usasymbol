namespace USASymbol.Helpers
{
    /// <summary>Resolves a displayable image for a symbol (same rules the "related symbols" sidebar uses).</summary>
    public static class SymbolImageResolver
    {
        public static string Resolve(USASymbol.Models.Symbol symbol, string? stateSlug, string webRootPath)
        {
            var imageUrl = symbol.ImageUrl?.Trim();
            if (string.IsNullOrWhiteSpace(imageUrl)) return string.Empty;

            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var abs)
                && (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
                return imageUrl;

            if (Exists(imageUrl, webRootPath)) return imageUrl;

            if (imageUrl.StartsWith("/images/mottos/", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            if (string.IsNullOrWhiteSpace(stateSlug)) return string.Empty;

            var fileName = Path.GetFileNameWithoutExtension(imageUrl);
            if (!string.IsNullOrWhiteSpace(fileName) && imageUrl.StartsWith("/images/mammals/", StringComparison.OrdinalIgnoreCase))
            {
                var p = $"/images/mammals/{stateSlug}/{fileName}.jpg";
                if (Exists(p, webRootPath)) return p;
            }
            if (imageUrl.StartsWith("/images/flags/", StringComparison.OrdinalIgnoreCase))
            {
                var p = $"/images/flags/{stateSlug}/flag.webp";
                if (Exists(p, webRootPath)) return p;
            }
            if (imageUrl.StartsWith("/images/nicknames/", StringComparison.OrdinalIgnoreCase))
            {
                var p = $"/images/nicknames/{stateSlug}.webp";
                if (Exists(p, webRootPath)) return p;
            }
            if (string.Equals(symbol.Type, "state-seal", StringComparison.OrdinalIgnoreCase))
            {
                var p = $"/images/seals/{stateSlug}/seal.webp";
                if (Exists(p, webRootPath)) return p;
            }
            if (string.Equals(symbol.Type, "coat-of-arms", StringComparison.OrdinalIgnoreCase))
            {
                var p = $"/images/coats-of-arms/{stateSlug}/coat-of-arms.webp";
                if (Exists(p, webRootPath)) return p;
            }
            return string.Empty;
        }

        private static bool Exists(string imageUrl, string webRootPath)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return false;
            var relative = imageUrl.Split('?', '#')[0].Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var root = string.IsNullOrWhiteSpace(webRootPath) ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot") : webRootPath;
            var full = Path.GetFullPath(Path.Combine(root, relative));
            return full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) && File.Exists(full);
        }
    }
}
