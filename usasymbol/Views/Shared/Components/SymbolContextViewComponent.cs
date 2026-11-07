using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Views.Shared.Components
{
    /// <summary>
    /// part = "badges" | "sidebar" | "prevnext". All computed from the Symbols table.
    /// </summary>
    public class SymbolContextViewComponent : ViewComponent
    {
        private readonly SymbolContextService _service;

        public SymbolContextViewComponent(SymbolContextService service) => _service = service;

        public async Task<IViewComponentResult> InvokeAsync(ISymbolDetailViewModel model, string part)
        {
            SymbolPageContext? ctx = null;
            try { ctx = await _service.BuildAsync(model); }
            catch { /* context blocks are optional; never break the page */ }

            if (ctx == null) return Content(string.Empty);

            return part switch
            {
                "badges" => View("Badges", ctx),
                "sidebar" => View("Sidebar", ctx),
                "prevnext" => View("PrevNext", ctx),
                _ => Content(string.Empty)
            };
        }
    }
}
