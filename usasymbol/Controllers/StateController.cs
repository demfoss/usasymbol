using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Controllers
{
    public class StateController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolService _symbolService;

        public StateController(IStateService stateService, ISymbolService symbolService)
        {
            _stateService = stateService;
            _symbolService = symbolService;
        }

        // GET: /states
        [Route("states")]
        public async Task<IActionResult> Listing(string? region = null)
        {
            ViewData["Title"] = "All 50 U.S. States";
            ViewData["Description"] = "Browse all 50 U.S. states and explore their official symbols, capitals, and unique characteristics.";

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

        // GET: /states/{slug}
        [Route("states/{slug}")]
        public async Task<IActionResult> Index(string slug)
        {
            var state = await _stateService.GetStateBySlugAsync(slug);

            if (state == null)
                return NotFound();

            ViewData["Title"] = $"{state.Name} State Symbols";
            ViewData["Description"] = $"Explore the official symbols of {state.Name}, including state bird, flower, tree, flag, and more.";

            var symbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            var relatedStates = await _stateService.GetStatesByRegionAsync(state.Region ?? "");

            var model = new StateViewModel
            {
                State = state,
                Symbols = symbols,
                RelatedStates = relatedStates.Where(s => s.Id != state.Id).Take(4).ToList()
            };

            return View(model);
        }
    }
}