using Microsoft.EntityFrameworkCore;
using usasymbol.Services;
using usasymbol.Services.Interface;
using USASymbol.Data;
using USASymbol.Extensions;
using USASymbol.Models;
using USASymbol.Models.Content;
using USASymbol.Services.Interface;
using Usasymbol.Helpers;

namespace USASymbol.Services
{
    public class SitemapBuilder
    {
        private readonly AppDbContext _db;
        private readonly IRankingsContentService _rankingsService;
        private readonly IListingsContentService _listingsService;
        private readonly ICollectionsContentService _collectionsService;
        private readonly IBorderService _borderService;
        private readonly ISurnamesService _surnamesService;
        private readonly IParkService _parkService;
        private readonly QuizService _quizService;
        private readonly IWebHostEnvironment _env;
        private readonly IMapPngService _mapPngService;
        private readonly IComparisonStatsService _statsService;
        private readonly IStateService _stateService;

        public SitemapBuilder(
            AppDbContext db,
            IRankingsContentService rankingsService,
            IListingsContentService listingsService,
            ICollectionsContentService collectionsService,
            IBorderService borderService,
            ISurnamesService surnamesService,
            IParkService parkService,
            QuizService quizService,
            IWebHostEnvironment env,
            IMapPngService mapPngService,
            IComparisonStatsService statsService,
            IStateService stateService)
        {
            _db                 = db;
            _rankingsService    = rankingsService;
            _listingsService    = listingsService;
            _collectionsService = collectionsService;
            _borderService      = borderService;
            _surnamesService    = surnamesService;
            _parkService        = parkService;
            _quizService        = quizService;
            _env                = env;
            _mapPngService      = mapPngService;
            _statsService       = statsService;
            _stateService       = stateService;
        }

        public async Task<List<string>> BuildMainUrlsAsync()
        {
            var urls = new List<string>();




            urls.Add("/");
            urls.Add("/states");
            urls.Add("/symbols");
            urls.Add("/rankings");
            urls.Add("/guides");
            urls.Add("/guides/state-abbreviations");
            urls.Add("/guides/state-borders");
            urls.Add("/guides/surnames");
            urls.Add("/collections");
            urls.Add("/quizzes");
            urls.Add("/national-parks");




            var states = await _db.States.ToListAsync();

            foreach (var state in states)
            {
                urls.Add($"/states/{state.Slug}");
                urls.Add($"/states/{state.Slug}/abbreviation");
                urls.Add($"/states/{state.Slug}/map");
            }




            var symbols = await _db.Symbols.ToListAsync();

            foreach (var s in symbols)
            {
                var state = states.First(x => x.Id == s.StateId);
                urls.Add(s.ToSymbolUrl(state.Slug));
            }




            foreach (var state in states)
            {
                var border = await _borderService.GetBorderContentAsync(state.Slug);
                if (border != null)
                    urls.Add($"/states/{state.Slug}/borders");
            }




            var surnamesSlugs = _surnamesService.GetAvailableSlugs();

            foreach (var slug in surnamesSlugs)
                urls.Add($"/states/{slug}/surnames");




            var collectionCategories = await _collectionsService.GetAllCategoriesAsync();

            foreach (var cat in collectionCategories)
            {
                urls.Add($"/collections/{cat.Id}");
                foreach (var item in cat.Items)
                    urls.Add(item.Url);
            }




            var rankingCategories = await _rankingsService.GetAllCategoriesAsync();

            foreach (var cat in rankingCategories)
            {
                urls.Add($"/rankings/{cat.Id}");
                foreach (var item in cat.Items)
                    urls.Add(item.Url);
            }




            var listingCategories = await _listingsService.GetAllCategoriesAsync();

            foreach (var cat in listingCategories)
                foreach (var item in cat.Items)
                    urls.Add(item.Url);




            var parks = await _parkService.GetAllNationalParksAsync();

            foreach (var park in parks)
            {
                if (!string.IsNullOrWhiteSpace(park.Slug))
                    urls.Add($"/national-parks/{park.Slug}");
            }




            var parkCollectionsPath = Path.Combine(_env.ContentRootPath, "Content", "parks", "collections");
            if (Directory.Exists(parkCollectionsPath))
            {
                foreach (var file in Directory.EnumerateFiles(parkCollectionsPath, "*.yml"))
                    urls.Add($"/national-parks/{Path.GetFileNameWithoutExtension(file)}");
            }




            var parkStatesPath = Path.Combine(_env.ContentRootPath, "Content", "parks", "states");
            if (Directory.Exists(parkStatesPath))
            {
                foreach (var file in Directory.EnumerateFiles(parkStatesPath, "*.yml"))
                    urls.Add($"/national-parks/in/{Path.GetFileNameWithoutExtension(file)}");
            }




            var quizzes = _quizService.GetAll();

            foreach (var quiz in quizzes)
                urls.Add($"/quizzes/{quiz.Slug}");

            return urls.Distinct().ToList();
        }

