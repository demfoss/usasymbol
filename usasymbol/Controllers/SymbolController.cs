using Microsoft.AspNetCore.Mvc;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using USASymbol.Services.Content;

namespace USASymbol.Controllers
{
    public class SymbolController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolService _symbolService;
        private readonly IBirdService _birdService;
        //private readonly IFlowerService _flowerService;
        // TODO: Add other content services as they are implemented
        // private readonly ITreeService _treeService;
        // private readonly IMottoService _mottoService;
        private readonly ILogger<SymbolController> _logger;

        public SymbolController(
            IStateService stateService,
            ISymbolService symbolService,
            IBirdService birdService,
            //IFlowerService flowerService,
            ILogger<SymbolController> logger)
        {
            _stateService = stateService;
            _symbolService = symbolService;
            _birdService = birdService;
            //_flowerService = flowerService;
            _logger = logger;
        }

        // GET: /symbols
        [Route("symbols")]
        public async Task<IActionResult> Categories()
        {
            ViewData["Title"] = "All State Symbol Categories";
            ViewData["Description"] = "Browse all types of official U.S. state symbols - birds, flowers, trees, flags, and more.";

            var symbolTypes = await _symbolService.GetAllSymbolTypesAsync();
            return View(symbolTypes);
        }

        // GET: /symbols/birds - UNCHANGED
        [Route("symbols/{type}")]
        public async Task<IActionResult> Listing(string type)
        {
            var symbols = await _symbolService.GetSymbolsByTypeAsync(type);

            if (!symbols.Any())
            {
                return NotFound();
            }

            var model = new SymbolListingViewModel
            {
                SymbolType = type,
                SymbolTypeName = GetSymbolTypeName(type),
                Symbols = symbols
            };

            ViewData["Title"] = $"All State {model.SymbolTypeName}";
            ViewData["Description"] = $"Discover the official {type} of all 50 U.S. states.";

            return View(model);
        }

