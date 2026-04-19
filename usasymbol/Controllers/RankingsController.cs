using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Controllers
{
    public class RankingsController : Controller
    {
        private readonly IRankingsContentService _service;
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly ILogger<RankingsController> _logger;

        public RankingsController(
            IRankingsContentService service,
            ILatestContentRailService latestContentRailService,
            ILogger<RankingsController> logger)
        {
            _service = service;
            _latestContentRailService = latestContentRailService;
            _logger  = logger;
        }

        [Route("/rankings")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _service.GetAllCategoriesAsync();
                return View(new PageHubViewModel { Categories = categories });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading rankings hub");
                return View("Error");
            }
        }

        [Route("/rankings/{category}")]
        public async Task<IActionResult> Category(string category)
        {
            try
            {
                var categories = await _service.GetAllCategoriesAsync();
                var cat = categories.Find(c =>
                    string.Equals(c.Id, category, System.StringComparison.OrdinalIgnoreCase));

                if (cat == null) return NotFound();

                ViewData["Title"]       = $"{cat.Title} Rankings";
                ViewData["Description"] = $"Compare U.S. states by {cat.Title.ToLower()}";

                return View("Index", new PageHubViewModel { Categories = new List<PageCategory> { cat } });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading category: {Category}", category);
                return View("Error");
            }
        }

        [Route("/rankings/{category}/{slug}")]
        public async Task<IActionResult> Detail(string category, string slug)
        {
            try
            {
                var content = await _service.GetContentAsync(category, slug);
                if (content == null)
                {
                    _logger.LogWarning("Ranking not found: {Category}/{Slug}", category, slug);
                    return NotFound();
                }

                ViewData["LatestContentRail"] = await _latestContentRailService.GetLatestItemsAsync(8);
                return View("Ranking", new PageDetailViewModel { Content = content });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading ranking: {Category}/{Slug}", category, slug);
                return View("Error");
            }
        }
    }
}
