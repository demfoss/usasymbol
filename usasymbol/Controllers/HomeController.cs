using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.RegularExpressions;
using USASymbol.Data;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using usasymbol.Models;
using usasymbol.Services.Interface;

namespace USASymbol.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStateService _stateService;
        private readonly AppDbContext _dbContext;
        private readonly IWebHostEnvironment _env;

        public HomeController(IStateService stateService, AppDbContext dbContext, IWebHostEnvironment env)
        {
            _stateService = stateService;
            _dbContext = dbContext;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var allWithImages = await _dbContext.Symbols
                .Include(s => s.State)
                .Where(s => s.ImageUrl != null && s.ImageUrl != string.Empty && s.State != null)
                .ToListAsync();

            var allStates = await _dbContext.States
                .OrderBy(s => s.Name)
                .ToListAsync();

            var rng = new Random();
            var pool = allWithImages
                .OrderBy(_ => rng.Next())
                .Take(15)
                .Select(s => new SymbolWithState { Symbol = s, State = s.State! })
                .ToList();

            var model = new HomeViewModel
            {
                FeaturedStates = await _stateService.GetFeaturedStatesAsync(5),
                SymbolCategories = await GetSymbolCategoriesAsync(),
                SymbolOfTheDayPool = pool,
                StateMapItems = allStates.Select(state => new HomeStateMapItem
                {
                    Name = state.Name,
                    Slug = state.Slug,
                    Abbreviation = state.Abbreviation,
                    Capital = state.Capital,
                    Population = state.Population
                }).ToList(),
                HomeMapSvg = GetHomeMapSvg()
            };

            return View(model);
        }

        [HttpGet("/editorial-policy")]
        public IActionResult EditorialPolicy()
        {
            return View();
        }

        [HttpGet("/accessibility")]
        public IActionResult Accessibility()
        {
            return View();
        }

        [HttpGet("/terms")]
        public IActionResult Terms()
        {
            return View();
        }

        [HttpGet("/about")]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet("/contact")]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpGet("/privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Quiz()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

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

        private async Task<List<SymbolCategoryViewModel>> GetSymbolCategoriesAsync()
        {
            var dbCats = await _dbContext.SymbolCategories.ToListAsync();

            var order = new[] { "birds", "flowers", "trees", "flags", "beverages", "mammals", "dinosaurs", "mottos", "nicknames", "colors", "license-plate-slogans" };

            return order
                .Select(slug => dbCats.FirstOrDefault(c => c.Type == slug))
                .Where(c => c != null)
                .Select(c => new SymbolCategoryViewModel
                {
                    Type = c!.Type.TrimEnd('s'),
                    Name = c.Name,
                    Description = c.Description,
                    Url = $"/symbols/{c.Type}",
                    ImageUrl = c.ImageUrl,
                    Icon = c.Type switch
                    {
                        "birds" => "🦅",
                        "flowers" => "🌸",
                        "trees" => "🌳",
                        "flags" => "🏴",
                        "beverages" => "🥤",
                        "mammals" => "🦌",
                        "dinosaurs" => "🦖",
                        "mottos" => "📜",
                        "nicknames" => "🏷️",
                        "colors" => "🎨",
                        "license-plate-slogans" => "🚗",
                        _ => "⭐"
                    }
                })
                .ToList();
        }

        private string GetHomeMapSvg()
        {
            var path = Path.Combine(_env.WebRootPath, "maps", "us-states.svg");
            if (!System.IO.File.Exists(path))
            {
                return string.Empty;
            }

            var svg = System.IO.File.ReadAllText(path);
            svg = Regex.Replace(svg, @"<title>.*?</title>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            svg = Regex.Replace(svg, @"<svg([^>]*)>", match =>
            {
                var attrs = match.Groups[1].Value;
                attrs = Regex.Replace(attrs, @"\s+width=""[^""]*""", "");
                attrs = Regex.Replace(attrs, @"\s+height=""[^""]*""", "");
                if (!attrs.Contains("viewBox"))
                {
                    attrs += " viewBox=\"0 0 959 593\"";
                }

                attrs += " width=\"100%\" preserveAspectRatio=\"xMidYMid meet\" aria-hidden=\"true\"";
                return $"<svg{attrs}>";
            });

            var css = string.Join(Environment.NewLine, new[]
            {
                "g.state path { fill: #d7b69b; cursor: pointer; transition: fill .18s ease, opacity .18s ease, transform .18s ease; }",
                "g.state path:hover, g.state path:focus { fill: #c38e63; opacity: 1; outline: none; }",
                ".borders { stroke: #ffffff; stroke-width: 1.1; }",
                ".separator1 { stroke: #9ca3af; stroke-width: 1.6; }"
            });

            var marker = "Place this code in the empty space below. */";
            var idx = svg.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var closeStyle = svg.IndexOf("</style>", idx, StringComparison.Ordinal);
                if (closeStyle >= 0)
                {
                    svg = svg[..(idx + marker.Length)] + Environment.NewLine + css + Environment.NewLine + svg[closeStyle..];
                }
            }

            return svg;
        }
    }
}
