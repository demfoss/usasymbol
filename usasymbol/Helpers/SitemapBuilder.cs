using Microsoft.EntityFrameworkCore;
using usasymbol.Services;
using USASymbol.Data;
using USASymbol.Extensions;
using USASymbol.Services.Interface;

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

        public SitemapBuilder(
            AppDbContext db,
            IRankingsContentService rankingsService,
            IListingsContentService listingsService,
            ICollectionsContentService collectionsService,
            IBorderService borderService,
            ISurnamesService surnamesService,
            IParkService parkService,
            QuizService quizService,
            IWebHostEnvironment env)
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

            return entries;
        }

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
