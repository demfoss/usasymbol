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
        private static readonly HashSet<string> Modes = new() { "practice", "classic", "timed", "daily", "inline" };
        private static readonly HashSet<string> EventTypes = new() { "start", "answer", "complete", "result_click" };
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
            var mode = (m ?? "").ToLowerInvariant() switch { "classic" => "Classic", "timed" => "Timed", "daily" => "Daily quiz", "inline" => "Quick quiz", _ => "Practice" };
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
            var correctByQuestion = quiz.Questions.GroupBy(q => q.Question.Trim()).ToDictionary(g => g.Key, g => g.First().CorrectAnswer);
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
                if (type == "result_click")
                {
                    key = (e.Q ?? "").Trim();
                    if (key.Length > 300 || !(key is "share" or "again" ||
                        key.StartsWith("/quizzes/", StringComparison.Ordinal) ||
                        key.StartsWith("/states/", StringComparison.Ordinal) || key == "/quizzes")) continue;
                }
                var validChoice = type == "answer" && e.P != null &&
                    choicesByQuestion.TryGetValue(key!, out var opts) && opts.Contains(e.P) ? e.P : null;
                if (mode == "inline" && type == "answer" && validChoice == null) continue;

                rows.Add(new QuizEvent
                {
                    QuizSlug = quiz.Slug,
                    Mode = mode,
                    EventType = type,
                    QuestionKey = key,
                    IsCorrect = type == "answer" ? (validChoice != null ? validChoice == correctByQuestion[key!] : e.C) : null,
                    Choice = validChoice,
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

        /// <summary>Five-question widget results, scored against the same server-side question pool.</summary>
        [HttpPost("{slug}/inline-result")]
        [RequestSizeLimit(16384)]
        public async Task<IActionResult> SaveInlineResult(string slug, [FromBody] InlineQuizResultDto dto)
        {
            var quiz = _quizService.GetBySlug(slug);
            if (quiz == null) return NotFound();
            if (dto?.Answers == null || dto.Answers.Count != 5 ||
                dto.Answers.Any(answer => answer == null || string.IsNullOrWhiteSpace(answer.Question)) ||
                dto.Answers.Select(answer => answer.Question.Trim()).Distinct().Count() != 5) return BadRequest();

            var known = quiz.Questions.GroupBy(question => question.Question.Trim()).ToDictionary(group => group.Key, group => group.First());
            var score = 0;
            foreach (var answer in dto.Answers)
            {
                if (!known.TryGetValue(answer.Question.Trim(), out var question) || !question.Choices.Contains(answer.Choice)) return BadRequest();
                if (answer.Choice == question.CorrectAnswer) score++;
            }

            try
            {
                var percentage = score * 20;
                var prior = _context.QuizAttempts.Where(attempt => attempt.QuizSlug == slug && attempt.Mode == "inline" && attempt.QuestionCount == 5);
                var rounds = await prior.CountAsync();
                int? percentile = rounds >= MinAnswersForMistakes
                    ? (int)Math.Round(await prior.CountAsync(attempt => attempt.Score < percentage) * 100d / rounds / 5) * 5 : null;
                var questions = dto.Answers.Select(answer => answer.Question.Trim()).ToList();
                var accuracies = await _context.QuizEvents
                    .Where(e => e.QuizSlug == slug && e.Mode == "inline" && e.EventType == "answer" && e.QuestionKey != null && questions.Contains(e.QuestionKey) && e.IsCorrect != null)
                    .GroupBy(e => e.QuestionKey!)
                    .Select(group => new { Question = group.Key, Answers = group.Count(), Correct = group.Count(e => e.IsCorrect == true) })
                    .Where(group => group.Answers >= MinAnswersForMistakes).ToListAsync();
                var hardest = accuracies.OrderBy(question => (double)question.Correct / question.Answers).FirstOrDefault();
                _context.QuizAttempts.Add(new QuizAttempt
                {
                    QuizSlug = slug, Mode = "inline", Score = percentage, QuestionCount = 5,
                    TimeSpentSeconds = Math.Clamp(dto.TimeSpentSeconds, 0, 7200), CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                Response.Headers.CacheControl = "no-store";
                return Ok(new
                {
                    rounds, percentile, hardestQuestion = hardest?.Question, questionAnswers = hardest?.Answers,
                    correctPercent = hardest == null ? (int?)null : (int)Math.Round(hardest.Correct * 100d / hardest.Answers)
                });
            }
            catch
            {
                // An unavailable analytics database must not interrupt a quiz or invent comparison data.
                return NoContent();
            }
        }

        [HttpPost("{slug}/result")]
        public async Task<IActionResult> SaveResult(string slug, [FromBody] QuizResultDto dto)
        {
            var mode = (dto?.Mode ?? "classic").Trim().ToLowerInvariant();
            // Inline rounds must go through the answer-key validator, not this legacy score endpoint.
            if (dto == null || !Modes.Contains(mode) || mode == "inline" || dto.Score is < 0 or > 100 ||
                dto.QuestionCount is < 0 or > 200 || dto.TimeSpentSeconds is < 0 or > 7200) return BadRequest();
            if (_quizService.GetBySlug(slug) == null) return NotFound();
            var attempt = new QuizAttempt
            {
                QuizSlug = slug,
                Mode = mode,
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
