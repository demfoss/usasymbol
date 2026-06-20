using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using USASymbol.Services.Interface;
using Usasymbol.ViewModels;

namespace Usasymbol.Views.Shared.Components
{
    public class MapBlockViewComponent : ViewComponent
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMapPngService _mapPngService;
        private static string? _cachedBaseSvg;
        private static readonly object _lock = new();

        private const string SvgInjectionMarker = "Place this code in the empty space below. */";

        // Approximate centroids in 959×593 SVG coordinate space
        private static readonly Dictionary<string, (double X, double Y)> StateCentroids = new()
        {
            ["AL"] = (730, 444), ["AK"] = (138, 502), ["AZ"] = (197, 398), ["AR"] = (635, 407),
            ["CA"] = (107, 338), ["CO"] = (312, 313), ["CT"] = (898, 212), ["DE"] = (872, 270),
            ["FL"] = (778, 505), ["GA"] = (760, 452), ["HI"] = (252, 530), ["ID"] = (182, 202),
            ["IL"] = (668, 315), ["IN"] = (715, 300), ["IA"] = (592, 263), ["KS"] = (482, 345),
            ["KY"] = (732, 360), ["LA"] = (635, 475), ["ME"] = (910, 135), ["MD"] = (855, 287),
            ["MA"] = (900, 200), ["MI"] = (718, 220), ["MN"] = (560, 183), ["MS"] = (675, 450),
            ["MO"] = (620, 347), ["MT"] = (260, 158), ["NE"] = (474, 288), ["NV"] = (165, 295),
            ["NH"] = (895, 178), ["NJ"] = (878, 255), ["NM"] = (267, 413), ["NY"] = (840, 208),
            ["NC"] = (826, 386), ["ND"] = (462, 162), ["OH"] = (765, 292), ["OK"] = (506, 402),
            ["OR"] = (110, 210), ["PA"] = (832, 255), ["RI"] = (916, 218), ["SC"] = (802, 430),
            ["SD"] = (465, 230), ["TN"] = (723, 393), ["TX"] = (492, 475), ["UT"] = (218, 315),
            ["VT"] = (882, 168), ["VA"] = (827, 328), ["WA"] = (120, 135), ["WV"] = (792, 315),
            ["WI"] = (647, 218), ["WY"] = (303, 245),
        };

        // Small NE states rendered as callouts to avoid overlap
        // Key = postal code; Value = (label X/Y, anchor X/Y on the state shape)
        private static readonly Dictionary<string, ((double X, double Y) Label, (double X, double Y) Anchor)> Callouts = new()
        {
            ["VT"] = ((948, 130), (882, 168)),
            ["NH"] = ((948, 148), (895, 178)),
            ["MA"] = ((948, 166), (900, 198)),
            ["CT"] = ((948, 184), (898, 211)),
            ["RI"] = ((948, 202), (916, 218)),
            ["NJ"] = ((948, 248), (878, 255)),
            ["DE"] = ((948, 266), (872, 270)),
            ["MD"] = ((948, 284), (855, 287)),
        };

        public MapBlockViewComponent(IWebHostEnvironment env, IMapPngService mapPngService)
        {
            _env = env;
            _mapPngService = mapPngService;
        }

        public async Task<IViewComponentResult> InvokeAsync(MapBlockViewModel model)
        {
            if (model == null) return Content(string.Empty);

            if (model.HasChoropleth)
            {
                var baseSvg = GetBaseSvg();
                if (!string.IsNullOrEmpty(baseSvg))
                {
                    model.SvgContent = InjectColors(baseSvg, model);
                }

                // Generate and cache a PNG of this map for Google Images and OG fallback
                if (!string.IsNullOrWhiteSpace(model.Slug) && model.Entries.Count > 0)
                {
                    model.GeneratedImagePath = await _mapPngService.EnsureMapPngAsync(model.Slug, model.Entries);
                }
            }

            if (!model.HasChoropleth && string.IsNullOrWhiteSpace(model.Image))
                return Content(string.Empty);

            return View("~/Views/Shared/Components/MapBlock.cshtml", model);
        }

        private string GetBaseSvg()
        {
            if (_cachedBaseSvg != null) return _cachedBaseSvg;
            lock (_lock)
            {
                if (_cachedBaseSvg != null) return _cachedBaseSvg;
                var path = Path.Combine(_env.WebRootPath, "maps", "us-states.svg");
                if (!File.Exists(path)) return string.Empty;
                var svg = File.ReadAllText(path);
                svg = MakeResponsive(svg);
                _cachedBaseSvg = svg;
            }
            return _cachedBaseSvg!;
        }

