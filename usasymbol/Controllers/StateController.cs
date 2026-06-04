using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using usasymbol.Services.Interface;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Controllers
{
    public class StateController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolService _symbolService;
        private readonly IStateHubContentService _stateHubContentService;
        private readonly IWebHostEnvironment _environment;

        public StateController(
            IStateService stateService,
            ISymbolService symbolService,
            IStateHubContentService stateHubContentService,
            IWebHostEnvironment environment)
        {
            _stateService = stateService;
            _symbolService = symbolService;
            _stateHubContentService = stateHubContentService;
            _environment = environment;
        }

        private static readonly Dictionary<string, int> _fips = new()
        {
            ["AL"]=1,["AK"]=2,["AZ"]=4,["AR"]=5,["CA"]=6,["CO"]=8,["CT"]=9,["DE"]=10,
            ["FL"]=12,["GA"]=13,["HI"]=15,["ID"]=16,["IL"]=17,["IN"]=18,["IA"]=19,["KS"]=20,
            ["KY"]=21,["LA"]=22,["ME"]=23,["MD"]=24,["MA"]=25,["MI"]=26,["MN"]=27,["MS"]=28,
            ["MO"]=29,["MT"]=30,["NE"]=31,["NV"]=32,["NH"]=33,["NJ"]=34,["NM"]=35,["NY"]=36,
            ["NC"]=37,["ND"]=38,["OH"]=39,["OK"]=40,["OR"]=41,["PA"]=42,["RI"]=44,["SC"]=45,
            ["SD"]=46,["TN"]=47,["TX"]=48,["UT"]=49,["VT"]=50,["VA"]=51,["WA"]=53,["WV"]=54,
            ["WI"]=55,["WY"]=56,
        };

        private static readonly Dictionary<int, string> _slugsByFips = new()
        {
            [1]="alabama",[2]="alaska",[4]="arizona",[5]="arkansas",[6]="california",
            [8]="colorado",[9]="connecticut",[10]="delaware",[12]="florida",[13]="georgia",
            [15]="hawaii",[16]="idaho",[17]="illinois",[18]="indiana",[19]="iowa",
            [20]="kansas",[21]="kentucky",[22]="louisiana",[23]="maine",[24]="maryland",
            [25]="massachusetts",[26]="michigan",[27]="minnesota",[28]="mississippi",[29]="missouri",
            [30]="montana",[31]="nebraska",[32]="nevada",[33]="new-hampshire",[34]="new-jersey",
            [35]="new-mexico",[36]="new-york",[37]="north-carolina",[38]="north-dakota",[39]="ohio",
            [40]="oklahoma",[41]="oregon",[42]="pennsylvania",[44]="rhode-island",[45]="south-carolina",
            [46]="south-dakota",[47]="tennessee",[48]="texas",[49]="utah",[50]="vermont",
            [51]="virginia",[53]="washington",[54]="west-virginia",[55]="wisconsin",[56]="wyoming",
        };

        [Route("states/{slug}/map")]
        public async Task<IActionResult> Map(string slug)
        {
            var state = await _stateService.GetStateBySlugAsync(slug);
            if (state == null) return NotFound();

            var allStates = await _stateService.GetAllStatesAsync();

            var stateDict = allStates
                .Where(s => !string.IsNullOrWhiteSpace(s.Abbreviation) && _fips.TryGetValue(s.Abbreviation.ToUpperInvariant(), out _))
                .ToDictionary(
                    s => _fips[s.Abbreviation.ToUpperInvariant()].ToString(),
                    s => new
                    {
                        name = s.Name,
                        slug = s.Slug,
                        capital = s.Capital,
                        population = s.Population,
                        region = s.Region,
                        statehood = s.StateHoodDate.HasValue ? s.StateHoodDate.Value.Year : (int?)null,
                    });

            var counties = BuildStateCountyMapItems(state);

            var model = new StateMapPageViewModel
            {
                State = state,
                AllStatesJson = JsonSerializer.Serialize(stateDict),
                StateSlugsJson = JsonSerializer.Serialize(_slugsByFips),
                Counties = counties,
                CountySummary = BuildStateCountyMapSummary(counties),
            };

            ViewData["Title"] = $"Map of {state.Name} | Counties & Location";
            ViewData["Description"] = $"Interactive map of {state.Name} showing its location in the United States. Click to zoom in and explore {state.Name} counties. Capital: {state.Capital}.";

            return View(model);
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

        private List<StateCountyMapItem> BuildStateCountyMapItems(State state)
        {
            if (string.IsNullOrWhiteSpace(state.Abbreviation) ||
                !_fips.TryGetValue(state.Abbreviation.ToUpperInvariant(), out var stateFips))
            {
                return new List<StateCountyMapItem>();
            }

            var countyDataPath = Path.Combine(_environment.WebRootPath, "maps", "county-data.json");
            if (!System.IO.File.Exists(countyDataPath))
                return new List<StateCountyMapItem>();

            var json = System.IO.File.ReadAllText(countyDataPath);
            var countyData = JsonSerializer.Deserialize<Dictionary<string, CountyDataPoint>>(json)
                ?? new Dictionary<string, CountyDataPoint>();

            return countyData
                .Where(e => e.Key.Length == 5 &&
                            int.TryParse(e.Key[..2], out var sf) && sf == stateFips)
                .OrderByDescending(e => e.Value.Population ?? 0)
                .ThenBy(e => e.Value.Name)
                .Select(e => new StateCountyMapItem
                {
                    DisplayName = GetCountyDisplayName(e.Value.Name),
                    StateCode = state.Abbreviation.ToUpperInvariant(),
                    FipsCode = e.Key,
                    Population = e.Value.Population
                })
                .ToList();
        }

        private static StateCountyMapSummary? BuildStateCountyMapSummary(List<StateCountyMapItem> counties)
        {
            if (counties.Count == 0) return null;
            var byPop = counties.OrderByDescending(c => c.Population ?? 0).ToList();
            return new StateCountyMapSummary
            {
                CountyCount = counties.Count,
                LargestByPopulation = byPop.FirstOrDefault(),
                SmallestByPopulation = byPop.LastOrDefault()
            };
        }

        private static string GetCountyDisplayName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "County";
            return name.Contains("County") || name.Contains("Parish") ||
                   name.Contains("Borough") || name.Contains("Census Area") ||
                   name.Contains("Municipality") || name.Contains("District") ||
                   name.Contains("Planning Region")
                ? name : $"{name} County";
        }

        private sealed class CountyDataPoint
        {
            [JsonPropertyName("n")] public string? Name { get; set; }
            [JsonPropertyName("p")] public long? Population { get; set; }
        }
    }
}
