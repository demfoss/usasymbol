using Microsoft.AspNetCore.Mvc;
using usasymbol.Services.Interface;
using USASymbol.Services;
using USASymbol.Models.ViewModels;

namespace USASymbol.Controllers
{
    public class StateController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolService _symbolService;
        private readonly IStateHubContentService _stateHubContentService;

        public StateController(
            IStateService stateService,
            ISymbolService symbolService,
            IStateHubContentService stateHubContentService)
        {
            _stateService = stateService;
            _symbolService = symbolService;
            _stateHubContentService = stateHubContentService;
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
    }
}
