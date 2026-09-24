using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using USASymbol.Data;
using USASymbol.Models.ViewModels;
using usasymbol.Models;

namespace USASymbol.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _dbContext;

        public HomeController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        public async Task<IActionResult> Index()
        {
            var states = await _dbContext.States
                .AsNoTracking()
                .Where(state => state.Abbreviation != "DC")
                .OrderBy(state => state.Name)
                .Select(state => new HomeStateMapItem
                {
                    Name = state.Name,
                    Slug = state.Slug,
                    Abbreviation = state.Abbreviation,
                    Capital = state.Capital,
                    Population = state.Population
                })
                .ToListAsync();

            return View(new HomeViewModel { StateMapItems = states });
        }

        [HttpGet("/editorial-policy")]
        public IActionResult EditorialPolicy() => View();

        [HttpGet("/accessibility")]
        public IActionResult Accessibility() => View();

        [HttpGet("/terms")]
        public IActionResult Terms() => View();

        [HttpGet("/about")]
        public IActionResult About() => View();

        [HttpGet("/about/artsiom-dusau")]
        public IActionResult ArtsiomDusau() => View();

        [HttpGet("/contact")]
        public IActionResult Contact() => View();

        [HttpGet("/privacy")]
        public IActionResult Privacy() => View();

        [HttpGet("/privacy-policy")]
        [HttpGet("/privacy-policy/")]
        [HttpGet("/privacy-policy.html")]
        [HttpGet("/privacy-policy.php")]
        [HttpGet("/privacy-policy.aspx")]
        [HttpGet("/privacy.html")]
        [HttpGet("/privacy.php")]
        [HttpGet("/privacy.aspx")]
        public IActionResult PrivacyRedirect() => RedirectPermanent("/privacy");

        public IActionResult Quiz() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        [Route("Error/{statusCode}")]
        public IActionResult Error(int statusCode)
        {
            if (statusCode == 404)
            {
                ViewData["Title"] = "Page Not Found - USA Symbol";
                return View("NotFound");
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
