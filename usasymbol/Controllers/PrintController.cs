using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
        public IActionResult DownloadRedirect(string slug, bool includeImages = false, string? source = null, string? category = null)
        {
            return ResolvePrintRequest(slug, includeImages, source, category, generatePdf: false);
        }

        [HttpPost("/print/download")]
        [ValidateAntiForgeryToken]
        public IActionResult Download(string slug, bool includeImages = false, string? source = null, string? category = null)
        {
            return ResolvePrintRequest(slug, includeImages, source, category, generatePdf: true);
        }

        private IActionResult ResolvePrintRequest(string slug, bool includeImages, string? source, string? category, bool generatePdf)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

            var normalizedSource = string.IsNullOrWhiteSpace(source) ? "symbols" : source.Trim().ToLowerInvariant();
            var normalizedCategory = string.IsNullOrWhiteSpace(category) ? string.Empty : category.Trim().ToLowerInvariant();

            var filePath = ResolveYamlPath(normalizedSource, normalizedCategory, slug);
            if (string.IsNullOrEmpty(filePath)) return NotFound();

            var resolvedSource = normalizedSource;
            var resolvedCategory = normalizedCategory;
            InferSourceAndCategory(filePath, ref resolvedSource, ref resolvedCategory);

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
            var sourceUrl = $"https://usasymbol.com{sourcePath}";

            Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";

            if (!generatePdf)
                return RedirectPermanent(sourcePath);

            var cacheKey = $"print:pdf:{resolvedSource}:{resolvedCategory}:{slug}:imgs:{includeImages}";
            if (_cache.TryGetValue(cacheKey, out byte[]? cached) && cached != null)
            {
                var cachedName = BuildFileName(slug, resolvedSource, includeImages);
                return File(cached, "application/pdf", cachedName);
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var document = new SymbolListDocument(webRoot, _db)
            {
                Title = title,
                Headers = headers,
                Rows = rows,
                ColumnKeys = columnKeys,
                IncludeImages = false,
                SourceUrl = sourceUrl
            };

            using var stream = new MemoryStream();
            QuestPDF.Settings.License = LicenseType.Community;
            document.GeneratePdf(stream);
            var bytes = stream.ToArray();
            var fileName = BuildFileName(slug, resolvedSource, includeImages: false);

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

        private sealed class SymbolListDocument : IDocument
        {
            private readonly string _webRoot;
            private readonly AppDbContext _db;

            public SymbolListDocument(string webRoot, AppDbContext db)
            {
                _webRoot = webRoot;
                _db = db;
            }

            public string Title { get; set; } = "List";
            public List<string> Headers { get; set; } = new();
            public List<List<string>> Rows { get; set; } = new();
            public List<string> ColumnKeys { get; set; } = new();
            public bool IncludeImages { get; set; }
            public byte[]? LogoBytes { get; set; }
            public string? SourceUrl { get; set; }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(text => text.FontSize(11));

                    page.Header().Element(BuildHeader);
                    page.Content().PaddingTop(14).Element(BuildTable);
                    page.Footer().Element(BuildFooter);
                });
            }

            private void BuildHeader(IContainer container)
            {
                container.Row(row =>
                {
                    if (LogoBytes != null)
                    {
                        row.ConstantItem(64).AlignLeft().Element(item => item.Image(LogoBytes).FitArea());
                    }

                    row.RelativeItem().AlignCenter().Column(column =>
                    {
                        column.Item().Text(Title).FontSize(30).SemiBold();
                        column.Item().Text("usasymbol.com").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                });
            }

            private void BuildTable(IContainer container)
            {
                var visibleIndices = GetVisibleColumnIndices();

                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var index = 0; index < Math.Max(1, visibleIndices.Count); index++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var index in visibleIndices)
                        {
                            header.Cell()
                                .Background(Colors.Teal.Darken1)
                                .Padding(7)
                                .Text(Headers[index])
                                .FontColor(Colors.White)
                                .FontSize(11)
                                .SemiBold();
                        }
                    });

                    foreach (var row in Rows)
                    {
                        foreach (var index in visibleIndices)
                        {
                            var value = index < row.Count ? row[index] : string.Empty;
                            var cell = table.Cell()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten3)
                                .Padding(6);

                            if (IncludeImages && IsImageColumn(index) && TryReadImage(value, out var imageBytes))
                            {
                                cell.AlignCenter().Height(64).Element(image => image.Image(imageBytes).FitArea());
                            }
                            else
                            {
                                cell.Text(value);
                            }
                        }
                    }
                });
            }

            private void BuildFooter(IContainer container)
            {
                container.Column(column =>
                {
                    column.Item().AlignCenter().Text("Generated by usasymbol.com").FontSize(10).FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(SourceUrl))
                    {
                        column.Item().AlignCenter().Text($"Source: {SourceUrl}").FontSize(9).FontColor(Colors.Grey.Lighten1);
                    }
                });
            }

            private List<int> GetVisibleColumnIndices()
            {
                var all = Enumerable.Range(0, Headers.Count).ToList();

                if (IncludeImages)
                {
                    return all;
                }

                return all.Where(index => !IsImageColumn(index)).ToList();
            }

            private bool IsImageColumn(int index)
            {
                var header = Headers.ElementAtOrDefault(index) ?? string.Empty;
                var key = ColumnKeys.ElementAtOrDefault(index) ?? string.Empty;

                return header.Contains("image", StringComparison.OrdinalIgnoreCase)
                    || key.Contains("image", StringComparison.OrdinalIgnoreCase);
            }

            private bool TryReadImage(string value, out byte[] bytes)
            {
                bytes = Array.Empty<byte>();

                var localPath = ResolveLocalImagePath(value);
                if (string.IsNullOrEmpty(localPath) || !System.IO.File.Exists(localPath))
                {
                    return false;
                }

                bytes = System.IO.File.ReadAllBytes(localPath);
                return true;
            }

            private string? ResolveLocalImagePath(string value)
            {
                var path = value.Trim();
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                if (path.StartsWith("/"))
                {
                    path = path.TrimStart('/');
                }

                var direct = Path.Combine(_webRoot, path.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(direct))
                {
                    return direct;
                }

                var fileName = Path.GetFileName(path);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return null;
                }

                var imageRoot = Path.Combine(_webRoot, "images");
                if (Directory.Exists(imageRoot))
                {
                    var found = Directory.GetFiles(imageRoot, fileName, SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }

                var symbol = _db.Symbols.FirstOrDefault(symbol =>
                    symbol.ImageUrl != null &&
                    symbol.ImageUrl.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(symbol?.ImageUrl))
                {
                    return null;
                }

                var imageUrl = symbol.ImageUrl.TrimStart('/');
                var fromDb = Path.Combine(_webRoot, imageUrl.Replace('/', Path.DirectorySeparatorChar));

                return System.IO.File.Exists(fromDb) ? fromDb : null;
            }
        }
    }
}
