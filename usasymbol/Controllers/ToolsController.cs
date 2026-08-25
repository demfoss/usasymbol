using Microsoft.AspNetCore.Mvc;
using USASymbol.Services.Content;
using USASymbol.Services.Interface;
using usasymbol.Services.Interface;

namespace USASymbol.Controllers
{
    public sealed class ToolsController : Controller
    {
        private readonly IZipCodeService _zipCodeService;
        private readonly IStateService _stateService;
        private readonly ICountyService _countyService;
        private readonly IAreaCodeService _areaCodeService;
        private readonly IParkService _parkService;

        public ToolsController(
            IZipCodeService zipCodeService,
            IStateService stateService,
            ICountyService countyService,
            IAreaCodeService areaCodeService,
            IParkService parkService)
        {
            _zipCodeService = zipCodeService;
            _stateService = stateService;
            _countyService = countyService;
            _areaCodeService = areaCodeService;
            _parkService = parkService;
        }

        [HttpGet("/tools")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Free Tools | USA Symbol";
            ViewData["Description"] = "Free interactive tools for exploring U.S. states, cities, ZIP codes, counties, and time zones.";
            ViewData["Canonical"] = "/tools";

            return View();
        }

        // ---- Random ZIP Code Generator ----

        [HttpGet("/tools/random-zip-code-generator")]
        public IActionResult RandomZipCodeGenerator()
        {
            ViewData["Title"] = "Random ZIP Code Generator | USA Symbol";
            ViewData["Description"] = "Generate random real U.S. ZIP codes with city and state, filter by state, copy results, or download them as a .txt file.";
            ViewData["Canonical"] = "/tools/random-zip-code-generator";

            ViewBag.InitialZips = _zipCodeService.GetRandom(20, null);
            ViewBag.States = _zipCodeService.GetStateSummaries();

            return View();
        }

        [HttpGet("/tools/random-zip-code-generator/generate")]
        public IActionResult GenerateZips(int count = 20, string? state = null)
        {
            return Json(_zipCodeService.GetRandom(count, state));
        }

        // ---- Random City/State Generator ----

        [HttpGet("/tools/random-city-generator")]
        public IActionResult RandomCityGenerator()
        {
            ViewData["Title"] = "Random City & State Generator | USA Symbol";
            ViewData["Description"] = "Generate random real U.S. cities with their state, filter by state, copy results, or download them as a .txt file.";
            ViewData["Canonical"] = "/tools/random-city-generator";

            ViewBag.InitialCities = _zipCodeService.GetRandomCities(20, null);
            ViewBag.States = _zipCodeService.GetStateSummaries();

            return View();
        }

        [HttpGet("/tools/random-city-generator/generate")]
        public IActionResult GenerateCities(int count = 20, string? state = null)
        {
            return Json(_zipCodeService.GetRandomCities(count, state));
        }

        // ---- ZIP Code Lookup ----

        [HttpGet("/tools/zip-code-lookup")]
        public IActionResult ZipCodeLookup()
        {
            ViewData["Title"] = "ZIP Code Lookup | USA Symbol";
            ViewData["Description"] = "Look up any U.S. ZIP code to find its city, county, state, and flag.";
            ViewData["Canonical"] = "/tools/zip-code-lookup";

            return View();
        }

        [HttpGet("/tools/zip-code-lookup/lookup")]
        public IActionResult LookupZip(string zip)
        {
            var result = _zipCodeService.GetByZip(zip ?? "");
            if (result == null) return Json(new { found = false });
            return Json(new { found = true, result });
        }

        // ---- County Finder ----

        [HttpGet("/tools/county-finder")]
        public IActionResult CountyFinder()
        {
            ViewData["Title"] = "County Finder by ZIP Code or City | USA Symbol";
            ViewData["Description"] = "Find which U.S. county a ZIP code or city belongs to.";
            ViewData["Canonical"] = "/tools/county-finder";

            return View();
        }

