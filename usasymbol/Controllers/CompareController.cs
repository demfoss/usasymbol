using Microsoft.AspNetCore.Mvc;
using USASymbol.Services;
using USASymbol.Services.Interface;

namespace USASymbol.Controllers
{
    public class CompareController : Controller
    {
        private readonly IComparisonService _comparison;

        public CompareController(IComparisonService comparison)
        {
            _comparison = comparison;
        }

        // GET /compare-states
        public async Task<IActionResult> Hub()
        {
            var model = await _comparison.GetHubViewModelAsync();

            ViewData["Title"] = "Compare U.S. States | Size, Population, Income & More";
            ViewData["Description"] = "Compare any two U.S. states side by side. Explore population, housing, taxes, political lean, state control, income, land area, and more.";

            return View(model);
        }

        // GET /compare/{pair}  e.g. /compare/california-vs-texas
        public async Task<IActionResult> Overview(string pair)
        {
            var (slugA, slugB) = ParsePair(pair);
            if (slugA == null || slugB == null)
                return NotFound();

            // Canonical redirect — always alphabetical
            var canonical = ComparisonService.CanonicalPairSlug(slugA, slugB);
            if (pair != canonical)
                return RedirectPermanent($"/compare/{canonical}");

            var model = await _comparison.GetPairComparisonAsync(slugA, slugB);
            if (model == null) return NotFound();

            ViewData["Title"] = $"{model.StateA.Name} vs {model.StateB.Name} | State Comparison";
            ViewData["Description"] = $"Compare {model.StateA.Name} and {model.StateB.Name} by population, housing, taxes, laws, politics, income, land area, and more.";
            ViewData["Canonical"] = $"/compare/{canonical}";

            return View(model);
        }

        // GET /compare/{pair}/{metric}  e.g. /compare/california-vs-texas/population
        public async Task<IActionResult> Metric(string pair, string metric)
        {
            var (slugA, slugB) = ParsePair(pair);
            if (slugA == null || slugB == null)
                return NotFound();

            // Canonical redirect
            var canonical = ComparisonService.CanonicalPairSlug(slugA, slugB);
            if (pair != canonical)
                return RedirectPermanent($"/compare/{canonical}/{metric}");

            var model = await _comparison.GetMetricComparisonAsync(slugA, slugB, metric);
            if (model == null) return NotFound();

            ViewData["Title"] = $"{model.StateA.Name} vs {model.StateB.Name} | {model.Metric.Name} Comparison";
            ViewData["Description"] = $"{model.Result.SummaryText ?? $"Compare {model.Metric.Name.ToLower()} between {model.StateA.Name} and {model.StateB.Name}."}";
            ViewData["Canonical"] = $"/compare/{canonical}/{metric}";

            return View(model);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        /// <summary>Parses "california-vs-texas" → ("california", "texas"). Returns nulls on failure.</summary>
        private static (string?, string?) ParsePair(string pair)
        {
            if (string.IsNullOrWhiteSpace(pair)) return (null, null);
            var idx = pair.IndexOf("-vs-", StringComparison.Ordinal);
            if (idx <= 0 || idx >= pair.Length - 4) return (null, null);
            var a = pair[..idx];
            var b = pair[(idx + 4)..];
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return (null, null);
            return (a, b);
        }
    }
}
