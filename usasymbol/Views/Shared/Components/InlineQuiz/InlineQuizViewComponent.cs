using Microsoft.AspNetCore.Mvc;
using usasymbol.Models;
using usasymbol.Services;
using USASymbol.Services.Interface;

namespace USASymbol.Views.Shared.Components
{
    public class InlineQuizViewModel
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string StateName { get; set; } = "";
        public string StateSlug { get; set; } = "";
        public string FullQuizUrl { get; set; } = "/quizzes";
        public string FullQuizLabel { get; set; } = "All quizzes";
        public int PerRound { get; set; } = 5;
        public List<QuizQuestion> Pool { get; set; } = new();
        public List<Quiz> More { get; set; } = new();
        public List<Quiz> Neighbors { get; set; } = new();
    }

    /// <summary>
    /// mode = "state"    -> quiz about one state's symbols (built from the symbol tables)
    /// mode = "rankings" -> "how well do you know America" pool from us-rankings-trivia-quiz.yaml
    /// </summary>
    public class InlineQuizViewComponent : ViewComponent
    {
        private const string RankingsQuiz = "us-rankings-trivia-quiz";
        private readonly QuizService _quizzes;
        private readonly IBorderService _borders;

        public InlineQuizViewComponent(QuizService quizzes, IBorderService borders)
        {
            _quizzes = quizzes;
            _borders = borders;
        }

        public async Task<IViewComponentResult> InvokeAsync(string mode, string? stateName = null)
        {
            var vm = new InlineQuizViewModel { Id = "iq-" + Guid.NewGuid().ToString("N")[..8] };
            string excludeSlug;

            if (mode == "state")
            {
                if (string.IsNullOrWhiteSpace(stateName)) return Content(string.Empty);
                vm.Title = $"{stateName} symbols";
                vm.StateName = stateName;
                vm.StateSlug = StateLinks.Slug(stateName);
                excludeSlug = StateSymbolQuizBuilder.SlugFor(stateName);
                vm.FullQuizUrl = "/quizzes/" + excludeSlug;
                var borders = await _borders.GetBorderContentAsync(vm.StateSlug);
                vm.Neighbors = (borders?.BorderSummary?.BorderingStatesList ?? "").Split(',')
                    .Select(name => name.Trim()).Where(name => StateSymbolQuizBuilder.AllStateNames.Contains(name))
                    .Select(name => _quizzes.GetBySlug(StateSymbolQuizBuilder.SlugFor(name)))
                    .Where(quiz => quiz != null).Cast<Quiz>().Take(3).ToList();
            }
            else
            {
                vm.Title = "Across America";
                vm.FullQuizUrl = "/quizzes/" + RankingsQuiz;
                excludeSlug = RankingsQuiz;
            }

            // Use the same cached questions as the full quiz and the answer-event validator.
            var fullQuiz = _quizzes.GetBySlug(excludeSlug);
            vm.Slug = excludeSlug;
            vm.Pool = fullQuiz?.Questions ?? new List<QuizQuestion>();
            var fullCount = fullQuiz?.QuestionsPerRound > 0 ? Math.Min(fullQuiz.QuestionsPerRound, vm.Pool.Count) : Math.Min(10, vm.Pool.Count);
            vm.FullQuizLabel = $"Full quiz · {fullCount} questions";

            if (vm.Pool.Count < vm.PerRound) return Content(string.Empty);

            vm.More = _quizzes.GetAll()
                .Where(q => !string.Equals(q.Slug, excludeSlug, StringComparison.OrdinalIgnoreCase)
                            && !q.Slug.EndsWith("-state-symbols-quiz", StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => q.Slug is "state-birds-quiz" or "state-shapes-quiz" ? 0 : 1)
                .ThenBy(q => q.Title)
                .Take(3)
                .ToList();

            return View(vm);
        }
    }
}