        [HttpGet("/tools/county-finder/lookup")]
        public async Task<IActionResult> LookupCounty(string? zip, string? city, string? state)
        {
            var matches = new List<USASymbol.Models.ZipCodeResult>();

            if (!string.IsNullOrWhiteSpace(zip))
            {
                var byZip = _zipCodeService.GetByZip(zip);
                if (byZip != null) matches.Add(byZip);
            }
            else if (!string.IsNullOrWhiteSpace(city))
            {
                matches = _zipCodeService.FindByCity(city, state);
            }

            if (matches.Count == 0) return Json(new { found = false });

            var results = new List<object>();
            foreach (var m in matches)
            {
                string? countyUrl = null;
                if (!string.IsNullOrWhiteSpace(m.County))
                {
                    var index = await _countyService.GetIndexAsync(m.StateSlug);
                    var normalizedTarget = NormalizeCountyName(m.County);
                    var match = index?.Counties.FirstOrDefault(c => NormalizeCountyName(c.Name) == normalizedTarget);
                    if (match != null && match.Published)
                    {
                        countyUrl = $"/states/{m.StateSlug}/counties/{match.Slug}";
                    }
                }

                results.Add(new
                {
                    zip = m.Zip,
                    city = m.City,
                    county = m.County,
                    stateCode = m.StateCode,
                    stateName = m.StateName,
                    stateSlug = m.StateSlug,
                    flagUrl = m.FlagUrl,
                    countyUrl
                });
            }

            return Json(new { found = true, results });
        }

