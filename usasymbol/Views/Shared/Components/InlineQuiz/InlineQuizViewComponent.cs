using Microsoft.AspNetCore.Mvc;
using usasymbol.Models;
using usasymbol.Services;

namespace USASymbol.Views.Shared.Components
{
    public class InlineQuizViewModel
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Intro { get; set; } = "";
        public string FullQuizUrl { get; set; } = "/quizzes";
        public string FullQuizLabel { get; set; } = "All quizzes";
        public int PerRound { get; set; } = 5;
        public List<QuizQuestion> Pool { get; set; } = new();
        public List<Quiz> More { get; set; } = new();
    }

    /// <summary>
    /// mode = "state"    -> quiz about one state's symbols (built from the symbol tables)
    /// mode = "rankings" -> "how well do you know America" pool from us-rankings-trivia-quiz.yaml
    /// </summary>
    public class InlineQuizViewComponent : ViewComponent
    {
        private const string RankingsQuiz = "us-rankings-trivia-quiz";
        private readonly QuizService _quizzes;
        private readonly StateSymbolQuizBuilder _builder;

        public InlineQuizViewComponent(QuizService quizzes, StateSymbolQuizBuilder builder)
        {
            _quizzes = quizzes;
            _builder = builder;
        }

        public IViewComponentResult Invoke(string mode, string? stateName = null)
        {
            var vm = new InlineQuizViewModel { Id = "iq-" + Guid.NewGuid().ToString("N")[..8] };
            string excludeSlug;

            if (mode == "state")
            {
                if (string.IsNullOrWhiteSpace(stateName)) return Content(string.Empty);
                vm.Pool = _builder.Build(stateName);
                vm.Title = $"How well do you know {stateName}'s symbols?";
                vm.Intro = "Five quick questions built from the symbols on this site. Questions and answers are shuffled every time you play.";
                excludeSlug = StateSymbolQuizBuilder.SlugFor(stateName);
                vm.FullQuizUrl = "/quizzes/" + excludeSlug;
                vm.FullQuizLabel = $"Play the full {stateName} quiz";
            }
            else
            {
                var quiz = _quizzes.GetBySlug(RankingsQuiz);
                vm.Pool = quiz?.Questions ?? new List<QuizQuestion>();
                vm.Title = "How well do you know America?";
                vm.Intro = "Five questions drawn from our state rankings. No exact percentages, just the things a curious person should know.";
                vm.FullQuizUrl = "/quizzes/" + RankingsQuiz;
                vm.FullQuizLabel = "Play the full America quiz";
                excludeSlug = RankingsQuiz;
            }

            if (vm.Pool.Count < vm.PerRound) return Content(string.Empty);

            vm.More = _quizzes.GetAll()
                .Where(q => !string.Equals(q.Slug, excludeSlug, StringComparison.OrdinalIgnoreCase)
                            && !q.Slug.EndsWith("-state-symbols-quiz", StringComparison.OrdinalIgnoreCase))
                .OrderBy(_ => Random.Shared.Next())
                .Take(4)
                .ToList();

            return View(vm);
        }
    }
}
