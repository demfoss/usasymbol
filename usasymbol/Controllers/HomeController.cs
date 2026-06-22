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
        private static readonly Dictionary<string, string> CategoryIcons = new(StringComparer.OrdinalIgnoreCase)
        {
            ["birds"] = "fa-solid fa-dove",
            ["flowers"] = "fa-solid fa-spa",
            ["trees"] = "fa-solid fa-tree",
            ["flags"] = "fa-solid fa-flag-usa",
            ["beverages"] = "fa-solid fa-glass-water",
            ["mammals"] = "fa-solid fa-paw",
            ["cats"] = "fa-solid fa-cat",
            ["dogs"] = "fa-solid fa-dog",
            ["horses"] = "fa-solid fa-horse",
            ["dinosaurs"] = "fa-solid fa-dragon",
            ["mottos"] = "fa-solid fa-scroll",
            ["nicknames"] = "fa-solid fa-tag",
            ["colors"] = "fa-solid fa-palette",
            ["marine-mammals"] = "fa-solid fa-fish",
            ["firearms"] = "fa-solid fa-bullseye",
            ["license-plate-slogans"] = "fa-solid fa-car-side",
            ["state-seals"] = "fa-solid fa-stamp",
            ["coats-of-arms"] = "fa-solid fa-shield-halved",
            ["sports"] = "fa-solid fa-medal"
        };

        public HomeController(IStateService stateService, AppDbContext dbContext, IWebHostEnvironment env)
        {
            _stateService = stateService;
            _dbContext = dbContext;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var allSymbols = await _dbContext.Symbols
                .Include(s => s.State)
                .Where(s => s.State != null)
                .ToListAsync();

            var allWithImages = allSymbols
                .Where(s => !string.IsNullOrWhiteSpace(s.ImageUrl))
                .ToList();

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
                SymbolCategories = await GetSymbolCategoriesAsync(allSymbols),
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

        [HttpGet("/about/artsiom-dusau")]
        public IActionResult ArtsiomDusau()
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

        [HttpGet("/privacy-policy")]
        [HttpGet("/privacy-policy/")]
        [HttpGet("/privacy-policy.html")]
        [HttpGet("/privacy-policy.php")]
        [HttpGet("/privacy-policy.aspx")]
        [HttpGet("/privacy.html")]
        [HttpGet("/privacy.php")]
        [HttpGet("/privacy.aspx")]
        public IActionResult PrivacyRedirect()
        {
            return RedirectPermanent("/privacy");
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

        private async Task<List<SymbolCategoryViewModel>> GetSymbolCategoriesAsync(IReadOnlyList<Symbol> symbols)
        {
            var dbCats = await _dbContext.SymbolCategories.ToListAsync();
            var rng = new Random();
            var listingAssets = GetSymbolListingAssets();
            var licensePlatePreviewImages = GetLicensePlatePreviewImages();
            var sealImages = GetSealImages();

            var order = new[] { "birds", "flowers", "trees", "flags", "mottos", "state-seals", "coats-of-arms", "sports", "license-plate-slogans", "mammals", "beverages", "nicknames", "colors", "dogs", "horses", "marine-mammals", "firearms", "dinosaurs", "cats" };

            return order
                .Select(slug => dbCats.FirstOrDefault(c => c.Type == slug))
                .Where(c => c != null)
                .Select(c =>
                {
                    var matchedSymbols = GetCategorySymbols(c!.Type, symbols).ToList();
                    var stateCount = matchedSymbols
                        .Select(s => s.StateId)
                        .Distinct()
                        .Count();

                    var previewImages = matchedSymbols
                        .Select(s => s.ImageUrl)
                        .Where(path => !string.IsNullOrWhiteSpace(path) && !path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        .Cast<string>()
                        .ToList();

                    if (c.Type == "cats")
                    {
                        previewImages = new List<string> { "/images/mammals/maine/maine-coon-cat.jpg" };
                    }
                    else if (c.Type == "license-plate-slogans" && licensePlatePreviewImages.Count > 0)
                    {
                        previewImages = licensePlatePreviewImages;
                    }
                    else if (c.Type == "state-seals" && sealImages.Count > 0)
                    {
                        previewImages = sealImages;
                    }

                    var randomImg = previewImages.Count > 0
                        ? previewImages[rng.Next(previewImages.Count)]
                        : listingAssets.TryGetValue(c.Type, out var listingPreview) && !string.IsNullOrWhiteSpace(listingPreview.ImageUrl)
                            ? listingPreview.ImageUrl
                            : c.ImageUrl;

                    return new SymbolCategoryViewModel
                    {
                        Type = c.Type,
                        Name = c.Name,
                        Description = c.Description,
                        StateCount = stateCount,
                        Url = $"/symbols/{c.Type}",
                        ImageUrl = randomImg,
                        Icon = GetCategoryIcon(c.Type)
                    };
                })
                .ToList();
        }

        private static IEnumerable<Symbol> GetCategorySymbols(string categoryType, IEnumerable<Symbol> symbols)
        {
            return symbols.Where(symbol => IsCategoryMatch(categoryType, symbol));
        }

        private static bool IsCategoryMatch(string categoryType, Symbol symbol)
        {
            var designation = symbol.Designation ?? string.Empty;
            var name = symbol.Name ?? string.Empty;

            return categoryType switch
            {
                "birds" => symbol.Type == "bird",
                "flowers" => symbol.Type == "flower",
                "trees" => symbol.Type == "tree",
                "flags" => symbol.Type == "flag",
                "beverages" => symbol.Type == "beverage",
                "mammals" => symbol.Type == "mammal" && !IsDog(designation) && !IsHorse(designation, name) && !IsMarineMammal(designation) && !IsCat(designation, name),
                "cats" => symbol.Type == "mammal" && IsCat(designation, name),
                "dogs" => symbol.Type == "mammal" && IsDog(designation),
                "horses" => symbol.Type == "mammal" && IsHorse(designation, name),
                "marine-mammals" => symbol.Type == "mammal" && IsMarineMammal(designation),
                "dinosaurs" => symbol.Type == "dinosaur",
                "mottos" => symbol.Type == "motto",
                "nicknames" => symbol.Type == "nickname",
                "colors" => symbol.Type == "color",
                "firearms" => symbol.Type == "firearm",
                "license-plate-slogans" => symbol.Type == "license-plate",
                "state-seals" => symbol.Type == "state-seal",
                "coats-of-arms" => symbol.Type == "coat-of-arms",
                "sports" => symbol.Type == "sport",
                _ => false
            };
        }

        private static bool IsDog(string designation) =>
            designation.Contains("dog", StringComparison.OrdinalIgnoreCase);

        private static bool IsMarineMammal(string designation) =>
            designation.Contains("marine mammal", StringComparison.OrdinalIgnoreCase);

        private static bool IsCat(string designation, string name) =>
            designation.Contains("state cat", StringComparison.OrdinalIgnoreCase) ||
            designation.Contains("official state cat", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("maine coon cat", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("calico cat", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("tabby cat", StringComparison.OrdinalIgnoreCase);

        private static bool IsHorse(string designation, string name) =>
            designation.Contains("horse", StringComparison.OrdinalIgnoreCase) ||
            designation.Contains("pony", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("horse", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("pony", StringComparison.OrdinalIgnoreCase);

        private Dictionary<string, ListingAsset> GetSymbolListingAssets()
        {
            var images = new Dictionary<string, ListingAsset>(StringComparer.OrdinalIgnoreCase);
            var symbolsDir = Path.Combine(_env.ContentRootPath, "Content", "symbols");
            if (!Directory.Exists(symbolsDir))
            {
                return images;
            }

            foreach (var file in Directory.EnumerateFiles(symbolsDir, "*.yml", SearchOption.TopDirectoryOnly))
            {
                var yaml = System.IO.File.ReadAllText(file);
                var rowImages = Regex.Matches(yaml, @"(?m)^\s*-\s*symbol_image:\s*(.+?)\s*$")
                    .Select(m => m.Groups[1].Value.Trim().Trim('"', '\''))
                    .Where(path => !string.IsNullOrWhiteSpace(path) && path.StartsWith('/'))
                    .ToList();

                var usableRowImages = rowImages
                    .Where(path => System.IO.File.Exists(Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))))
                    .ToList();

                var previewImage = usableRowImages.FirstOrDefault(path => !path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    ?? usableRowImages.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(previewImage))
                {
                    var heroMatch = Regex.Match(yaml, @"(?m)^hero_image:\s*(.+?)\s*$");
                    if (heroMatch.Success)
                    {
                        var heroImage = heroMatch.Groups[1].Value.Trim().Trim('"', '\'');
                        var heroPath = Path.Combine(_env.WebRootPath, heroImage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (!string.IsNullOrWhiteSpace(heroImage) && System.IO.File.Exists(heroPath))
                        {
                            previewImage = heroImage;
                        }
                    }
                }

                var stateCount = Regex.Matches(yaml, @"(?m)^\s*state_slug:\s*(.+?)\s*$")
                    .Select(m => m.Groups[1].Value.Trim().Trim('"', '\''))
                    .Where(slug => !string.IsNullOrWhiteSpace(slug))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                images[Path.GetFileNameWithoutExtension(file)] = new ListingAsset(previewImage, stateCount);
            }

            return images;
        }

        private static string GetCategoryIcon(string type)
        {
            return CategoryIcons.TryGetValue(type, out var icon)
                ? icon
                : "fa-solid fa-star";
        }

        private sealed record ListingAsset(string? ImageUrl, int StateCount);

        private List<string> GetSealImages()
        {
            var sealsDir = Path.Combine(_env.WebRootPath, "images", "seals");
            if (!Directory.Exists(sealsDir))
                return new List<string>();

            return Directory
                .EnumerateFiles(sealsDir, "seal.webp", SearchOption.AllDirectories)
                .Select(f => "/images/seals/" + Path.GetFileName(Path.GetDirectoryName(f)) + "/seal.webp")
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();
        }

        private List<string> GetLicensePlateHeroImages()
        {
            var images = new List<string>();
            var statesDir = Path.Combine(_env.ContentRootPath, "Content", "states");
            if (!Directory.Exists(statesDir))
            {
                return images;
            }

            foreach (var file in Directory.EnumerateFiles(statesDir, "license-plate.yaml", SearchOption.AllDirectories))
            {
                var yaml = System.IO.File.ReadAllText(file);
                var match = Regex.Match(yaml, @"(?m)^hero_image:\s*(.+?)\s*$");
                if (!match.Success)
                {
                    continue;
                }

                var imageUrl = match.Groups[1].Value.Trim().Trim('"', '\'');
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var localPath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(localPath))
                {
                    images.Add(imageUrl);
                }
            }

            return images;
        }

        private List<string> GetLicensePlatePreviewImages()
        {
            var images = new List<string>();
            var statesDir = Path.Combine(_env.ContentRootPath, "Content", "states");
            if (!Directory.Exists(statesDir))
            {
                return images;
            }

            foreach (var file in Directory.EnumerateFiles(statesDir, "license-plate.yaml", SearchOption.AllDirectories))
            {
                var yaml = System.IO.File.ReadAllText(file);
                var versionMatch = Regex.Match(yaml, @"(?m)^\s*image:\s*(\/images\/license-plates\/.+?)\s*$");
                var heroMatch = Regex.Match(yaml, @"(?m)^hero_image:\s*(\/images\/license-plates\/.+?)\s*$");
                var imageUrl = versionMatch.Success
                    ? versionMatch.Groups[1].Value.Trim().Trim('"', '\'')
                    : heroMatch.Success
                        ? heroMatch.Groups[1].Value.Trim().Trim('"', '\'')
                        : string.Empty;

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var localPath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(localPath))
                {
                    images.Add(imageUrl);
                }
            }

            return images;
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
                "g.state path { fill: #cfd9e8; cursor: pointer; transition: fill .18s ease, opacity .18s ease; }",
                "g.state path:hover, g.state path:focus { fill: #5f7faa; opacity: 1; outline: none; }",
                ".borders { stroke: #ffffff; stroke-width: 1.1; }",
                ".separator1 { stroke: #94a3b8; stroke-width: 1.6; }"
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