        private static string NormalizeCountyName(string name) =>
            (name ?? "")
                .Trim()
                .Replace(" County", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" Parish", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" Borough", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" Census Area", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" Municipality", "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToLowerInvariant();

        // ---- State Abbreviation Finder ----

        [HttpGet("/tools/state-abbreviation-finder")]
        public IActionResult StateAbbreviationFinder()
        {
            ViewData["Title"] = "State Abbreviation Finder | USA Symbol";
            ViewData["Description"] = "Look up any U.S. state's two-letter postal abbreviation, or find the state behind an abbreviation.";
            ViewData["Canonical"] = "/tools/state-abbreviation-finder";

            ViewBag.States = UsStatesReference.All();

            return View();
        }

        // ---- State Capital Flashcards ----

        [HttpGet("/tools/state-capital-flashcards")]
        public async Task<IActionResult> StateCapitalFlashcards()
        {
            ViewData["Title"] = "State Capital Flashcards | USA Symbol";
            ViewData["Description"] = "Practice all 50 U.S. state capitals with a free flip-card trainer.";
            ViewData["Canonical"] = "/tools/state-capital-flashcards";

            // The site only covers the 50 states (no DC) — same convention used across rankings.
            var states = await _stateService.GetAllStatesAsync();
            ViewBag.States = states
                .Where(s => !string.IsNullOrWhiteSpace(s.Capital) && !string.Equals(s.Name, "District of Columbia", StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();

            return View();
        }

        // ---- Time Zone Finder ----

        [HttpGet("/tools/timezone-finder")]
        public IActionResult TimeZoneFinder()
        {
            ViewData["Title"] = "Time Zone Finder by State or ZIP Code | USA Symbol";
            ViewData["Description"] = "Find which U.S. time zone a state or ZIP code falls in, with UTC offsets.";
            ViewData["Canonical"] = "/tools/timezone-finder";

            ViewBag.States = UsStatesReference.All();

            return View();
        }

        [HttpGet("/tools/timezone-finder/lookup")]
        public IActionResult LookupTimeZone(string? state, string? zip)
        {
            var stateCode = state;

            if (string.IsNullOrWhiteSpace(stateCode) && !string.IsNullOrWhiteSpace(zip))
            {
                var byZip = _zipCodeService.GetByZip(zip);
                if (byZip == null) return Json(new { found = false });
                stateCode = byZip.StateCode;
            }

            var result = TimeZoneReference.Lookup(stateCode ?? "");
            if (result == null) return Json(new { found = false });

            return Json(new { found = true, result });
        }

        // ---- Distance Calculator ----

        [HttpGet("/tools/distance-calculator")]
        public IActionResult DistanceCalculator()
        {
            ViewData["Title"] = "ZIP Code Distance Calculator | USA Symbol";
            ViewData["Description"] = "Calculate the straight-line distance between two U.S. ZIP codes in miles and kilometers.";
            ViewData["Canonical"] = "/tools/distance-calculator";

            return View();
        }

        [HttpGet("/tools/distance-calculator/calculate")]
        public IActionResult CalculateDistance(string fromZip, string toZip)
        {
            var result = _zipCodeService.GetDistance(fromZip ?? "", toZip ?? "");
            if (result == null) return Json(new { found = false });
            return Json(new { found = true, result });
        }

        // ---- Area Code Lookup ----

        [HttpGet("/tools/area-code-lookup")]
        public IActionResult AreaCodeLookup()
        {
            ViewData["Title"] = "Area Code Lookup | USA Symbol";
            ViewData["Description"] = "Look up any U.S. telephone area code to find its state and primary cities.";
            ViewData["Canonical"] = "/tools/area-code-lookup";

            return View();
        }

        [HttpGet("/tools/area-code-lookup/lookup")]
        public IActionResult LookupAreaCode(string code)
        {
            var result = _areaCodeService.GetByAreaCode(code ?? "");
            if (result == null) return Json(new { found = false });
            return Json(new { found = true, result });
        }

        // ---- Random Fake Address Generator ----

        [HttpGet("/tools/random-address-generator")]
        public IActionResult RandomAddressGenerator()
        {
            ViewData["Title"] = "Random Address Generator | USA Symbol";
            ViewData["Description"] = "Generate a random, non-deliverable U.S. street address for testing, QA, or form-filling — not a real address.";
            ViewData["Canonical"] = "/tools/random-address-generator";

            ViewBag.States = _zipCodeService.GetStateSummaries();
            ViewBag.InitialAddress = _zipCodeService.GenerateRandomAddress(null);

            return View();
        }

        [HttpGet("/tools/random-address-generator/generate")]
        public IActionResult GenerateAddress(string? state = null)
        {
            return Json(_zipCodeService.GenerateRandomAddress(state));
        }

        // ---- Population Lookup ----

        [HttpGet("/tools/population-lookup")]
        public IActionResult PopulationLookup()
        {
            ViewData["Title"] = "Population Lookup by ZIP Code or City | USA Symbol";
            ViewData["Description"] = "Look up the population of any U.S. ZIP code or city.";
            ViewData["Canonical"] = "/tools/population-lookup";

            ViewBag.States = UsStatesReference.All();

            return View();
        }

        [HttpGet("/tools/population-lookup/lookup")]
        public IActionResult LookupPopulation(string? zip, string? city, string? state)
        {
            if (!string.IsNullOrWhiteSpace(zip))
            {
                var byZip = _zipCodeService.GetByZip(zip);
                if (byZip == null) return Json(new { found = false });

                return Json(new
                {
                    found = true,
                    mode = "zip",
                    population = byZip.Population,
                    place = byZip
                });
            }

            if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(state))
            {
                var cityResult = _zipCodeService.GetCityPopulation(city, state);
                if (cityResult == null) return Json(new { found = false });

                return Json(new
                {
                    found = true,
                    mode = "city",
                    population = cityResult.Value.Population,
                    zipCount = cityResult.Value.Zips.Count,
                    place = cityResult.Value.Zips.FirstOrDefault()
                });
            }

            return Json(new { found = false });
        }

        // ---- Nearest National Park Finder ----

        [HttpGet("/tools/nearest-national-parks")]
        public IActionResult NearestNationalParks()
        {
            ViewData["Title"] = "Nearest National Park Finder | USA Symbol";
            ViewData["Description"] = "Find the closest U.S. national parks to any ZIP code, ranked by straight-line distance.";
            ViewData["Canonical"] = "/tools/nearest-national-parks";

            return View();
        }

        [HttpGet("/tools/nearest-national-parks/find")]
        public async Task<IActionResult> FindNearestParks(string zip, int count = 10)
        {
            var origin = _zipCodeService.GetByZip(zip ?? "");
            if (origin == null || !origin.Lat.HasValue || !origin.Lng.HasValue)
                return Json(new { found = false });

            var allParks = await _parkService.GetAllNationalParksAsync();

            var nearest = allParks
                .Where(p => p.Location.Latitude != 0 || p.Location.Longitude != 0)
                .Select(p => new USASymbol.Models.NearestParkResult
                {
                    Name = p.Name,
                    Slug = p.Slug,
                    Url = $"/national-parks/{p.Slug}",
                    StateCode = p.Location.StateCode,
                    NearestCity = p.Location.NearestCity,
                    GoogleMapsUrl = p.Map.GoogleSearchUrl,
                    DistanceMiles = Math.Round(GeoMath.HaversineMiles(origin.Lat.Value, origin.Lng.Value, p.Location.Latitude, p.Location.Longitude), 1)
                })
                .OrderBy(p => p.DistanceMiles)
                .Take(Math.Clamp(count, 1, 25))
                .ToList();

            return Json(new { found = true, origin, results = nearest });
        }
    }
}
