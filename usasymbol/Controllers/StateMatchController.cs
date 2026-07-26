using Microsoft.AspNetCore.Mvc;
using USASymbol.Services;

namespace USASymbol.Controllers
{
    public sealed class StateMatchController : Controller
    {
        private readonly IStateMatchService _stateMatchService;

        public StateMatchController(IStateMatchService stateMatchService)
        {
            _stateMatchService = stateMatchService;
        }

        [HttpGet("/state-match")]
        public async Task<IActionResult> Index()
        {
            var model = await _stateMatchService.BuildAsync();

            ViewData["Title"] = "Find Your Best State to Live In | State Match";
            ViewData["Description"] = "Adjust cost, jobs, housing, safety, climate, healthcare, education, and tax priorities to find the U.S. states that fit you best.";
            ViewData["Canonical"] = "/state-match";
            ViewData["BodyClass"] = "state-match-surface";

            return View(model);
        }
    }
}
