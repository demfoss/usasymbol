using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Controllers
{
    public class ListingsController : Controller
    {
        private readonly IListingsContentService _service;

        public ListingsController(IListingsContentService service)
        {
            _service = service;
        }

        [Route("symbols/{slug}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> SymbolListing(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            slug = slug.ToLowerInvariant();

            var canonicalSlug = slug switch
            {
                "drinks" => "beverages",
                "state-drinks" => "beverages",
                "state-beverages" => "beverages",
                _ => slug
            };

            if (!string.Equals(canonicalSlug, slug, StringComparison.Ordinal))
                return RedirectPermanent($"/symbols/{canonicalSlug}");

            var content = await _service.GetContentAsync("symbols", canonicalSlug);
            if (content == null)
                return NotFound();

            var viewModel = new PageDetailViewModel { Content = content };

            ViewData["Title"] = content.Seo?.Title;
            ViewData["Description"] = content.Seo?.Description;
            ViewData["Canonical"] = content.Url;

            return View("~/Views/Listings/Detail.cshtml", viewModel);
        }
    }
}
