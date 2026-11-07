using SkiaSharp;

namespace USASymbol.Services
{
    public interface IQuizShareImageService
    {
        /// <summary>1200x630 result card: quiz title, score and a green/red square per question.</summary>
        byte[] Render(string quizTitle, int score, int total, string modeName, string marks);
    }

    /// <summary>
    /// Same approach as StateMatchOgImageService: plain SkiaSharp drawing, no webfonts on the server,
    /// platform fallbacks for the family names. Colors match the quiz pages (cold paper, navy, gold).
    /// </summary>
    public sealed class QuizShareImageService : IQuizShareImageService
    {
        private static readonly SKColor Paper = SKColor.Parse("#EFF3F6");
        private static readonly SKColor Ink = SKColor.Parse("#0E1A26");
        private static readonly SKColor Ink2 = SKColor.Parse("#55697B");
        private static readonly SKColor Ink3 = SKColor.Parse("#8496A5");
        private static readonly SKColor Gold = SKColor.Parse("#A8791A");
        private static readonly SKColor Ok = SKColor.Parse("#1F7A4D");
        private static readonly SKColor Bad = SKColor.Parse("#B4432F");

        public byte[] Render(string quizTitle, int score, int total, string modeName, string marks)
        {
            const int width = 1200;
            const int height = 630;

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(Paper);

            using var serifBold = SKTypeface.FromFamilyName("Georgia", SKFontStyle.Bold);
            using var monoBold = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold);
            using var sansBold = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
            using var sansRegular = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);

            DrawText(canvas, "USA SYMBOL  ·  QUIZ", 72, 92, monoBold, 24, Gold);
            using (var rule = new SKPaint { Color = Gold, IsAntialias = true })
            {
                canvas.DrawRect(new SKRect(72, 112, 152, 115), rule);
            }

            // title, wrapped to at most two lines
            var y = 190f;
            foreach (var line in Wrap(quizTitle, serifBold, 62, width - 144).Take(2))
            {
                DrawText(canvas, line, 72, y, serifBold, 62, Ink);
                y += 72;
            }

            var scoreText = score.ToString();
            DrawText(canvas, scoreText, 68, 400, sansBold, 170, Ink);
            var scoreWidth = Measure(scoreText, sansBold, 170);
            DrawText(canvas, $"/ {total}", 68 + scoreWidth + 16, 400, sansRegular, 56, Ink3);
            DrawText(canvas, modeName + "  ·  Can you beat it?", 72, 450, sansRegular, 30, Ink2);

            // one square per answered question
            var shown = marks.Take(20).ToList();
            if (shown.Count > 0)
            {
                const float size = 44f, gap = 10f;
                var maxPerRow = 20;
                var x = 72f;
                var rowY = 490f;
                using var paint = new SKPaint { IsAntialias = true };
                foreach (var m in shown)
                {
                    paint.Color = m == '1' ? Ok : Bad;
                    canvas.DrawRoundRect(new SKRect(x, rowY, x + size, rowY + size), 8, 8, paint);
                    x += size + gap;
                    if (x + size > width - 72 && maxPerRow > 0) { x = 72f; rowY += size + gap; }
                }
            }

            DrawText(canvas, "usasymbol.com/quizzes", 72, height - 36, sansRegular, 24, Ink3);

            canvas.Flush();
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private static IEnumerable<string> Wrap(string text, SKTypeface face, float size, float maxWidth)
        {
            var line = "";
            foreach (var word in (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var test = line.Length == 0 ? word : line + " " + word;
                if (Measure(test, face, size) > maxWidth && line.Length > 0)
                {
                    yield return line;
                    line = word;
                }
                else
                {
                    line = test;
                }
            }
            if (line.Length > 0) yield return line;
        }

        private static void DrawText(SKCanvas canvas, string text, float x, float y, SKTypeface face, float size, SKColor color)
        {
            using var paint = new SKPaint { Color = color, IsAntialias = true, Typeface = face, TextSize = size, TextAlign = SKTextAlign.Left };
            canvas.DrawText(text, x, y, paint);
        }

        private static float Measure(string text, SKTypeface face, float size)
        {
            using var paint = new SKPaint { Typeface = face, TextSize = size };
            return paint.MeasureText(text);
        }
    }
}
