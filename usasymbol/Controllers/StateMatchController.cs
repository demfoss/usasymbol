using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Controllers
{
    public sealed class StateMatchController : Controller
    {
        private readonly IStateMatchService _stateMatchService;
        private readonly IStateMatchOgImageService _ogImageService;

        public StateMatchController(IStateMatchService stateMatchService, IStateMatchOgImageService ogImageService)
        {
            _stateMatchService = stateMatchService;
            _ogImageService = ogImageService;
        }

        [HttpGet("/state-match")]
        public async Task<IActionResult> Index(string? state, string? w)
        {
            var model = await _stateMatchService.BuildAsync();

            ViewData["Title"] = "Find Your Best State to Live In | State Match";
            ViewData["Description"] = "Adjust cost, jobs, housing, safety, climate, healthcare, education, and tax priorities to find the U.S. states that fit you best.";
            ViewData["Canonical"] = "/state-match";
            ViewData["BodyClass"] = "state-match-surface";

            if (!string.IsNullOrWhiteSpace(state) &&
                model.Places.Any(place => string.Equals(place.Slug, state, StringComparison.OrdinalIgnoreCase)))
            {
                ViewData["OgImage"] = "/state-match/og.png?state=" + Uri.EscapeDataString(state) +
                    (string.IsNullOrWhiteSpace(w) ? "" : "&w=" + Uri.EscapeDataString(w));
            }

            return View(model);
        }

        [HttpGet("/match/{slug}")]
        public async Task<IActionResult> Preset(string slug)
        {
            var preset = StateMatchPresetCatalog.Find(slug);
            if (preset == null)
            {
                return NotFound();
            }

            var tool = await _stateMatchService.BuildAsync();
            var ranked = await _stateMatchService.RankAsync(preset.Weights, tool);

            ViewData["Title"] = preset.MetaTitle;
            ViewData["Description"] = preset.MetaDescription;
            ViewData["Canonical"] = "/match/" + preset.Slug;
            ViewData["BodyClass"] = "state-match-surface";

            if (ranked.Count > 0)
            {
                ViewData["OgImage"] = "/state-match/og.png?state=" + Uri.EscapeDataString(ranked[0].Place.Slug) +
                    "&w=" + Uri.EscapeDataString(BuildWeightVector(preset.Weights));
            }

            var model = new StateMatchPresetPageViewModel
            {
                Tool = tool,
                Slug = preset.Slug,
                Heading = preset.Heading,
                Lede = preset.Lede,
                InitialWeights = preset.Weights,
                Top10 = ranked.Take(10).ToList(),
                OtherPresets = StateMatchPresetCatalog.All
                    .Where(other => other.Slug != preset.Slug)
                    .Select(other => new StateMatchPresetLinkViewModel(other.Slug, other.NavLabel))
                    .ToList()
            };

            return View("Preset", model);
        }

        [HttpGet("/state-match/methodology")]
        public async Task<IActionResult> Methodology()
        {
            var model = await _stateMatchService.BuildAsync();

            ViewData["Title"] = "How State Match Scoring Works | Methodology";
            ViewData["Description"] = "The formula, data sources, and update cadence behind every State Match score: how metrics are normalized, weighted, and ranked.";
            ViewData["Canonical"] = "/state-match/methodology";
            ViewData["BodyClass"] = "state-match-surface";

            return View(model);
        }

        /// <summary>
        /// Read-only JSON mirror of the tool's own scoring — same weight vector format as the
        /// "?w=" share links. Unauthenticated and unversioned: fine for the site's own use and
        /// for people who find it, but add auth/rate limiting before promoting it as a public API.
        /// </summary>
        [HttpGet("/api/state-match/rank")]
        public async Task<IActionResult> Rank(string? w)
        {
            var tool = await _stateMatchService.BuildAsync();
            var weights = string.IsNullOrWhiteSpace(w)
                ? tool.MetricOptions.ToDictionary(option => option.Key, option => option.DefaultWeight)
                : ParseWeightVector(w);

            var ranked = await _stateMatchService.RankAsync(weights, tool);

            return Json(new
            {
                weights,
                generatedAt = DateTimeOffset.UtcNow,
                results = ranked.Select(row => new
                {
                    rank = row.Rank,
                    state = row.Place.Name,
                    abbreviation = row.Place.Abbreviation,
                    slug = row.Place.Slug,
                    score = row.Score
                })
            });
        }

        /// <summary>
        /// A tiny top-5 card meant for other sites to iframe (relocation forums, realtor blogs) —
        /// its own bare layout with an attribution link back, no site nav/header/ads.
        /// </summary>
        [HttpGet("/state-match/widget/{preset?}")]
        public async Task<IActionResult> Widget(string? preset)
        {
            var tool = await _stateMatchService.BuildAsync();
            var presetDef = preset != null ? StateMatchPresetCatalog.Find(preset) : null;
            var weights = presetDef?.Weights
                ?? tool.MetricOptions.ToDictionary(option => option.Key, option => option.DefaultWeight);

            var ranked = await _stateMatchService.RankAsync(weights, tool);

            var quickPresetSlugs = new[] { "low-taxes", "best-states-for-families", "warm-climate" };
            var model = new StateMatchWidgetViewModel
            {
                PresetSlug = presetDef?.Slug,
                PresetLabel = presetDef?.NavLabel ?? "Balanced",
                Top5 = ranked.Take(5).ToList(),
                QuickPresets = quickPresetSlugs
                    .Select(StateMatchPresetCatalog.Find)
                    .Where(match => match != null)
                    .Select(match => new StateMatchPresetLinkViewModel(match!.Slug, match.NavLabel))
                    .ToList()
            };

            Response.Headers.CacheControl = "public, max-age=3600";
            return View(model);
        }

        /// <summary>
        /// The "Your state — North Dakota, 69/100" share-preview image behind og:image on
        /// /state-match?state=... and every /match/{slug} preset page. Cached a day per URL
        /// since a given state+weight combination always scores the same.
        /// </summary>
        [HttpGet("/state-match/og.png")]
        public async Task<IActionResult> OgImage(string state, string? w)
        {
            var tool = await _stateMatchService.BuildAsync();
            var place = tool.Places.FirstOrDefault(p => string.Equals(p.Slug, state, StringComparison.OrdinalIgnoreCase));
            if (place == null)
            {
                return NotFound();
            }

            var weights = string.IsNullOrWhiteSpace(w)
                ? tool.MetricOptions.ToDictionary(option => option.Key, option => option.DefaultWeight)
                : ParseWeightVector(w);

            var ranked = await _stateMatchService.RankAsync(weights, tool);
            var row = ranked.FirstOrDefault(r => r.Place.Slug == place.Slug);
            if (row == null)
            {
                return NotFound();
            }

            var png = _ogImageService.Render(place.Name, place.Abbreviation, row.Score, row.StrongestMetricName);
            Response.Headers.CacheControl = "public, max-age=86400";
            return File(png, "image/png");
        }

        private static IReadOnlyDictionary<string, int> ParseWeightVector(string vector)
        {
            var result = new Dictionary<string, int>();
            foreach (var part in vector.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var pieces = part.Split(':');
                if (pieces.Length == 2 && int.TryParse(pieces[1], out var value))
                {
                    result[pieces[0]] = Math.Clamp(value, 0, 100);
                }
            }
            return result;
        }

        private static string BuildWeightVector(IReadOnlyDictionary<string, int> weights) =>
            string.Join(",", weights.Select(pair => $"{pair.Key}:{pair.Value}"));
    }
}
