using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using USASymbol.Services.Interface;
using Usasymbol.Helpers;

namespace USASymbol.Controllers
{
    public class CompareController : Controller
    {
        private readonly IComparisonService _comparison;
        private readonly IRankingsContentService _rankingsContent;

        public CompareController(IComparisonService comparison, IRankingsContentService rankingsContent)
        {
            _comparison = comparison;
            _rankingsContent = rankingsContent;
        }

        private static bool IsValidCategory(string category) =>
            ComparisonMetricsConfig.GroupOrder.Any(g => string.Equals(g.Slug, category, StringComparison.OrdinalIgnoreCase));

        private static string GetCategoryName(string category) =>
            ComparisonMetricsConfig.GroupOrder.First(g => string.Equals(g.Slug, category, StringComparison.OrdinalIgnoreCase)).Name;

        // GET /compare-states
        public async Task<IActionResult> Hub()
        {
            var model = await _comparison.GetHubViewModelAsync();

            ViewData["Title"] = "Compare U.S. States Side by Side (2026)";
            ViewData["Description"] = "Compare any two U.S. states by cost of living, taxes, housing, jobs, climate, safety, laws, health, and quality of life with sourced data.";

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

            ViewData["Title"] = ComparisonSeoTemplates.OverviewTitle(model);
            ViewData["Description"] = ComparisonSeoTemplates.OverviewDescription(model);
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

            var metricDefinition = ComparisonMetricsConfig.GetBySlug(metric);
            if (metricDefinition == null)
                return NotFound();

            if (!string.Equals(metric, metricDefinition.Slug, StringComparison.OrdinalIgnoreCase))
                return RedirectPermanent($"/compare/{canonical}/{metricDefinition.Slug}");

            var model = await _comparison.GetMetricComparisonAsync(slugA, slugB, metric);
            if (model == null) return NotFound();

            ViewData["Title"] = ComparisonSeoTemplates.MetricTitle(model);
            ViewData["Description"] = ComparisonSeoTemplates.MetricDescription(model);
            ViewData["Canonical"] = $"/compare/{canonical}/{metric}";

            return View(model);
        }

        // GET /compare/{category}  e.g. /compare/demographics
        public async Task<IActionResult> CategoryHub(string category)
        {
            if (!IsValidCategory(category)) return NotFound();

            var (table, highlights) = await _comparison.GetCategoryStatesTableAsync(category);
            if (table == null) return NotFound();

            var categoryName = GetCategoryName(category);
            var description = ComparisonCategoryCopy.Get(category, categoryName);
            var hub = await _comparison.GetHubViewModelAsync();

            var model = new CompareCategoryHubViewModel
            {
                CategorySlug = category,
                CategoryName = categoryName,
                Description = description,
                States = hub.States,
                StatesTable = table,
                Highlights = highlights,
                FeaturedPairs = hub.FeaturedPairs.Take(6).ToList()
            };

            var featuredPair = model.FeaturedPairs.FirstOrDefault();
            if (featuredPair.Slug1 != null && featuredPair.Slug2 != null)
            {
                var featuredPairModel = await _comparison.GetPairComparisonAsync(featuredPair.Slug1, featuredPair.Slug2);
                if (featuredPairModel != null)
                {
                    model.FeaturedComparison = ComparisonScoreCalculator.CalculateForCategory(featuredPairModel, category);
                }
            }

            if (ComparisonCategoryCopy.RankingsCategoryMap.TryGetValue(category, out var rankingsCategoryId))
            {
                var rankingsCategories = await _rankingsContent.GetAllCategoriesAsync();
                var rankingsCategory = rankingsCategories.Find(c => string.Equals(c.Id, rankingsCategoryId, StringComparison.OrdinalIgnoreCase));
                if (rankingsCategory != null)
                {
                    model.RankingsCategoryId = rankingsCategory.Id;
                    model.RankingsCategoryTitle = rankingsCategory.Title;
                    model.RankingsItemCount = rankingsCategory.Items.Count;
                }
            }

            ViewData["Title"] = $"{categoryName} by State: Compare All 50 States (2026)";
            ViewData["Description"] = $"2026 state comparison: {description}";
            ViewData["Canonical"] = $"/compare/{category}";

            return View(model);
        }

        // GET /compare/{category}/{pair}  e.g. /compare/demographics/california-vs-texas
        public async Task<IActionResult> CategoryPair(string category, string pair)
        {
            if (!IsValidCategory(category)) return NotFound();

            var (slugA, slugB) = ParsePair(pair);
            if (slugA == null || slugB == null)
                return NotFound();

            var canonical = ComparisonService.CanonicalPairSlug(slugA, slugB);
            if (pair != canonical)
                return RedirectPermanent($"/compare/{category}/{canonical}");

            var pairModel = await _comparison.GetPairComparisonAsync(slugA, slugB);
            if (pairModel == null) return NotFound();

            var categoryResults = pairModel.MetricResults
                .Where(r => string.Equals(r.Metric.GroupSlug, category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Metric.SortOrder)
                .ToList();
            if (categoryResults.Count == 0) return NotFound();

            var categoryName = GetCategoryName(category);

            var model = new CompareCategoryPairViewModel
            {
                CategorySlug = category,
                CategoryName = categoryName,
                Pair = pairModel,
                CategoryResults = categoryResults,
                Spotlights = CategorySpotlightCardBuilder.Build(categoryResults, pairModel.PairSlug)
            };

            ViewData["Title"] = ComparisonSeoTemplates.CategoryPairTitle(model);
            ViewData["Description"] = $"Compare {pairModel.StateA.Name} and {pairModel.StateB.Name} by {categoryName.ToLowerInvariant()}: " +
                string.Join(", ", categoryResults.Take(3).Select(r => r.Metric.Name.ToLowerInvariant())) + ", and more.";
            ViewData["Canonical"] = $"/compare/{category}/{canonical}";

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
