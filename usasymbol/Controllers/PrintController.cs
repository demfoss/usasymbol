using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Data;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using YamlDotNet.RepresentationModel;

namespace USASymbol.Controllers
{
    public class PrintController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly AppDbContext _db;

        public PrintController(IWebHostEnvironment env, IMemoryCache cache, AppDbContext db)
        {
            _env = env;
            _cache = cache;
            _db = db;
        }

        [HttpGet("/print/download")]
        public IActionResult Download(string slug, bool includeImages = false, string? source = null, string? category = null)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

            var normalizedSource = string.IsNullOrWhiteSpace(source) ? "symbols" : source.Trim().ToLowerInvariant();
            var normalizedCategory = string.IsNullOrWhiteSpace(category) ? string.Empty : category.Trim().ToLowerInvariant();

            var filePath = ResolveYamlPath(normalizedSource, normalizedCategory, slug);
            if (string.IsNullOrEmpty(filePath)) return NotFound();

            var resolvedSource = normalizedSource;
            var resolvedCategory = normalizedCategory;
            InferSourceAndCategory(filePath, ref resolvedSource, ref resolvedCategory);

            var cacheKey = $"print:pdf:{resolvedSource}:{resolvedCategory}:{slug}:imgs:{includeImages}";
            if (_cache.TryGetValue(cacheKey, out byte[]? cached) && cached != null)
            {
                var cachedName = BuildFileName(slug, resolvedSource, includeImages);
                return File(cached, "application/pdf", cachedName);
            }


            var yaml = System.IO.File.ReadAllText(filePath);
            var input = new StringReader(yaml);
            var yamlStream = new YamlStream();
            yamlStream.Load(input);
            var root = (YamlMappingNode)yamlStream.Documents[0].RootNode;

            string title = slug;
            if (root.Children.TryGetValue(new YamlScalarNode("page"), out var pageNode))
            {
                var pageMap = pageNode as YamlMappingNode;
                if (pageMap != null && pageMap.Children.TryGetValue(new YamlScalarNode("h1"), out var h1))
                    title = (h1 as YamlScalarNode)?.Value ?? title;
            }

            var headers = new List<string>();
            var columnKeys = new List<string>();
            var rows = new List<List<string>>();

            if (root.Children.TryGetValue(new YamlScalarNode("table"), out var tableNode))
            {
                var tableMap = tableNode as YamlMappingNode;
                if (tableMap != null && tableMap.Children.TryGetValue(new YamlScalarNode("columns"), out var cols))
                {
                    var colsMap = cols as YamlMappingNode;
                    if (colsMap != null)
                    {
                        foreach (var kv in colsMap.Children)
                        {
                            var key = ((YamlScalarNode)kv.Key).Value ?? "";
                            var label = ((YamlScalarNode)kv.Value).Value ?? key;
                            columnKeys.Add(key);
                            headers.Add(label);
                        }
                    }
                }

                if (tableMap != null && tableMap.Children.TryGetValue(new YamlScalarNode("rows"), out var rowsNode))
                {
                    var seq = rowsNode as YamlSequenceNode;
                    if (seq != null)
                    {
                        foreach (YamlMappingNode rowMap in seq.Children.Cast<YamlMappingNode>())
                        {
                            var row = new List<string>();
                            foreach (var key in columnKeys)
                            {
                                if (rowMap.Children.TryGetValue(new YamlScalarNode(key), out var val))
                                    row.Add((val as YamlScalarNode)?.Value ?? "");
                                else
                                    row.Add("");
                            }
                            rows.Add(row);
                        }
                    }
                }
            }

            var relativePageUrl = ReadRootScalar(root, "url");
            var defaultPath = resolvedSource switch
            {
                "rankings" => $"/rankings/{resolvedCategory}/{slug}".Replace("//", "/"),
                "collections" => $"/collections/{resolvedCategory}/{slug}".Replace("//", "/"),
                _ => $"/symbols/{slug}"
            };
            var sourcePath = !string.IsNullOrWhiteSpace(relativePageUrl) ? relativePageUrl! : defaultPath;
            var sourceUrl = $"{Request.Scheme}://{Request.Host}{sourcePath}";


            string webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string[] logoCandidates = { "images/profile/logo.png", "images/profile/logo.svg", "images/profile/logo1.png", "images/og-default.webp" };
            byte[]? logoBytes = null;
            foreach (var cand in logoCandidates)
            {
                var p = Path.Combine(webRoot, cand.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(p))
                {
                    logoBytes = System.IO.File.ReadAllBytes(p);
                    break;
                }
            }


