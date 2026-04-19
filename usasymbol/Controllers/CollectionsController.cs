using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace USASymbol.Controllers
{
    public class CollectionsController : Controller
    {
        private readonly ICollectionsContentService _service;
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly ILogger<CollectionsController> _logger;

        public CollectionsController(
            ICollectionsContentService service,
            ILatestContentRailService latestContentRailService,
            ILogger<CollectionsController> logger)
        {
            _service = service;
            _latestContentRailService = latestContentRailService;
            _logger  = logger;
        }

        [Route("/collections")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _service.GetAllCategoriesAsync();
                return View(new PageHubViewModel { Categories = categories });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading collections hub");
                return View("Error");
            }
        }

        [Route("/collections/{group}")]
        public async Task<IActionResult> Group(string group)
        {
            try
            {
                var categories = await _service.GetAllCategoriesAsync();
                var cat = categories.Find(c =>
                    string.Equals(c.Id, group, System.StringComparison.OrdinalIgnoreCase));

                if (cat == null) return NotFound();

                ViewData["Title"]       = $"{cat.Title} Collections";
                ViewData["Description"] = $"Browse U.S. state collections related to {cat.Title.ToLower()}";

                return View("Index", new PageHubViewModel { Categories = new List<PageCategory> { cat }, IsGroupPage = true });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading collection group: {Group}", group);
                return View("Error");
            }
        }

        [Route("/collections/{group}/{slug}")]
        public async Task<IActionResult> Detail(string group, string slug)
        {
            try
            {
                var content = await _service.GetContentAsync(group, slug);
                if (content == null)
                {
                    _logger.LogWarning("Collection not found: {Group}/{Slug}", group, slug);
                    return NotFound();
                }

                var vm = new PageDetailViewModel { Content = content };

                ViewData["Title"]       = content.Seo?.Title;
                ViewData["Description"] = content.Seo?.Description;
                ViewData["Canonical"]   = content.Url;
                ViewData["LatestContentRail"] = await _latestContentRailService.GetLatestItemsAsync(8);

                return View("Detail", vm);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading collection: {Group}/{Slug}", group, slug);
                return View("Error");
            }
        }
    }
}
