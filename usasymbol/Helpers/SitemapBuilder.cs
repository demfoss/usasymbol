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
        private readonly IBorderService _borderService;
        private readonly QuizService _quizService;

        public SitemapBuilder(
            AppDbContext db,
            IRankingsContentService rankingsService,
            IListingsContentService listingsService,
            IBorderService borderService,
            QuizService quizService)
        {
            _db              = db;
            _rankingsService = rankingsService;
            _listingsService = listingsService;
            _borderService   = borderService;
            _quizService     = quizService;
        }

        public async Task<List<string>> BuildMainUrlsAsync()
        {
            var urls = new List<string>();




            urls.Add("/");
            urls.Add("/states");
            urls.Add("/symbols");
            urls.Add("/rankings");
            urls.Add("/guides");
            urls.Add("/guides/state-borders");
            urls.Add("/quizzes");




            var states = await _db.States.ToListAsync();

            foreach (var state in states)
                urls.Add($"/states/{state.Slug}");




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




            var quizzes = _quizService.GetAll();

            foreach (var quiz in quizzes)
                urls.Add($"/quizzes/{quiz.Slug}");

            return urls.Distinct().ToList();
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
