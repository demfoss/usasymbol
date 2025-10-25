using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Controllers
{
    public class SymbolController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolService _symbolService;
        private readonly IMarkdownService _markdownService;

        public SymbolController(
            IStateService stateService,
            ISymbolService symbolService,
            IMarkdownService markdownService)
        {
            _stateService = stateService;
            _symbolService = symbolService;
            _markdownService = markdownService;
        }

        // GET: /symbols
        public async Task<IActionResult> Categories()
        {
            ViewData["Title"] = "All State Symbol Categories";
            ViewData["Description"] = "Browse all types of official U.S. state symbols - birds, flowers, trees, flags, and more.";

            var symbolTypes = await _symbolService.GetAllSymbolTypesAsync();
            return View(symbolTypes);
        }

        // GET: /symbols/birds
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

        // GET: /states/illinois/bird
        // GET: /states/illinois/bird
        public async Task<IActionResult> Detail(string stateSlug, string symbolType)
        {
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

            // Get markdown content
            var content = await _markdownService.GetSymbolContentAsync(stateSlug, symbolType);

            // Get other symbols from the same state
            var relatedSymbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            relatedSymbols = relatedSymbols.Where(s => s.Id != symbol.Id).Take(3).ToList();

            var model = new SymbolDetailViewModel
            {
                State = state,
                Symbol = symbol,
                Content = content ?? new USASymbol.Models.SymbolContent(),
                RelatedSymbols = relatedSymbols
            };

            ViewData["Title"] = $"{symbol.Name} - {state.Name} State {GetSymbolTypeName(symbolType)}";
            ViewData["Description"] = $"Learn about {symbol.Name}, the official state {symbolType} of {state.Name}.";
            ViewData["OgImage"] = symbol.ImageUrl ?? state.FlagImageUrl;

            // Выбираем View в зависимости от типа символа
            var viewName = symbolType switch
            {
                "bird" => "Detail_Bird",
                "flower" => "Detail_Flower",
                "tree" => "Detail_Tree",
                "motto" => "Detail_Motto",
                "animal" => "Detail_Animal",
                "flag" => "Detail_Flag",
                _ => "Detail" // Fallback на общий шаблон
            };

            return View(viewName, model);
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