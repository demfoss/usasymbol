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
        private readonly RankingDirectoryService _directory;
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly IComparisonStatsService _statsService;
        private readonly IStateService _stateService;
        private readonly IMapPngService _mapPngService;
        private readonly ILogger<RankingsController> _logger;

        public RankingsController(
            IRankingsContentService service,
            RankingDirectoryService directory,
            ILatestContentRailService latestContentRailService,
            IComparisonStatsService statsService,
            IStateService stateService,
            IMapPngService mapPngService,
            ILogger<RankingsController> logger)
        {
            _service = service;
            _directory = directory;
            _latestContentRailService = latestContentRailService;
            _statsService = statsService;
            _stateService = stateService;
            _mapPngService = mapPngService;
            _logger  = logger;
        }

        [Route("/rankings")]
        public async Task<IActionResult> Index(string? q, string? topic, string? group, string? year, string? sort, int page = 1)
        {
            try
            {
                var categories = await _directory.GetCategoriesAsync();
                var model = RankingDirectoryService.Build(categories, null, q, topic, group, year, sort, page);
                if (model.Page > model.PageCount) return NotFound();
                return View("Directory", model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading rankings hub");
                throw;
            }
        }

        [Route("/rankings/tag/{tag}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> Tag(string tag)
        {
            var page = await _directory.GetTagPageAsync(tag);
            if (page == null) return NotFound();
            if (!string.Equals(tag, page.Slug, System.StringComparison.Ordinal))
                return RedirectPermanent(page.Url);
            return View("Tag", page);
        }

        [Route("/rankings/{category}")]
        public async Task<IActionResult> Category(string category, string? q, string? topic, string? year, string? sort, int page = 1)
        {
            try
            {
                var categories = await _directory.GetCategoriesAsync();
                var cat = categories.FirstOrDefault(c =>
                    string.Equals(c.Id, category, System.StringComparison.OrdinalIgnoreCase));

                if (cat == null) return NotFound();

                ViewData["Title"]       = $"{cat.Title} Rankings";
                ViewData["Description"] = $"Compare U.S. states by {cat.Title.ToLower()}";

                var model = RankingDirectoryService.Build(categories, cat, q, topic, null, year, sort, page);
                if (model.Page > model.PageCount) return NotFound();
                return View("Directory", model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading category: {Category}", category);
                throw;
            }
        }

        [Route("/rankings/{category}/{slug}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> Detail(string category, string slug)
        {
            try
            {
                // Retire the stale 2022 health-category duplicate in favor of the
                // maintained crime ranking. Existing inbound links keep working.
                if (category.Equals("health", System.StringComparison.OrdinalIgnoreCase) &&
                    slug.Equals("states-by-crime-rate", System.StringComparison.OrdinalIgnoreCase))
                    return RedirectPermanent("/rankings/crime/crime-rate-by-state");

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
                        : ChoroplethBuilder.Build(content.Map, content.Table.Rows,
                            content.Table.Columns.FirstOrDefault(column => column.Key == content.Map.MetricKey));

                    var mapPngPath = await _mapPngService.EnsureMapPngAsync(slug, choropleth.Entries);
                    if (mapPngPath != null)
                        ViewData["MapPngPath"] = mapPngPath;

                    var sharePath = await _mapPngService.EnsureSharePngAsync(
                        slug,
                        content.Map.Title ?? content.Page?.H1 ?? slug,
                        choropleth.Entries,
                        choropleth.LegendGroups,
                        content.Page?.Sources?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Name))?.Name,
                        $"https://usasymbol.com{(string.IsNullOrWhiteSpace(content.Url) ? $"/rankings/{category}/{slug}" : content.Url)}");
                    if (sharePath != null)
                        ViewData["MapSharePngPath"] = sharePath;
                }

                // Closest matches go to the sidebar (visible for the whole read, and the
                // generic "Explore more" cards it replaces got few clicks); the rest feed
                // the end-of-article rail, so the two never repeat a link.
                const int sidebarRelatedCount = 4;
                var links = await _directory.GetPageLinksAsync(category, slug, take: sidebarRelatedCount + 6);
                ViewData["RankingChips"] = links.Chips;
                ViewData["RankingSubHub"] = links.SubHub;
                var relatedItems = links.Related.Select(r => new LatestContentRailItemViewModel
                {
                    Title = r.Title,
                    Description = r.Description,
                    Url = r.Url,
                    ImageUrl = string.IsNullOrWhiteSpace(r.HeroImage) ? "/images/usasymbol.png" : r.HeroImage,
                    Eyebrow = r.Topic == "General" ? r.CategoryTitle : r.Topic,
                    SectionLabel = r.CategoryTitle
                }).ToList();
                ViewData["RelatedRankings"] = new LatestContentRailViewModel
                {
                    Title = links.RelatedHeading,
                    Items = relatedItems.Take(sidebarRelatedCount).ToList()
                };
                ViewData["LatestContentRail"] = new LatestContentRailViewModel
                {
                    Title = links.RelatedHeading,
                    Items = relatedItems.Skip(sidebarRelatedCount).ToList()
                };
                return View("Ranking", new PageDetailViewModel { Content = content });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading ranking: {Category}/{Slug}", category, slug);
                throw;
            }
        }

        [HttpGet("/embed/rankings/{category}/{slug}/map")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> MapEmbed(string category, string slug)
        {
            var content = await _service.GetContentAsync(category, slug);
            if (content?.Map == null) return NotFound();

            if (content.ComputedData != null && content.Tables.Count == 0)
                await BuildComputedTableAsync(content);

            Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            Response.Headers["Content-Security-Policy"] = "frame-ancestors *";
            Response.Headers["Cache-Control"] = "public, max-age=3600";

            return View("MapEmbed", new PageDetailViewModel { Content = content });
        }

        [HttpGet("/download/rankings/{category}/{slug}.csv")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> DownloadCsv(string category, string slug)
        {
            var content = await _service.GetContentAsync(category, slug);
            if (content == null) return NotFound();

            if (content.ComputedData != null && content.Tables.Count == 0)
                await BuildComputedTableAsync(content);

            var table = content.Table;
            if (table == null || table.Rows.Count == 0) return NotFound();

            var hidden = new System.Collections.Generic.HashSet<string>(table.HiddenColumns, System.StringComparer.OrdinalIgnoreCase);
            var skipTypes = new[] { "image", "link", "color-swatches" };
            var columns = table.Columns
                .Where(c => c.Key != "state_slug" && !hidden.Contains(c.Key)
                            && !skipTypes.Contains(c.Type, System.StringComparer.OrdinalIgnoreCase))
                .ToList();

            static string Csv(string? value)
            {
                var text = value ?? "";
                // Neutralise spreadsheet formula injection, then quote when needed.
                if (text.Length > 0 && "=+@\t\r".IndexOf(text[0]) >= 0) text = "'" + text;
                return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Join(",", columns.Select(c => Csv(c.Label))));
            foreach (var row in table.Rows)
                sb.AppendLine(string.Join(",", columns.Select(c => Csv(row.GetString(c.Key)))));

            var totals = Usasymbol.Helpers.TableTotals.Resolve(table);
            if (totals != null)
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var labelKey = columns.FirstOrDefault(c => c.Key == "state" || c.Type == "state-link" || c.Type == "text")?.Key;
                string Cell(string label, double value, TableColumn c) =>
                    c.Key == labelKey ? Csv(label)
                    : c.Key == totals.MetricKey ? value.ToString("0.##", inv)
                    : c.Key == "share_of_us" ? (value / totals.Total * 100).ToString("0.0", inv)
                    : "";
                if (totals.ShowOther)
                    sb.AppendLine(string.Join(",", columns.Select(c => Cell(totals.OtherLabel, totals.Other, c))));
                sb.AppendLine(string.Join(",", columns.Select(c => Cell(totals.TotalLabel, totals.Total, c))));
            }

            var pageUrl = $"https://usasymbol.com{(string.IsNullOrWhiteSpace(content.Url) ? $"/rankings/{category}/{slug}" : content.Url)}";
            sb.AppendLine();
            sb.AppendLine(Csv($"\u00A9 {System.DateTime.UtcNow.Year} USA Symbol, {pageUrl}"));

            var bytes = new System.Text.UTF8Encoding(true).GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", slug + ".csv");
        }

        [HttpGet("/embed/rankings/{category}/{slug}/table")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> TableEmbed(string category, string slug)
        {
            var content = await _service.GetContentAsync(category, slug);
            if (content == null) return NotFound();

            if (content.ComputedData != null && content.Tables.Count == 0)
                await BuildComputedTableAsync(content);

            if (content.Table == null || content.Table.Rows.Count == 0) return NotFound();

            Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            Response.Headers["Content-Security-Policy"] = "frame-ancestors *";
            Response.Headers["Cache-Control"] = "public, max-age=3600";

            return View("TableEmbed", new PageDetailViewModel { Content = content });
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