            var doc = new SymbolListDocument(webRoot, _db)
            {
                Title = title,
                Headers = headers,
                Rows = rows,
                ColumnKeys = columnKeys,
                IncludeImages = includeImages,
                LogoBytes = logoBytes,
                SourceFile = filePath,
                SourceUrl = sourceUrl,
                Slug = slug
            };

            using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            ms.Position = 0;
            var fileName = BuildFileName(slug, resolvedSource, includeImages);

            var bytes = ms.ToArray();

            _cache.Set(cacheKey, bytes, TimeSpan.FromMinutes(30));

            return File(bytes, "application/pdf", fileName);
        }

        private static string BuildFileName(string slug, string source, bool includeImages)
        {
            var suffix = source switch
            {
                "rankings" => "-ranking",
                "collections" => "-collection",
                _ => "-list"
            };
            return (slug + suffix + (includeImages ? "-images.pdf" : ".pdf"))
                .Replace("/", "-")
                .Replace("..", "");
        }

        private static string? ResolveYamlPath(string source, string category, string slug)
        {
            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content");

            if (source == "rankings")
            {
                var direct = TryPickYaml(Path.Combine(contentRoot, "rankings", category), slug);
                if (!string.IsNullOrEmpty(direct)) return direct;

                return FindYamlRecursive(Path.Combine(contentRoot, "rankings"), slug);
            }

            if (source == "collections")
            {
                var direct = TryPickYaml(Path.Combine(contentRoot, "collections", category), slug);
                if (!string.IsNullOrEmpty(direct)) return direct;

                return FindYamlRecursive(Path.Combine(contentRoot, "collections"), slug);
            }

            var symbols = TryPickYaml(Path.Combine(contentRoot, "symbols"), slug);
            if (!string.IsNullOrEmpty(symbols)) return symbols;


            var rankings = FindYamlRecursive(Path.Combine(contentRoot, "rankings"), slug);
            if (!string.IsNullOrEmpty(rankings)) return rankings;

            return FindYamlRecursive(Path.Combine(contentRoot, "collections"), slug);
        }

        private static void InferSourceAndCategory(string filePath, ref string source, ref string category)
        {
            var normalizedPath = filePath.Replace('\\', '/');

            if (normalizedPath.Contains("/Content/rankings/", StringComparison.OrdinalIgnoreCase))
            {
                source = "rankings";
            }
            else if (normalizedPath.Contains("/Content/collections/", StringComparison.OrdinalIgnoreCase))
            {
                source = "collections";
            }
            else
            {
                source = "symbols";
            }

            var directory = Path.GetDirectoryName(filePath);
            category = directory == null ? category : Path.GetFileName(directory)?.ToLowerInvariant() ?? category;
        }

        private static string? TryPickYaml(string folder, string slug)
        {
            if (!Directory.Exists(folder)) return null;

            var yml = Path.Combine(folder, slug + ".yml");
            if (System.IO.File.Exists(yml)) return yml;

            var yaml = Path.Combine(folder, slug + ".yaml");
            if (System.IO.File.Exists(yaml)) return yaml;

            return null;
        }

        private static string? FindYamlRecursive(string folder, string slug)
        {
            if (!Directory.Exists(folder)) return null;

            var yml = Directory.GetFiles(folder, slug + ".yml", SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrEmpty(yml)) return yml;

            return Directory.GetFiles(folder, slug + ".yaml", SearchOption.AllDirectories).FirstOrDefault();
        }

        private static string? ReadRootScalar(YamlMappingNode root, string key)
        {
            if (!root.Children.TryGetValue(new YamlScalarNode(key), out var node))
                return null;

            return (node as YamlScalarNode)?.Value;
        }


        private class SymbolListDocument : IDocument
        {
            public string Title { get; set; } = "List";
            public List<string> Headers { get; set; } = new();
            public List<List<string>> Rows { get; set; } = new();
            public List<string> ColumnKeys { get; set; } = new();
            public bool IncludeImages { get; set; } = false;
            public byte[]? LogoBytes { get; set; }
            public string? SourceFile { get; set; }
            public string? SourceUrl { get; set; }
            public string? Slug { get; set; }
            private readonly string _webRoot;
            private readonly AppDbContext? _dbLocal;

            public SymbolListDocument(string webRoot, AppDbContext? db)
            {
                _webRoot = webRoot ?? string.Empty;
                _dbLocal = db;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                            page.Header().Element(h =>
                            {
                                h.Row(r =>
                                {
                                    if (LogoBytes != null)
                                    {
                                        r.ConstantItem(64).AlignLeft().Element(e => e.Image(LogoBytes).FitArea());
                                    }
                                    r.RelativeItem().AlignCenter().Column(col =>
                                    {
                                        col.Item().Text(Title).FontSize(36).SemiBold();
                                        col.Item().Text("usasymbol.com").FontSize(10).FontColor(Colors.Grey.Darken1);
                                    });
                                });
                            });

                    page.Content().Element(c => BuildTable(c));

                    page.Footer().Column(f =>
                    {
                        f.Item().AlignCenter().Text("Generated by usasymbol.com").FontSize(10).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrEmpty(SourceUrl))
                        {
                            f.Item().AlignCenter().Text($"Source: {SourceUrl}").FontSize(9).FontColor(Colors.Grey.Lighten1);
                        }
                    });
                });
            }

            void BuildTable(IContainer container)
            {
                container.PaddingTop(10).Column(col =>
                {

                    col.Item().PaddingVertical(8).Element(x =>
                    {
                        x.Table(table =>
                        {

                            var imageColumnIndices = new List<int>();
                            for (int i = 0; i < Headers.Count; i++)
                            {
                                var h = Headers[i] ?? string.Empty;
                                var k = ColumnKeys.ElementAtOrDefault(i) ?? string.Empty;
                                if (h.Trim().ToLowerInvariant().Contains("image") || k.Trim().ToLowerInvariant().Contains("image"))
                                    imageColumnIndices.Add(i);
                            }


                            List<int> visibleIndices;
                            if (IncludeImages && imageColumnIndices.Any())
                            {
                                visibleIndices = Enumerable.Range(0, Headers.Count).ToList();
                            }
                            else
                            {

                                visibleIndices = Enumerable.Range(0, Headers.Count).Where(i => !imageColumnIndices.Contains(i)).ToList();
                            }


                            table.ColumnsDefinition(columns =>
                            {
                                for (int c = 0; c < Math.Max(1, visibleIndices.Count); c++)
                                    columns.RelativeColumn();
                            });


                            table.Header(header =>
                            {
                                foreach (var idx in visibleIndices)
                                {
                                    var h = Headers[idx];
                                    header.Cell().Background(Colors.Teal.Darken1).Padding(8).Text(h).FontColor(Colors.White).FontSize(12).SemiBold();
                                }
                            });


                            foreach (var row in Rows)
                            {
                                foreach (var idx in visibleIndices)
                                {
                                    var cell = idx < row.Count ? row[idx] : "";

                                    if (IncludeImages && imageColumnIndices.Contains(idx) && IsImagePath(cell))
                                    {
                                        var local = ResolveLocalPath(cell);
                                        if (!string.IsNullOrEmpty(local) && System.IO.File.Exists(local))
                                        {
                                            var bytes = System.IO.File.ReadAllBytes(local);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Element(el =>
                                            {
                                                el.AlignCenter().Height(64).Element(img => img.Image(bytes).FitArea());
                                            });
                                        }
                                        else
                                        {
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text("");
                                        }
                                    }
                                    else
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(cell);
                                    }
                                }
                            }
                        });
                    });


                });
            }

            bool IsImagePath(string s)
            {
                var lower = s.Trim().ToLowerInvariant();
                return lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".webp") || lower.StartsWith("/images/");
            }

            string? ResolveLocalPath(string s)
            {
                var p = s.Trim();
                if (p.StartsWith("/")) p = p.TrimStart('/');
                var candidate = Path.Combine(_webRoot ?? string.Empty, p.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(candidate)) return candidate;

                var name = Path.GetFileName(p);
                if (!string.IsNullOrEmpty(name))
                {
                    var dir = Path.Combine(_webRoot ?? string.Empty, "images");
                    if (Directory.Exists(dir))
                    {
                        var found = Directory.GetFiles(dir, "*" + name, SearchOption.AllDirectories).FirstOrDefault();
                        if (!string.IsNullOrEmpty(found)) return found;
                    }
                }

                try
                {
                    if (_dbLocal != null && !string.IsNullOrEmpty(name))
                    {
                        var symbol = _dbLocal.Symbols.FirstOrDefault(sy => sy.ImageUrl != null && sy.ImageUrl.EndsWith(name, StringComparison.OrdinalIgnoreCase));
                        if (symbol != null && !string.IsNullOrEmpty(symbol.ImageUrl))
                        {
                            var url = symbol.ImageUrl.Trim();
                            if (url.StartsWith("/")) url = url.TrimStart('/');
                            var candidate2 = Path.Combine(_webRoot ?? string.Empty, url.Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(candidate2)) return candidate2;
                        }
                    }
                }
                catch
                {

                }

                return null;
            }
        }
    }
}
