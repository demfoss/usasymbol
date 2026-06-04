using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;
using YamlDotNet.Serialization;

namespace USASymbol.Controllers
{
    public class ParkController : Controller
    {
        private readonly IParkService _parkService;
        private readonly ILogger<ParkController> _logger;
        private readonly IWebHostEnvironment _env;

        public ParkController(IParkService parkService, ILogger<ParkController> logger, IWebHostEnvironment env)
        {
            _parkService = parkService;
            _logger = logger;
            _env = env;
        }

        [Route("national-parks")]
        public async Task<IActionResult> Index()
        {
            var parks = await _parkService.GetAllNationalParksAsync();
            ViewData["Title"] = "All 63 US National Parks Ranked Best to Worst";
            ViewData["Description"] = "Every US national park ranked from best to worst — scores for scenery, hiking, wildlife, accessibility, and crowds. Find the right park for your next trip.";
            ViewData["Canonical"] = "/national-parks";
            return View(parks);
        }

        // More specific route — matched before {slug} when URL is /national-parks/in/california
        [Route("national-parks/in/{stateSlug}")]
        public async Task<IActionResult> ByState(string stateSlug)
        {
            var stateName = SlugToTitleCase(stateSlug);
            var all = await _parkService.GetAllNationalParksAsync();
            var parks = all
                .Where(p => p.Location.State.Contains(stateName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Stats.VisitationRank > 0 ? p.Stats.VisitationRank : 999)
                .ThenBy(p => p.Name)
                .ToList();

            if (parks.Count == 0)
                return NotFound();

            // Load hand-written content from YAML if it exists, otherwise auto-generate
            var (manualIntro, manualQuickAnswer) = await LoadStateContentAsync(stateSlug);
            var intro       = manualIntro       ?? BuildStateIntro(stateName, parks);
            var quickAnswer = manualQuickAnswer ?? BuildStateQuickAnswer(stateName, parks);
            var seoDesc = $"All {parks.Count} national parks in {stateName} with map, entrance fees, area, and visitor numbers. " + quickAnswer;

            var vm = new ParkCollectionViewModel
            {
                Parks = parks,
                StateDisplayName = stateName,
                Config = new Models.Content.ParkCollectionConfig
                {
                    Slug = $"in-{stateSlug}",
                    H1 = $"National Parks in {stateName}",
                    SeoTitle = $"National Parks in {stateName}: Map & Complete List ({parks.Count} Parks)",
                    SeoDescription = seoDesc,
                    Intro = intro,
                    QuickAnswer = quickAnswer,
                    MetricFormat = "text",
                    SortBy = "visitation_rank",
                    Updated = "2026",
                },
            };

            ViewData["Title"] = vm.Config.SeoTitle;
            ViewData["Description"] = vm.Config.SeoDescription;
            ViewData["Canonical"] = $"/national-parks/in/{stateSlug}";
            return View("Collection", vm);
        }

        private static string BuildStateIntro(string stateName, List<ParkContent> parks)
        {
            var count = parks.Count;
            if (count == 0) return "";

            static string ShortName(string name) => name
                .Replace(" National Park & Preserve", "")
                .Replace(" National Park and Preserve", "")
                .Replace(" National Park", "")
                .Replace(" National Preserve", "")
                .Replace(" National Monument", "");

            var topNames = string.Join(", ", parks.Take(Math.Min(3, count)).Select(p => ShortName(p.Name)));
            var moreSuffix = count > 3 ? $", and {count - 3} more" : "";

            var sentence1 = count == 1
                ? $"{parks[0].Name} is the only national park in {stateName}."
                : $"{stateName} has {count} national parks — {topNames}{moreSuffix}.";

            var extras = new List<string>();
            var freeCount = parks.Count(p => !p.Filters.HasEntranceFee);
            if (freeCount > 0 && freeCount < count)
                extras.Add($"{freeCount} {(freeCount == 1 ? "park has" : "parks have")} free admission");

            var largest = parks.Where(p => p.Stats.AreaAcres > 0).OrderByDescending(p => p.Stats.AreaAcres).FirstOrDefault();
            if (largest != null && count > 1)
                extras.Add($"{ShortName(largest.Name)} is the largest at {largest.Stats.AreaAcres:N0} acres");

            var sentence2 = extras.Count > 0 ? " " + string.Join("; ", extras) + "." : "";
            return sentence1 + sentence2;
        }

        private static string BuildStateQuickAnswer(string stateName, List<ParkContent> parks)
        {
            if (parks.Count == 0) return "";
            static string ShortName(string name) => name
                .Replace(" National Park & Preserve", "")
                .Replace(" National Park and Preserve", "")
                .Replace(" National Park", "")
                .Replace(" National Preserve", "")
                .Replace(" National Monument", "");
            var names = string.Join(", ", parks.Select(p => ShortName(p.Name)));
            return $"{stateName} has {parks.Count} national park{(parks.Count > 1 ? "s" : "")}: {names}.";
        }

        // Dispatches to either a collection page or a park detail page
        [Route("national-parks/{slug}")]
        public async Task<IActionResult> Show(string slug)
        {
            // 1. Collection config takes priority
            var config = await _parkService.GetCollectionConfigAsync(slug);
            if (config != null)
            {
                var parks = await _parkService.GetCollectionParksAsync(config);
                var vm = new ParkCollectionViewModel { Config = config, Parks = parks };

                ViewData["Title"] = config.SeoTitle;
                ViewData["Description"] = config.SeoDescription;
                ViewData["Canonical"] = $"/national-parks/{slug}";
                return View("Collection", vm);
            }

            // 2. Fall back to park detail
            var park = await _parkService.GetParkAsync(slug);
            if (park == null)
            {
                _logger.LogWarning("Park not found: {Slug}", slug);
                return NotFound();
            }

            var allParks = await _parkService.GetAllNationalParksAsync();
            var ranked = allParks
                .Where(p => p.Rankings.OverallScore > 0)
                .OrderByDescending(p => p.Rankings.OverallScore)
                .ThenByDescending(p => p.Rankings.Personality)
                .ToList();
            var scoreRank = ranked.FindIndex(p => p.Slug == slug) + 1;

            var detailVm = new ParkDetailViewModel
            {
                Park = park,
                ScoreRank = scoreRank,
                TotalRanked = ranked.Count,
            };

            ViewData["Title"] = park.SeoTitle ?? park.Name;
            ViewData["Description"] = park.SeoDescription ?? park.IntroText;
            ViewData["Canonical"] = $"/national-parks/{slug}";

            if (!string.IsNullOrWhiteSpace(park.Media.HeroImage))
                ViewData["OgImage"] = park.Media.HeroImage;

            return View("Detail", detailVm);
        }

        private static string SlugToTitleCase(string slug) =>
            string.Join(" ", slug.Split('-')
                .Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w[1..].ToLower() : w));

        // Load hand-written intro/quick_answer from Content/parks/states/{slug}.yml
        // Returns (null, null) if file doesn't exist — caller falls back to auto-generated text
        private async Task<(string? Intro, string? QuickAnswer)> LoadStateContentAsync(string stateSlug)
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "parks", "states", $"{stateSlug}.yml");
            if (!System.IO.File.Exists(path))
                return (null, null);

            try
            {
                var raw  = await System.IO.File.ReadAllTextAsync(path);
                var yaml = new DeserializerBuilder().Build();
                var data = yaml.Deserialize<Dictionary<object, object>>(raw);
                if (data == null) return (null, null);

                string? Get(string key) =>
                    data.TryGetValue(key, out var v) ? v?.ToString()?.Trim() : null;

                return (Get("intro"), Get("quick_answer"));
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
