using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using usasymbol.Models;
using usasymbol.Services;
using USASymbol.Data;

namespace usasymbol.Controllers
{
    public record QuizMistake(string Question, int MissPercent, int Answers);

    [Route("quizzes")]
    public class QuizController : Controller
    {
        private static readonly HashSet<string> Modes = new() { "practice", "classic", "timed", "daily" };
        private static readonly HashSet<string> EventTypes = new() { "start", "answer", "complete" };
        private const int MinAnswersForMistakes = 20;

        private readonly QuizService _quizService;
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly USASymbol.Services.IQuizShareImageService _shareImages;

        public QuizController(QuizService quizService, AppDbContext context, IMemoryCache cache, USASymbol.Services.IQuizShareImageService shareImages)
        {
            _shareImages = shareImages;
            _quizService = quizService;
            _context = context;
            _cache = cache;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var quizzes = _quizService.GetAll();
            ViewData["Popularity"] = GetPopularity();
            return View(quizzes);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var quiz = _quizService.GetBySlug(slug);

            if (quiz == null)
                return NotFound();

            ViewData["CommonMistakes"] = await GetCommonMistakesAsync(quiz);
            ViewData["AnswerStats"] = await GetAnswerStatsAsync(quiz);
            return View(quiz);
        }

        /// <summary>Plays per quiz (finished rounds), cached for an hour. Empty if the table is unavailable.</summary>
        private Dictionary<string, int> GetPopularity()
        {
            return _cache.GetOrCreate("quiz-popularity", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                try
                {
                    return _context.QuizAttempts
                        .GroupBy(a => a.QuizSlug)
                        .Select(g => new { g.Key, N = g.Count() })
                        .ToDictionary(x => x.Key, x => x.N);
                }
                catch
                {
                    return new Dictionary<string, int>();
                }
            })!;
        }

