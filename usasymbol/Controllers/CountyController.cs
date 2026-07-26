using Microsoft.AspNetCore.Mvc;
using USASymbol.Services.Interface;

namespace USASymbol.Controllers
{
    public sealed class CountyController : Controller
    {
        private readonly ICountyService _countyService;

        public CountyController(ICountyService countyService)
        {
            _countyService = countyService;
        }

        [HttpGet("/county-match")]
        public async Task<IActionResult> Match()
        {
            var model = await _countyService.GetMatcherAsync();
            ViewData["Title"] = "County Match: Find the Best U.S. County for Your Priorities";
            ViewData["Description"] = "Weight housing affordability, income, jobs, education, and health to find counties that fit your priorities. Missing county metrics are automatically excluded and weights are rebalanced.";
            ViewData["Canonical"] = "/county-match";
            ViewData["BodyClass"] = "county-match-page";
            return View(model);
        }

        [HttpGet("/county-rankings")]
        public async Task<IActionResult> Rankings(string? state)
        {
            var model = await _countyService.GetRankingsAsync(state);
            ViewData["Title"] = string.IsNullOrWhiteSpace(model.SelectedStateSlug)
                ? "Best U.S. Counties: Affordability, Income, Jobs, Education & Health"
                : "Best Counties by Affordability, Income, Jobs, Education & Health";
            ViewData["Description"] = "Explore transparent county rankings for housing affordability, household income, unemployment, college attainment, and health using ACS, BLS LAUS, and County Health Rankings data.";
            ViewData["Canonical"] = string.IsNullOrWhiteSpace(model.SelectedStateSlug)
                ? "/county-rankings"
                : $"/county-rankings?state={model.SelectedStateSlug}";
            ViewData["BodyClass"] = "county-rankings-page";
            return View(model);
        }

        [HttpGet("/states/{stateSlug}/counties")]
        public async Task<IActionResult> Index(string stateSlug)
        {
            var model = await _countyService.GetIndexAsync(stateSlug);
            if (model is null)
                return NotFound();

            ViewData["Title"] = $"{model.State.Name} Counties: Income, Housing, Jobs & Health";
            ViewData["Description"] = $"Compare all {model.Counties.Count} counties in {model.State.Name} by population, household income, home value, rent, unemployment, education, and life expectancy.";
            ViewData["Canonical"] = $"/states/{model.State.Slug}/counties";
            ViewData["BodyClass"] = "county-directory-page";
            return View(model);
        }

        [HttpGet("/states/{stateSlug}/counties/{countySlug}")]
        public async Task<IActionResult> Detail(string stateSlug, string countySlug)
        {
            var model = await _countyService.GetProfileAsync(stateSlug, countySlug);
            if (model is null)
                return NotFound();

            ViewData["Title"] = $"{model.County.Name}, {model.State.Name}: Cost, Jobs, Education & Health";
            ViewData["Description"] = $"County profile for {model.County.Name}, {model.State.Name}: income, housing, unemployment, education, health, population, statewide comparisons, methodology, and official sources.";
            ViewData["Canonical"] = $"/states/{model.State.Slug}/counties/{model.County.Slug}";
            ViewData["OgImage"] = model.State.FlagImageUrl;
            ViewData["BodyClass"] = "county-profile-page";
            return View(model);
        }
    }
}
