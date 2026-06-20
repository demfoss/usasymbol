using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using USASymbol.Models;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using USASymbol.Services.Interface;
using usasymbol.Services.Interface;
using Usasymbol.Helpers;

namespace USASymbol.Controllers
{
    public class RankingsController : Controller
    {
        private readonly IRankingsContentService _service;
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly IComparisonStatsService _statsService;
        private readonly IStateService _stateService;
        private readonly IMapPngService _mapPngService;
        private readonly ILogger<RankingsController> _logger;

        public RankingsController(
            IRankingsContentService service,
            ILatestContentRailService latestContentRailService,
            IComparisonStatsService statsService,
            IStateService stateService,
            IMapPngService mapPngService,
            ILogger<RankingsController> logger)
        {
            _service = service;
            _latestContentRailService = latestContentRailService;
            _statsService = statsService;
            _stateService = stateService;
            _mapPngService = mapPngService;
            _logger  = logger;
        }

        [Route("/rankings")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _service.GetAllCategoriesAsync();
                return View(new PageHubViewModel { Categories = categories });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading rankings hub");
                throw;
            }
        }

        [Route("/rankings/{category}")]
        public async Task<IActionResult> Category(string category)
        {
            try
            {
                var categories = await _service.GetAllCategoriesAsync();
                var cat = categories.Find(c =>
                    string.Equals(c.Id, category, System.StringComparison.OrdinalIgnoreCase));

                if (cat == null) return NotFound();

                ViewData["Title"]       = $"{cat.Title} Rankings";
                ViewData["Description"] = $"Compare U.S. states by {cat.Title.ToLower()}";

                return View("Category", BuildCategoryViewModel(cat));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading category: {Category}", category);
                throw;
            }
        }

        [Route("/rankings/{category}/{slug}")]
        public async Task<IActionResult> Detail(string category, string slug)
        {
            try
            {
                var content = await _service.GetContentAsync(category, slug);
                if (content == null)
                {
                    _logger.LogWarning("Ranking not found: {Category}/{Slug}", category, slug);
                    return NotFound();
                }

                if (content.ComputedData != null && content.Tables.Count == 0)
                    await BuildComputedTableAsync(content);

                // Pre-generate map PNG so OG image and hero fallback are available immediately
                if (content.Map != null && content.Table?.Rows?.Count > 0)
                {
                    var choropleth = string.IsNullOrWhiteSpace(content.Map.MetricKey)
                        ? ChoroplethBuilder.BuildFlat(content.Table.Rows)
                        : ChoroplethBuilder.Build(content.Map, content.Table.Rows);

                    var mapPngPath = await _mapPngService.EnsureMapPngAsync(slug, choropleth.Entries);
                    if (mapPngPath != null)
                        ViewData["MapPngPath"] = mapPngPath;
                }

                ViewData["LatestContentRail"] = await _latestContentRailService.GetLatestItemsAsync(8);
                return View("Ranking", new PageDetailViewModel { Content = content });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading ranking: {Category}/{Slug}", category, slug);
                throw;
            }
        }

