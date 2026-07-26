using System.Globalization;
using System.Text.RegularExpressions;

namespace USASymbol.Services
{
    /// <summary>Resolved per-state accent pair for a compare page, plus a readable foreground for each.</summary>
    public record CompareAccentPair(string HexA, string ForegroundA, string HexB, string ForegroundB);

    /// <summary>
    /// Turns each state's flag color(s) (from Content/states/{state}/color.yaml, loaded via IColorService)
    /// into a deterministic, distinguishable accent pair for the compare page's scoreboard, metric bars,
    /// and pros/cons headers. Never random — the same state pair always resolves to the same colors.
    /// </summary>
    public static class CompareAccentColor
    {
        private const string FallbackA = "#1D5A8F";
        private const string FallbackB = "#B45309";
        private const double HueCollisionDegrees = 20;
        private const double LightnessCollisionDelta = 0.15;

        public static CompareAccentPair Resolve(IReadOnlyList<string>? colorsA, IReadOnlyList<string>? colorsB)
        {
            var candidatesA = (colorsA ?? Array.Empty<string>()).Where(IsValidHex).ToList();
            var candidatesB = (colorsB ?? Array.Empty<string>()).Where(IsValidHex).ToList();

            var hexA = candidatesA.FirstOrDefault() ?? FallbackA;
            var hexB = candidatesB.FirstOrDefault() ?? FallbackB;

            if (candidatesB.Count > 0 && IsTooSimilar(hexA, hexB))
            {
                var altB = candidatesB.Skip(1).FirstOrDefault(c => !IsTooSimilar(hexA, c));
                hexB = altB ?? RotateHue(hexB, 40);
            }

            return new CompareAccentPair(hexA, ForegroundFor(hexA), hexB, ForegroundFor(hexB));
        }

        private static bool IsValidHex(string? hex) =>
            !string.IsNullOrWhiteSpace(hex) && Regex.IsMatch(hex.Trim(), "^#?[0-9a-fA-F]{6}$");

        private static bool IsTooSimilar(string hexA, string hexB)
        {
            var (h1, _, l1) = ToHsl(hexA);
            var (h2, _, l2) = ToHsl(hexB);
            var hueDistance = Math.Min(Math.Abs(h1 - h2), 360 - Math.Abs(h1 - h2));
            var lightnessDistance = Math.Abs(l1 - l2);
            return hueDistance < HueCollisionDegrees && lightnessDistance < LightnessCollisionDelta;
        }

        private static string RotateHue(string hex, double degrees)
        {
            var (h, s, l) = ToHsl(hex);
            var rotated = (h + degrees) % 360;
            return FromHsl(rotated, Math.Max(s, 0.35), Math.Clamp(l, 0.3, 0.55));
        }

        private static string ForegroundFor(string hex)
        {
            var (_, _, l) = ToHsl(hex);
            return l > 0.6 ? "#12161B" : "#FFFFFF";
        }

        private static (double H, double S, double L) ToHsl(string hex)
        {
            hex = hex.TrimStart('#');
            var r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber) / 255.0;
            var g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber) / 255.0;
            var b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber) / 255.0;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var l = (max + min) / 2;
            double h = 0, s;
            var d = max - min;

            if (d == 0)
            {
                s = 0;
            }
            else
            {
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;
                h *= 60;
            }

            return (h, s, l);
        }

        private static string FromHsl(double h, double s, double l)
        {
            double r, g, b;

            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                var p = 2 * l - q;
                r = HueToRgb(p, q, h / 360 + 1.0 / 3);
                g = HueToRgb(p, q, h / 360);
                b = HueToRgb(p, q, h / 360 - 1.0 / 3);
            }

            return $"#{ToByte(r):X2}{ToByte(g):X2}{ToByte(b):X2}";
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }

        private static int ToByte(double v) => (int)Math.Round(Math.Clamp(v, 0, 1) * 255);
    }
}
