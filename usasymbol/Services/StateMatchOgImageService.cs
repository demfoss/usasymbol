using SkiaSharp;

namespace USASymbol.Services
{
    public interface IStateMatchOgImageService
    {
        /// <summary>Renders a 1200x630 share-preview PNG for one scored state — the "state + score" card people actually post.</summary>
        byte[] Render(string stateName, string abbreviation, int score, string? strongestMetricName);
    }

    /// <summary>
    /// Draws directly with SkiaSharp (no SVG round-trip, unlike MapPngService) since this is
    /// just text and rules. Uses the classic SKPaint text APIs (TextSize/Typeface/MeasureText)
    /// rather than SKFont-based overloads, since those aren't all present in the pinned
    /// SkiaSharp 2.88.x used by this project. The site's own webfonts (Libre Baskerville / IBM
    /// Plex Mono) aren't bundled as files, so family names here are best-effort — SkiaSharp
    /// falls back to the platform default serif/monospace if a name isn't installed on the
    /// server, it never throws.
    /// </summary>
    public sealed class StateMatchOgImageService : IStateMatchOgImageService
    {
        private static readonly SKColor Paper = SKColor.Parse("#F5F1E8");
        private static readonly SKColor Ink = SKColor.Parse("#172033");
        private static readonly SKColor Ink2 = SKColor.Parse("#465064");
        private static readonly SKColor Ink3 = SKColor.Parse("#727B8C");
        private static readonly SKColor Teal = SKColor.Parse("#245E61");

        public byte[] Render(string stateName, string abbreviation, int score, string? strongestMetricName)
        {
            const int width = 1200;
            const int height = 630;

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(Paper);

            using var serifBold = SKTypeface.FromFamilyName("Georgia", SKFontStyle.Bold);
            using var serifRegular = SKTypeface.FromFamilyName("Georgia", SKFontStyle.Normal);
            using var monoBold = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold);
            using var sansBold = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
            using var sansRegular = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);

            DrawText(canvas, "STATE MATCH", 72, 96, sansBold, 26, Teal);

            using (var rulePaint = new SKPaint { Color = Teal, IsAntialias = true })
            {
                canvas.DrawRect(new SKRect(72, 116, 152, 119), rulePaint);
            }

            DrawText(canvas, stateName, 68, 250, serifBold, 88, Ink);
            DrawText(canvas, abbreviation, 72, 292, sansRegular, 30, Ink3);

            DrawText(canvas, score.ToString(), 68, 470, monoBold, 168, Teal);
            var scoreWidth = MeasureText(score.ToString(), monoBold, 168);
            DrawText(canvas, "/ 100 match", 68 + scoreWidth + 18, 470, serifRegular, 44, Ink3);

            if (!string.IsNullOrWhiteSpace(strongestMetricName))
            {
                DrawText(canvas, $"Strongest fit: {strongestMetricName}", 72, 520, sansRegular, 28, Ink2);
            }

            DrawText(canvas, "usasymbol.com/state-match", 72, height - 56, sansRegular, 26, Ink3);

            canvas.Flush();
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private static void DrawText(SKCanvas canvas, string text, float x, float y, SKTypeface typeface, float size, SKColor color)
        {
            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                Typeface = typeface,
                TextSize = size,
                TextAlign = SKTextAlign.Left
            };
            canvas.DrawText(text, x, y, paint);
        }

        private static float MeasureText(string text, SKTypeface typeface, float size)
        {
            using var paint = new SKPaint { Typeface = typeface, TextSize = size };
            return paint.MeasureText(text);
        }
    }
}