        private static string MakeResponsive(string svg)
        {
            svg = Regex.Replace(svg, @"<title>.*?</title>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            svg = Regex.Replace(svg,
                @"<svg([^>]*)>",
                m =>
                {
                    var attrs = m.Groups[1].Value;
                    attrs = Regex.Replace(attrs, @"\s+width=""[^""]*""", "");
                    attrs = Regex.Replace(attrs, @"\s+height=""[^""]*""", "");
                    if (!attrs.Contains("viewBox"))
                        attrs += " viewBox=\"0 0 959 593\"";
                    attrs += " width=\"100%\" preserveAspectRatio=\"xMidYMid meet\" aria-hidden=\"true\"";
                    return $"<svg{attrs}>";
                });
            return svg;
        }

        private static string InjectColors(string baseSvg, MapBlockViewModel model)
        {
            var css = new StringBuilder();
            css.AppendLine("g.state path { cursor: pointer; transition: opacity .12s ease; }");
            css.AppendLine("g.state path:hover, g.state path:focus { opacity: .80; outline: none; }");
            css.AppendLine(".state { fill: #e2e8f0; }");

            foreach (var e in model.Entries)
                css.AppendLine($".{e.PostalCode.ToLower()} {{ fill: {e.FillColor}; }}");

            var idx = baseSvg.IndexOf(SvgInjectionMarker, StringComparison.Ordinal);
            if (idx < 0) return baseSvg;

            var closeStyle = baseSvg.IndexOf("</style>", idx, StringComparison.Ordinal);
            if (closeStyle < 0) return baseSvg;

            return baseSvg[..(idx + SvgInjectionMarker.Length)]
                 + "\n"
                 + css
                 + baseSvg[closeStyle..];
        }

        private static string InjectLabels(string svg, MapBlockViewModel model)
        {
            var fillLookup = model.Entries
                .ToDictionary(e => e.PostalCode.ToUpper(), e => e.FillColor, StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.Append("<g id=\"state-labels\" aria-hidden=\"true\" style=\"pointer-events:none\">");

            foreach (var (postal, c) in StateCentroids)
            {
                var fill     = fillLookup.TryGetValue(postal, out var fc) ? fc : "#e2e8f0";
                var textFill = GetTextColor(fill);
                var stroke   = textFill == "#fff" ? "#000" : "#fff";

                if (Callouts.TryGetValue(postal, out var callout))
                {
                    // Leader line: thin line from state centroid to callout label
                    sb.Append($"<line x1=\"{callout.Anchor.X:F0}\" y1=\"{callout.Anchor.Y:F0}\" " +
                              $"x2=\"{callout.Label.X - 8:F0}\" y2=\"{callout.Label.Y:F0}\" " +
                              $"stroke=\"#94a3b8\" stroke-width=\"0.8\" stroke-opacity=\"0.7\"/>");
                    // Dot on the state
                    sb.Append($"<circle cx=\"{callout.Anchor.X:F0}\" cy=\"{callout.Anchor.Y:F0}\" r=\"2\" " +
                              $"fill=\"#94a3b8\" opacity=\"0.7\"/>");
                    // Label at callout position
                    sb.Append($"<text x=\"{callout.Label.X:F0}\" y=\"{callout.Label.Y:F0}\" " +
                              $"text-anchor=\"middle\" dominant-baseline=\"central\" " +
                              $"font-family=\"system-ui,sans-serif\" font-size=\"8.5\" font-weight=\"700\" " +
                              $"fill=\"#334155\" stroke=\"#fff\" stroke-width=\"2\" stroke-opacity=\"0.82\" " +
                              $"style=\"paint-order:stroke\">{postal}</text>");
                }
                else
                {
                    sb.Append($"<text x=\"{c.X:F0}\" y=\"{c.Y:F0}\" " +
                              $"text-anchor=\"middle\" dominant-baseline=\"central\" " +
                              $"font-family=\"system-ui,sans-serif\" font-size=\"9\" font-weight=\"700\" " +
                              $"fill=\"{textFill}\" stroke=\"{stroke}\" stroke-width=\"2.2\" stroke-opacity=\"0.52\" " +
                              $"style=\"paint-order:stroke\">{postal}</text>");
                }
            }

            sb.Append("</g>");

            var insertAt = svg.LastIndexOf("</svg>", StringComparison.OrdinalIgnoreCase);
            return insertAt < 0 ? svg : svg[..insertAt] + sb + svg[insertAt..];
        }

        private static string GetTextColor(string hexFill)
        {
            if (hexFill.StartsWith('#') && hexFill.Length == 7)
            {
                var r = Convert.ToInt32(hexFill[1..3], 16) / 255.0;
                var g = Convert.ToInt32(hexFill[3..5], 16) / 255.0;
                var b = Convert.ToInt32(hexFill[5..7], 16) / 255.0;
                var luminance = 0.299 * r + 0.587 * g + 0.114 * b;
                return luminance > 0.50 ? "#334155" : "#fff";
            }
            return "#fff";
        }
    }
}