        private static PageCategoryViewModel BuildCategoryViewModel(PageCategory cat)
        {
            var mostPopular = cat.Items
                .OrderByDescending(i => i.DateModified ?? i.DatePublished ?? System.DateTime.MinValue)
                .Take(2)
                .ToList();

            var subcategoryFilters = cat.Items
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Subcategory) ? "Not set" : i.Subcategory!)
                .Select(g => new SubcategoryFilterOption { Value = g.Key, Label = g.Key, Count = g.Count() })
                .OrderBy(f => f.Value == "Not set" ? 1 : 0)
                .ThenByDescending(f => f.Count)
                .ToList();

            return new PageCategoryViewModel
            {
                Category = cat,
                MostPopular = mostPopular,
                SubcategoryFilters = subcategoryFilters,
            };
        }

        private async Task BuildComputedTableAsync(PageContent content)
        {
            var cfg = content.ComputedData!;
            var allStats = await _statsService.GetAllStatsAsync();
            var allStates = await _stateService.GetAllStatesAsync();
            var nameMap = allStates.ToDictionary(s => s.Slug, s => s.Name, System.StringComparer.OrdinalIgnoreCase);

            var entries = new List<(string slug, string name, double value)>();
            foreach (var (slug, stats) in allStats)
            {
                var value = GetStatValue(stats, cfg.Field);
                if (!value.HasValue) continue;
                if (!nameMap.TryGetValue(slug, out var name)) continue;
                entries.Add((slug, name, value.Value));
            }

            entries = cfg.Sort == "asc"
                ? entries.OrderBy(e => e.value).ThenBy(e => e.name).ToList()
                : entries.OrderByDescending(e => e.value).ThenBy(e => e.name).ToList();

            var table = new PageTable { Searchable = true, Sortable = true, DefaultColumn = cfg.MetricKey };
            table.Columns.Add(new TableColumn { Key = "rank",       Label = "Rank",       Type = "rank",       Sortable = true });
            table.Columns.Add(new TableColumn { Key = "state",      Label = "State",      Type = "state-link", Sortable = true });
            table.Columns.Add(new TableColumn { Key = cfg.MetricKey, Label = cfg.Label,   Type = "number",     Format = cfg.Format, Sortable = true });

            for (int i = 0; i < entries.Count; i++)
            {
                var (slug, name, value) = entries[i];
                var row = new TableRow();
                row.Data["rank"]       = i + 1;
                row.Data["state"]      = name;
                row.Data["state_slug"] = slug;
                row.Data[cfg.MetricKey] = value;
                table.Rows.Add(row);
            }

            content.Tables.Add(table);
        }

        private static double? GetStatValue(StateStats stats, string field) => field switch
        {
            "median_household_income"      => stats.MedianHouseholdIncome,
            "median_home_value"            => stats.MedianHomeValue,
            "median_gross_rent"            => stats.MedianGrossRent,
            "cost_of_living_index"         => stats.CostOfLivingIndex,
            "income_tax_rate_pct"          => stats.IncomeTaxRatePct,
            "sales_tax_rate_pct"           => stats.SalesTaxRatePct,
            "property_tax_rate_pct"        => stats.PropertyTaxRatePct,
            "gas_tax_cents"                => stats.GasTaxCents,
            "unemployment_rate_pct"        => stats.UnemploymentRatePct,
            "poverty_rate_pct"             => stats.PovertyRatePct,
            "gas_price_regular"            => stats.GasPriceRegular,
            "electricity_rate_cents_kwh"   => stats.ElectricityRateCentsKwh,
            "life_expectancy_years"        => stats.LifeExpectancyYears,
            "violent_crime_rate_per_100k"  => stats.ViolentCrimeRatePer100k,
            "property_crime_rate_per_100k" => stats.PropertyCrimeRatePer100k,
            "obesity_rate_pct"             => stats.ObesityRatePct,
            "uninsured_rate_pct"           => stats.UninsuredRatePct,
            "k12_rank"                     => stats.K12Rank,
            "high_school_graduation_pct"   => stats.HighSchoolGraduationPct,
            "college_educated_pct"         => stats.CollegeEducatedPct,
            "student_teacher_ratio"        => stats.StudentTeacherRatio,
            "mean_commute_minutes"         => stats.MeanCommuteMinutes,
            "average_temperature_f"        => stats.AverageTemperatureF,
            "annual_precipitation_in"      => stats.AnnualPrecipitationIn,
            "sunny_days_per_year"          => stats.SunnyDaysPerYear,
            "homeownership_rate_pct"       => stats.HomeownershipRatePct,
            "purchasing_power_100"         => stats.PurchasingPower100,
            _                              => null
        };
    }
}