        public async Task<List<(string PageUrl, string ImageUrl, string Title)>> BuildImageEntriesAsync()
        {
            var entries = new List<(string PageUrl, string ImageUrl, string Title)>();
            var states = await _db.States.ToListAsync();
            var symbols = await _db.Symbols.ToListAsync();

            foreach (var state in states)
            {
                if (!string.IsNullOrWhiteSpace(state.FlagImageUrl))
                    entries.Add(($"/states/{state.Slug}", state.FlagImageUrl, $"{state.Name} State Flag"));
            }

            foreach (var symbol in symbols)
            {
                if (string.IsNullOrWhiteSpace(symbol.ImageUrl)) continue;
                var state = states.FirstOrDefault(x => x.Id == symbol.StateId);
                if (state == null) continue;
                var pageUrl = symbol.ToSymbolUrl(state.Slug);
                var typeDisplay = string.IsNullOrWhiteSpace(symbol.Designation)
                    ? (string.IsNullOrWhiteSpace(symbol.Type) ? "Symbol" : char.ToUpper(symbol.Type[0]) + symbol.Type[1..])
                    : symbol.Designation;
                entries.Add((pageUrl, symbol.ImageUrl, $"{symbol.Name} – {state.Name} {typeDisplay}"));
            }

            entries.AddRange(await BuildRankingImageEntriesAsync());

            return entries;
        }

        private async Task<List<(string PageUrl, string ImageUrl, string Title)>> BuildRankingImageEntriesAsync()
        {
            var entries = new List<(string PageUrl, string ImageUrl, string Title)>();
            var categories = await _rankingsService.GetAllCategoriesAsync();

            foreach (var category in categories)
            {
                foreach (var item in category.Items)
                {
                    var slug = item.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    if (string.IsNullOrWhiteSpace(slug))
                    {
                        continue;
                    }

                    var content = await _rankingsService.GetContentAsync(category.Id, slug);
                    if (content?.Map == null || string.IsNullOrWhiteSpace(content.Url))
                    {
                        continue;
                    }

                    var imageTitle = !string.IsNullOrWhiteSpace(content.Map.Title)
                        ? content.Map.Title
                        : (!string.IsNullOrWhiteSpace(content.Page.H1) ? content.Page.H1 : item.Title);

                    if (!string.IsNullOrWhiteSpace(content.Map.Image))
                    {
                        entries.Add((content.Url, content.Map.Image, imageTitle));
                    }

                    var generatedMapPath = await EnsureRankingMapImageAsync(content);
                    if (!string.IsNullOrWhiteSpace(generatedMapPath))
                    {
                        entries.Add((content.Url, generatedMapPath, imageTitle));
                    }
                }
            }

            return entries
                .Distinct()
                .ToList();
        }