        // GET: /states/{stateSlug}/bird - Specialized for Birds
        [Route("states/{stateSlug}/bird")]
        public async Task<IActionResult> Bird(string stateSlug)
        {
            // Get state
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Get bird symbol for this state
            var birdSymbol = await _symbolService.GetSymbolAsync(state.Id, "bird");
            if (birdSymbol == null)
            {
                _logger.LogWarning("Bird symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Get specialized bird content from markdown
            var birdContent = await _birdService.GetBirdContentAsync(stateSlug);

            // Get other symbols from this state for "Related Symbols" section
            var allSymbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            var relatedSymbols = allSymbols.Where(s => s.Type != "bird").Take(6).ToList();

            var model = new BirdDetailViewModel
            {
                State = state,
                Symbol = birdSymbol,
                BirdContent = birdContent,
                RelatedSymbols = relatedSymbols
            };

            // SEO
            ViewData["Title"] = $"{birdSymbol.Name} - {state.Name} State Bird";
            ViewData["Description"] = $"Learn about the {birdSymbol.Name}, the official state bird of {state.Name}. " +
                                     $"Discover its history, characteristics, habitat, and significance to {state.Name}.";
            ViewData["OgImage"] = birdSymbol.ImageUrl ?? "/images/default-bird.jpg";

            return View(model);
        }

        // GET: /states/{stateSlug}/flower - Specialized for Flowers
        [Route("states/{stateSlug}/flower")]
        public async Task<IActionResult> Flower(string stateSlug)
        {
            // Get state
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Get flower symbol for this state
            var flowerSymbol = await _symbolService.GetSymbolAsync(state.Id, "flower");
            if (flowerSymbol == null)
            {
                _logger.LogWarning("Flower symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Get specialized flower content from markdown
            //var flowerContent = await _flowerService.GetFlowerContentAsync(stateSlug);

            // Get other symbols from this state
            var allSymbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            var relatedSymbols = allSymbols.Where(s => s.Type != "flower").Take(6).ToList();

            var model = new FlowerDetailViewModel
            {
                State = state,
                Symbol = flowerSymbol,
                //FlowerContent = flowerContent,
                RelatedSymbols = relatedSymbols
            };

            // SEO
            ViewData["Title"] = $"{flowerSymbol.Name} - {state.Name} State Flower";
            ViewData["Description"] = $"Learn about the {flowerSymbol.Name}, the official state flower of {state.Name}. " +
                                     $"Discover its blooming season, growing conditions, and cultural significance.";
            ViewData["OgImage"] = flowerSymbol.ImageUrl ?? "/images/default-flower.jpg";

            return View(model);
        }

        // GET: /states/{stateSlug}/tree - Placeholder for Trees
        [Route("states/{stateSlug}/tree")]
        public async Task<IActionResult> Tree(string stateSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                return NotFound();
            }

            var treeSymbol = await _symbolService.GetSymbolAsync(state.Id, "tree");
            if (treeSymbol == null)
            {
                return NotFound();
            }

            // TODO: Implement TreeService and TreeContent
            // var treeContent = await _treeService.GetTreeContentAsync(stateSlug);

            var allSymbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            var relatedSymbols = allSymbols.Where(s => s.Type != "tree").Take(6).ToList();

            // Use base SymbolDetailViewModel until TreeDetailViewModel is created
            var model = new SymbolDetailViewModel
            {
                State = state,
                Symbol = treeSymbol,
                Content = null, // Will be replaced with TreeContent
                RelatedSymbols = relatedSymbols
            };

            ViewData["Title"] = $"{treeSymbol.Name} - {state.Name} State Tree";
            ViewData["Description"] = $"Learn about the {treeSymbol.Name}, the official state tree of {state.Name}.";
            ViewData["OgImage"] = treeSymbol.ImageUrl ?? "/images/default-tree.jpg";

            return View("Tree", model);
        }

        // GET: /states/{stateSlug}/{symbolType} - Generic fallback for other symbol types
        [Route("states/{stateSlug}/{symbolType}")]
        public async Task<IActionResult> Detail(string stateSlug, string symbolType)
        {
            // Redirect to specialized actions if they exist
            switch (symbolType)
            {
                case "bird":
                    return RedirectToAction(nameof(Bird), new { stateSlug });
                case "flower":
                    return RedirectToAction(nameof(Flower), new { stateSlug });
                case "tree":
                    return RedirectToAction(nameof(Tree), new { stateSlug });
                default:
                    // Handle other symbol types generically
                    break;
            }

            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                return NotFound();
            }

            var symbol = await _symbolService.GetSymbolAsync(state.Id, symbolType);
            if (symbol == null)
            {
                return NotFound();
            }

            // For other symbol types, use the generic MarkdownService (if still needed)
            // This is a fallback for symbol types not yet migrated to specialized services

            var relatedSymbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            relatedSymbols = relatedSymbols.Where(s => s.Id != symbol.Id).Take(6).ToList();

            var model = new SymbolDetailViewModel
            {
                State = state,
                Symbol = symbol,
                Content = null,
                RelatedSymbols = relatedSymbols
            };

            ViewData["Title"] = $"{symbol.Name} - {state.Name} State {GetSymbolTypeName(symbolType)}";
            ViewData["Description"] = $"Learn about {symbol.Name}, the official state {symbolType} of {state.Name}.";
            ViewData["OgImage"] = symbol.ImageUrl ?? state.FlagImageUrl;

            // Use a generic detail view for unspecialized symbol types
            return View("Detail", model);
        }

        private string GetSymbolTypeName(string type)
        {
            return type switch
            {
                "bird" => "Bird",
                "birds" => "Birds",
                "flower" => "Flower",
                "flowers" => "Flowers",
                "tree" => "Tree",
                "trees" => "Trees",
                "motto" => "Motto",
                "mottos" => "Mottos",
                "animal" => "Animal",
                "animals" => "Animals",
                "flag" => "Flag",
                "flags" => "Flags",
                _ => char.ToUpper(type[0]) + type.Substring(1)
            };
        }
    }
}