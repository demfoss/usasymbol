using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Controllers
{
    public sealed class StateMatchController : Controller
    {
        private readonly IStateMatchService _stateMatchService;
        private readonly IStateMatchOgImageService _ogImageService;
        private readonly IRankingsContentService _rankingsContent;

        public StateMatchController(
            IStateMatchService stateMatchService,
            IStateMatchOgImageService ogImageService,
            IRankingsContentService rankingsContent)
        {
            _stateMatchService = stateMatchService;
            _ogImageService = ogImageService;
            _rankingsContent = rankingsContent;
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
        /// Take-Home Pay by State: salary (or a profession's real pay by state) run through 2025
        /// federal + state brackets, then housing and sales/property tax, for every state at once.
        /// Rent and combined sales tax come from the site's own ranking pages so the numbers match them.
        /// </summary>
        [HttpGet("/tools/take-home-pay-by-state")]
        public async Task<IActionResult> TakeHomePay()
        {
            var tool = await _stateMatchService.BuildAsync();
            var rentByAbbr = await ReadRankingColumnAsync("economy", "average-rent-by-state", "rent");
            var salesTaxByAbbr = await ReadRankingColumnAsync("taxes", "sales-tax-by-state", "combined_rate");

            var model = new TakeHomePageViewModel
            {
                Places = tool.Places
                    .Select(place => new TakeHomePlaceViewModel
                    {
                        Name = place.Name,
                        Slug = place.Slug,
                        Abbreviation = place.Abbreviation,
                        FlagImageUrl = place.FlagImageUrl,
                        PropertyTaxRate = place.Metrics.FirstOrDefault(metric => metric.Key == "propertytax")?.Raw,
                        SalesTaxCombinedRate = salesTaxByAbbr.TryGetValue(place.Abbreviation, out var sales)
                            ? sales
                            : place.Metrics.FirstOrDefault(metric => metric.Key == "salestax")?.Raw,
                        AverageRent = rentByAbbr.TryGetValue(place.Abbreviation, out var rent) ? rent : null
                    })
                    .ToList(),
                IncomeTaxScheduleJson = _stateMatchService.GetIncomeTaxScheduleJson(),
                OccupationSalariesJson = _stateMatchService.GetOccupationSalariesJson()
            };

            ViewData["Title"] = "Take-Home Pay by State Calculator (2025 Taxes) | USA Symbol";
            ViewData["Description"] = "Enter a salary or pick a job and see take-home pay after federal and state taxes in every state, plus what's left after rent or a mortgage.";
            ViewData["Canonical"] = "/tools/take-home-pay-by-state";
            ViewData["BodyClass"] = "state-match-surface";

            return View("TakeHomePay", model);
        }

        /// <summary>
        /// Two-person State Match: each partner sets their own priorities, and states are ranked
        /// by how well they suit both. "?a=" and "?b=" use the same weight-vector format as "?w=".
        /// </summary>
        [HttpGet("/tools/where-should-we-move")]
        public async Task<IActionResult> CoupleMatch()
        {
            var model = await _stateMatchService.BuildAsync();

            ViewData["Title"] = "Where Should We Move? State Match for Couples | USA Symbol";
            ViewData["Description"] = "You and your partner each set what matters most. See which states suit you both, where your top 10s overlap, and where you disagree.";
            ViewData["Canonical"] = "/tools/where-should-we-move";
            ViewData["BodyClass"] = "state-match-surface";

            return View("CoupleMatch", model);
        }

        /// <summary>
        /// Everything State Match's "Money check" needs, fetched only when a state panel opens:
        /// the tax schedules plus average rent and combined sales tax per state (same sources as
        /// the Take-Home Pay tool, so both show the same figure).
        /// </summary>
        [HttpGet("/api/state-match/money-data")]
        public async Task<IActionResult> MoneyData()
        {
            var rent = await ReadRankingColumnAsync("economy", "average-rent-by-state", "rent");
            var sales = await ReadRankingColumnAsync("taxes", "sales-tax-by-state", "combined_rate");
            var costs = rent.Keys.Union(sales.Keys, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    abbr => abbr.ToUpperInvariant(),
                    abbr => new
                    {
                        rent = rent.TryGetValue(abbr, out var r) ? r : (double?)null,
                        sales = sales.TryGetValue(abbr, out var s) ? s : (double?)null
                    });

            var json = "{\"taxes\":" + _stateMatchService.GetIncomeTaxScheduleJson() +
                ",\"costs\":" + System.Text.Json.JsonSerializer.Serialize(costs) + "}";

            Response.Headers.CacheControl = "public, max-age=3600";
            return Content(json, "application/json");
        }

        private async Task<Dictionary<string, double>> ReadRankingColumnAsync(string category, string slug, string column)
        {
            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var content = await _rankingsContent.GetContentAsync(category, slug);
            if (content?.Table == null)
            {
                return result;
            }

            foreach (var row in content.Table.Rows)
            {
                var abbreviation = row.GetString("postal_code");
                if (string.IsNullOrWhiteSpace(abbreviation))
                {
                    continue;
                }
                if (row.Data.TryGetValue(column, out var raw) && raw != null &&
                    double.TryParse(Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
                {
                    result[abbreviation.Trim()] = value;
                }
            }

            return result;
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