        /// <summary>
        /// Share of players who picked each option, per question, rounded to the nearest 5%.
        /// Only questions with enough answers are included, so small samples never look precise.
        /// </summary>
        private Task<Dictionary<string, Dictionary<string, int>>> GetAnswerStatsAsync(Quiz quiz)
        {
            return _cache.GetOrCreateAsync($"quiz-answer-stats-{quiz.Slug}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                var result = new Dictionary<string, Dictionary<string, int>>();
                try
                {
                    var rows = await _context.QuizEvents
                        .Where(e => e.QuizSlug == quiz.Slug && e.EventType == "answer" && e.Choice != null)
                        .GroupBy(e => new { e.QuestionKey, e.Choice })
                        .Select(g => new { Q = g.Key.QuestionKey!, C = g.Key.Choice!, N = g.Count() })
                        .ToListAsync();

                    foreach (var q in rows.GroupBy(r => r.Q))
                    {
                        var total = q.Sum(x => x.N);
                        if (total < MinAnswersForMistakes) continue;
                        result[q.Key] = q.ToDictionary(x => x.C, x => (int)Math.Round(x.N * 100.0 / total / 5.0) * 5);
                    }
                }
                catch
                {
                    // analytics must never break the page
                }
                return result;
            })!;
        }

        /// <summary>Questions players miss most, built from our own anonymous answer events.</summary>
        private Task<List<QuizMistake>> GetCommonMistakesAsync(Quiz quiz)
        {
            return _cache.GetOrCreateAsync($"quiz-mistakes-{quiz.Slug}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                try
                {
                    var rows = await _context.QuizEvents
                        .Where(e => e.QuizSlug == quiz.Slug && e.EventType == "answer" && e.QuestionKey != null)
                        .GroupBy(e => e.QuestionKey!)
                        .Select(g => new { Q = g.Key, Total = g.Count(), Wrong = g.Count(x => x.IsCorrect == false) })
                        .Where(x => x.Total >= MinAnswersForMistakes)
                        .ToListAsync();

                    var known = quiz.Questions.Select(q => q.Question.Trim()).ToHashSet();
                    return rows
                        .Where(x => known.Contains(x.Q) && x.Wrong > 0)
                        .Select(x => new QuizMistake(x.Q, (int)Math.Round(x.Wrong * 100.0 / x.Total / 5.0) * 5, x.Total))
                        .OrderByDescending(x => x.MissPercent)
                        .Take(5)
                        .ToList();
                }
                catch
                {
                    return new List<QuizMistake>(); // analytics must never break the page
                }
            })!;
        }

        /// <summary>Result card used as the og:image when someone shares a score.</summary>
        [HttpGet("{slug}/share.png")]
        public IActionResult ShareImage(string slug, int s, int n, string? m, string? g)
        {
            var quiz = _quizService.GetBySlug(slug);
            if (quiz == null) return NotFound();

            n = Math.Clamp(n, 1, 200);
            s = Math.Clamp(s, 0, n);
            var mode = (m ?? "").ToLowerInvariant() switch { "classic" => "Classic", "timed" => "Timed", "daily" => "Daily quiz", _ => "Practice" };
            var marks = new string((g ?? "").Where(c => c == '0' || c == '1').Take(20).ToArray());

            var png = _shareImages.Render(quiz.Seo?.Title ?? quiz.Title, s, n, mode, marks);
            Response.Headers.CacheControl = "public, max-age=86400";
            return File(png, "image/png");
        }

        /// <summary>Anonymous start / answer / complete events. Stores no IP, user id or cookie.</summary>
        [HttpPost("{slug}/events")]
        public async Task<IActionResult> SaveEvents(string slug, [FromBody] QuizEventsDto dto)
        {
            var quiz = _quizService.GetBySlug(slug);
            if (quiz == null || dto?.Events == null || dto.Events.Count == 0 || dto.Events.Count > 80)
                return NoContent();

            var mode = (dto.Mode ?? "").ToLowerInvariant();
            if (!Modes.Contains(mode)) mode = "practice";

            var known = quiz.Questions.Select(q => q.Question.Trim()).ToHashSet();
            var choicesByQuestion = quiz.Questions.GroupBy(q => q.Question.Trim()).ToDictionary(g => g.Key, g => g.First().Choices);
            var now = DateTime.UtcNow;
            var rows = new List<QuizEvent>();

            foreach (var e in dto.Events)
            {
                var type = (e.T ?? "").ToLowerInvariant();
                if (!EventTypes.Contains(type)) continue;

                string? key = null;
                if (type == "answer")
                {
                    key = (e.Q ?? "").Trim();
                    if (!known.Contains(key)) continue;   // ignore anything that is not a real question
                }

                rows.Add(new QuizEvent
                {
                    QuizSlug = quiz.Slug,
                    Mode = mode,
                    EventType = type,
                    QuestionKey = key,
                    IsCorrect = type == "answer" ? e.C : null,
                    Choice = type == "answer" && e.P != null && choicesByQuestion.TryGetValue(key!, out var opts) && opts.Contains(e.P) ? e.P : null,
                    CreatedAt = now
                });
            }

            if (rows.Count == 0) return NoContent();

            try
            {
                _context.QuizEvents.AddRange(rows);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // never surface analytics failures to players
            }

            return NoContent();
        }

        [HttpPost("{slug}/result")]
        public async Task<IActionResult> SaveResult(string slug, [FromBody] QuizResultDto dto)
        {
            var attempt = new QuizAttempt
            {
                QuizSlug = slug,
                Mode = dto.Mode ?? "classic",
                Score = dto.Score,
                QuestionCount = dto.QuestionCount,
                TimeSpentSeconds = dto.TimeSpentSeconds,
                CreatedAt = DateTime.UtcNow
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            var baseQuery = _context.QuizAttempts
                .Where(x => x.QuizSlug == slug && x.Mode == attempt.Mode);

            var total = await baseQuery.CountAsync();
            var better = await baseQuery.CountAsync(x => x.Score < attempt.Score);

            var percentile = total == 0 ? 50 : (int)((double)better / total * 100);
            var average = await baseQuery.AverageAsync(x => (double?)x.Score) ?? 0;

            return Ok(new
            {
                percentile,
                average = (int)average
            });
        }
    }
}