        private async Task<string?> EnsureRankingMapImageAsync(PageContent content)
        {
            if (content.Map == null)
            {
                return null;
            }

            if (content.ComputedData != null && content.Tables.Count == 0)
            {
                await BuildComputedRankingTableAsync(content);
            }

            var primaryTable = content.Table;
            if (primaryTable?.Rows?.Count <= 0)
            {
                return null;
            }

            var rows = primaryTable!.Rows;
            var choropleth = string.IsNullOrWhiteSpace(content.Map.MetricKey)
                ? ChoroplethBuilder.BuildFlat(rows)
                : ChoroplethBuilder.Build(content.Map, rows);

            if (choropleth.Entries.Count == 0)
            {
                return null;
            }

            var slug = !string.IsNullOrWhiteSpace(content.Slug)
                ? content.Slug
                : content.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

            return string.IsNullOrWhiteSpace(slug)
                ? null
                : await _mapPngService.EnsureMapPngAsync(slug, choropleth.Entries);
        }

        private async Task BuildComputedRankingTableAsync(PageContent content)
        {
            var cfg = content.ComputedData;
            if (cfg == null)
            {
                return;
            }

            var allStats = await _statsService.GetAllStatsAsync();
            var allStates = await _stateService.GetAllStatesAsync();
            var nameMap = allStates.ToDictionary(s => s.Slug, s => s.Name, StringComparer.OrdinalIgnoreCase);

            var entries = new List<(string slug, string name, double value)>();
            foreach (var (slug, stats) in allStats)
            {
                var value = GetStatValue(stats, cfg.Field);
                if (!value.HasValue)
                {
                    continue;
                }

                if (!nameMap.TryGetValue(slug, out var name))
                {
                    continue;
                }

                entries.Add((slug, name, value.Value));
            }

            entries = cfg.Sort == "asc"
                ? entries.OrderBy(e => e.value).ThenBy(e => e.name).ToList()
                : entries.OrderByDescending(e => e.value).ThenBy(e => e.name).ToList();

            var table = new PageTable
            {
                Searchable = true,
                Sortable = true,
                DefaultColumn = cfg.MetricKey
            };

            table.Columns.Add(new TableColumn { Key = "rank", Label = "Rank", Type = "rank", Sortable = true });
            table.Columns.Add(new TableColumn { Key = "state", Label = "State", Type = "state-link", Sortable = true });
            table.Columns.Add(new TableColumn { Key = cfg.MetricKey, Label = cfg.Label, Type = "number", Format = cfg.Format, Sortable = true });

            for (int i = 0; i < entries.Count; i++)
            {
                var (slug, name, value) = entries[i];
                var row = new TableRow();
                row.Data["rank"] = i + 1;
                row.Data["state"] = name;
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

        public Task<List<string>> BuildCompareUrlsAsync()
        {
            var urls = new List<string>
            {
                "/compare-states"
            };

            foreach (var (slug1, slug2) in ComparisonService.TopComparisonPairs)
            {
                var pairSlug = ComparisonService.CanonicalPairSlug(slug1, slug2);
                urls.Add($"/compare/{pairSlug}");
            }

            var metricSlugs = ComparisonMetricsConfig.All.Select(metric => metric.Slug).ToList();
            foreach (var (slug1, slug2) in ComparisonService.TierOneComparisonPairs)
            {
                var pairSlug = ComparisonService.CanonicalPairSlug(slug1, slug2);
                foreach (var metricSlug in metricSlugs)
                    urls.Add($"/compare/{pairSlug}/{metricSlug}");
            }

            return Task.FromResult(urls.Distinct().ToList());
        }

        public async Task<List<string>> BuildUrlsAsync()
        {
            var urls = new List<string>();

            urls.AddRange(await BuildMainUrlsAsync());
            urls.AddRange(await BuildCompareUrlsAsync());

            return urls.Distinct().ToList();
        }
    }
}
