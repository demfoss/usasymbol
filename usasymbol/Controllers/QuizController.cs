using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using usasymbol.Models;
using usasymbol.Services;
using USASymbol.Data;

namespace usasymbol.Controllers
{
    [Route("quizzes")]
    public class QuizController : Controller
    {
        private readonly QuizService _quizService;
        private readonly AppDbContext _context;

        public QuizController(QuizService quizService, AppDbContext context)
        {
            _quizService = quizService;
            _context = context;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var quizzes = _quizService.GetAll();
            return View(quizzes);
        }

        [HttpGet("{slug}")]
        public IActionResult Details(string slug)
        {
            var quiz = _quizService.GetBySlug(slug);

            if (quiz == null)
                return NotFound();

            return View(quiz);
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
