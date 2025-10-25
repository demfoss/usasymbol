using Microsoft.AspNetCore.Mvc;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using System.Diagnostics;
using usasymbol.Models;

namespace USASymbol.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolService _symbolService;

        public HomeController(IStateService stateService, ISymbolService symbolService)
        {
            _stateService = stateService;
            _symbolService = symbolService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Home";
            ViewData["Description"] = "Explore official state symbols across all 50 U.S. states. Discover state birds, flowers, trees, flags, and more.";

            var model = new HomeViewModel
            {
                FeaturedStates = await _stateService.GetFeaturedStatesAsync(5),
                SymbolCategories = GetSymbolCategories()
            };

            return View(model);
        }

        public IActionResult About()
        {
            ViewData["Title"] = "About";
            return View();
        }

        public IActionResult Quiz()
        {
            ViewData["Title"] = "Quiz";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private List<SymbolCategory> GetSymbolCategories()
        {
            return new List<SymbolCategory>
            {
                new SymbolCategory { Name = "State Birds", Icon = "🦅", Description = "Discover the official birds of all 50 states.", Url = "/symbols/birds" },
                new SymbolCategory { Name = "State Flowers", Icon = "🌸", Description = "See the blossoms that define each state.", Url = "/symbols/flowers" },
                new SymbolCategory { Name = "State Trees", Icon = "🌳", Description = "Learn about America's state trees.", Url = "/symbols/trees" },
                new SymbolCategory { Name = "State Flags", Icon = "🏴", Description = "Explore all 50 flags side by side.", Url = "/symbols/flags" },
                new SymbolCategory { Name = "State Animals", Icon = "🦌", Description = "Meet the animals that represent each state.", Url = "/symbols/animals" },
                new SymbolCategory { Name = "State Mottos", Icon = "📜", Description = "Discover the words that define each state's spirit.", Url = "/symbols/mottos" }
            };
        }
    }
}