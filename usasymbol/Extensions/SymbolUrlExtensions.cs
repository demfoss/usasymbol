using USASymbol.Models;

namespace USASymbol.Extensions
{
    public static class SymbolUrlExtensions
    {
        public static string ToSymbolUrl(this Symbol symbol, string stateSlug)
        {
            ArgumentNullException.ThrowIfNull(symbol);

            var normalizedStateSlug = NormalizeSegment(stateSlug);
            var normalizedType = NormalizeSegment(symbol.Type);
            var normalizedSymbolSlug = NormalizeSegment(symbol.Slug);

            if (string.IsNullOrWhiteSpace(normalizedStateSlug))
                return "/";

            if (string.IsNullOrWhiteSpace(normalizedType))
                return $"/states/{normalizedStateSlug}";

            if (string.IsNullOrWhiteSpace(normalizedSymbolSlug))
                return $"/states/{normalizedStateSlug}/{normalizedType}";

            return $"/states/{normalizedStateSlug}/{normalizedType}/{normalizedSymbolSlug}";
        }

        public static string ToSymbolUrl(this Symbol symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);

            var stateSlug = symbol.State?.Slug;

            if (string.IsNullOrWhiteSpace(stateSlug))
                throw new InvalidOperationException("State slug is required to build a symbol URL.");

            return ToSymbolUrl(symbol, stateSlug);
        }

        private static string NormalizeSegment(string? value)
            => (value ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
    }
}
