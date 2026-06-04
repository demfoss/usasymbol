using Microsoft.AspNetCore.Mvc;
using usasymbol.Services.Interface;
using USASymbol.Services;
using USASymbol.Models.ViewModels;
using System.Text.Json;

namespace USASymbol.Controllers
{
    public class StateController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolService _symbolService;
        private readonly IStateHubContentService _stateHubContentService;
        private readonly IWebHostEnvironment _env;

        public StateController(
            IStateService stateService,
            ISymbolService symbolService,
            IStateHubContentService stateHubContentService,
            IWebHostEnvironment env)
        {
            _stateService = stateService;
            _symbolService = symbolService;
            _stateHubContentService = stateHubContentService;
            _env = env;
        }

        [Route("states")]
        public async Task<IActionResult> Listing(string? region = null)
        {
            var states = string.IsNullOrEmpty(region)
                ? await _stateService.GetAllStatesAsync()
                : await _stateService.GetStatesByRegionAsync(region);

            var model = new StatesListingViewModel
            {
                States = states,
                SelectedRegion = region,
                Regions = new List<string> { "Northeast", "Midwest", "South", "West" }
            };

            return View(model);
        }

        public async Task<IActionResult> Index(string slug)
        {
            var state = await _stateService.GetStateBySlugAsync(slug);

            if (state == null)
            {
                return NotFound();
            }

            var symbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            var relatedStates = await _stateService.GetStatesByRegionAsync(state.Region ?? "");
            var hubContent = await _stateHubContentService.GetHubAsync(state.Slug);

            var model = new StateViewModel
            {
                State = state,
                Symbols = symbols,
                RelatedStates = relatedStates.Where(s => s.Id != state.Id).Take(3).ToList(),
                HubContent = hubContent
            };

            ViewData["OgImage"] = state.FlagImageUrl;

            return View(model);
        }

        [Route("states/{slug}/map")]
        public async Task<IActionResult> Map(string slug)
        {
            var state = await _stateService.GetStateBySlugAsync(slug);
            if (state == null) return NotFound();

            // Load county data from wwwroot/maps/county-data.json
            // Format: { "FIPS": { "n": "Name", "p": population }, ... }
            var counties = new List<CountyItem>();
            var countyDataPath = Path.Combine(_env.WebRootPath, "maps", "county-data.json");
            if (System.IO.File.Exists(countyDataPath))
            {
                var json = await System.IO.File.ReadAllTextAsync(countyDataPath);
                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                var stateCode = state.Abbreviation;
                // FIPS: first 2 digits = state FIPS code
                // Build state FIPS from abbreviation via lookup
                var stateFipsPrefix = GetStateFipsPrefix(state.Abbreviation);
                if (raw != null && stateFipsPrefix != null)
                {
                    foreach (var (fips, val) in raw)
                    {
                        if (!fips.StartsWith(stateFipsPrefix)) continue;
                        var name = val.TryGetProperty("n", out var n) ? n.GetString() ?? "" : "";
                        var pop = val.TryGetProperty("p", out var p) ? (int?)p.GetInt32() : null;
                        counties.Add(new CountyItem
                        {
                            DisplayName = name,
                            StateCode = stateCode,
                            FipsCode = fips,
                            Population = pop
                        });
                    }
                    counties = counties.OrderBy(c => c.DisplayName).ToList();
                }
            }

            CountySummary? summary = null;
            if (counties.Count > 0)
            {
                summary = new CountySummary
                {
                    CountyCount = counties.Count,
                    LargestByPopulation = counties.Where(c => c.Population.HasValue)
                        .OrderByDescending(c => c.Population).FirstOrDefault(),
                    SmallestByPopulation = counties.Where(c => c.Population.HasValue)
                        .OrderBy(c => c.Population).FirstOrDefault()
                };
            }

            // AllStatesJson for the choropleth map
            var allStates = await _stateService.GetAllStatesAsync();
            var allStatesJson = JsonSerializer.Serialize(allStates.Select(s => new
            {
                slug = s.Slug,
                abbr = s.Abbreviation,
                name = s.Name
            }));
            var stateSlugsJson = JsonSerializer.Serialize(allStates.Select(s => s.Slug));

            var model = new StateMapPageViewModel
            {
                State = state,
                Counties = counties,
                CountySummary = summary,
                AllStatesJson = allStatesJson,
                StateSlugsJson = stateSlugsJson
            };

            return View(model);
        }

        private static string? GetStateFipsPrefix(string abbreviation) => abbreviation.ToUpper() switch
        {
            "AL" => "01", "AK" => "02", "AZ" => "04", "AR" => "05", "CA" => "06",
            "CO" => "08", "CT" => "09", "DE" => "10", "FL" => "12", "GA" => "13",
            "HI" => "15", "ID" => "16", "IL" => "17", "IN" => "18", "IA" => "19",
            "KS" => "20", "KY" => "21", "LA" => "22", "ME" => "23", "MD" => "24",
            "MA" => "25", "MI" => "26", "MN" => "27", "MS" => "28", "MO" => "29",
            "MT" => "30", "NE" => "31", "NV" => "32", "NH" => "33", "NJ" => "34",
            "NM" => "35", "NY" => "36", "NC" => "37", "ND" => "38", "OH" => "39",
            "OK" => "40", "OR" => "41", "PA" => "42", "RI" => "44", "SC" => "45",
            "SD" => "46", "TN" => "47", "TX" => "48", "UT" => "49", "VT" => "50",
            "VA" => "51", "WA" => "53", "WV" => "54", "WI" => "55", "WY" => "56",
            _ => null
        };
    }
}
