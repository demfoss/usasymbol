using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using USASymbol.Services;
using Usasymbol.Helpers;

var category = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0].Trim().ToLowerInvariant()
    : "agriculture";

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var webRoot = Path.Combine(repoRoot, "wwwroot");
var contentRoot = repoRoot;
var rankingsRoot = Path.Combine(repoRoot, "Content", "rankings");
var categoryDir = Path.Combine(rankingsRoot, category);

var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
var cache = new MemoryCache(new MemoryCacheOptions());
var env = new ToolWebHostEnvironment(contentRoot, webRoot);

var rankings = new RankingsContentService(
    loggerFactory.CreateLogger<PageContentService>(),
    env);

var mapService = new MapPngService(
    env,
    loggerFactory.CreateLogger<MapPngService>(),
    cache);

var files = Directory.GetFiles(categoryDir, "*.yml")
    .Concat(Directory.GetFiles(categoryDir, "*.yaml"))
    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
    .ToList();

var generated = 0;
var skipped = 0;
var failed = new List<string>();

foreach (var file in files)
{
    var slug = Path.GetFileNameWithoutExtension(file);
    var content = await rankings.GetContentAsync(category, slug);
    if (content?.Map == null || string.IsNullOrWhiteSpace(content.Map.Image))
    {
        skipped++;
        continue;
    }

    var outputPath = Path.Combine(webRoot, content.Map.Image.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    if (File.Exists(outputPath))
    {
        skipped++;
        continue;
    }

    var rows = content.Table?.Rows;
    if (rows == null || rows.Count == 0)
    {
        failed.Add($"{slug}: no table rows");
        continue;
    }

    var choropleth = string.IsNullOrWhiteSpace(content.Map.MetricKey)
        ? ChoroplethBuilder.BuildFlat(rows)
        : ChoroplethBuilder.Build(content.Map, rows);

    if (choropleth.Entries.Count == 0)
    {
        failed.Add($"{slug}: no choropleth entries");
        continue;
    }

    var generatedPath = await mapService.EnsureMapPngAsync(slug, choropleth.Entries);
    if (string.IsNullOrWhiteSpace(generatedPath))
    {
        failed.Add($"{slug}: map service returned null");
        continue;
    }

    var pngPath = Path.Combine(webRoot, generatedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(pngPath))
    {
        failed.Add($"{slug}: generated png missing");
        continue;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

    using var bitmap = SKBitmap.Decode(pngPath);
    if (bitmap == null)
    {
        failed.Add($"{slug}: failed to decode png");
        continue;
    }

    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Jpeg, 92);
    await using var fs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
    data.SaveTo(fs);
    generated++;
    Console.WriteLine($"generated {slug}");
}

Console.WriteLine($"generated={generated}");
Console.WriteLine($"skipped={skipped}");
Console.WriteLine($"failed={failed.Count}");
foreach (var item in failed)
{
    Console.WriteLine(item);
}

internal sealed class ToolWebHostEnvironment : IWebHostEnvironment
{
    public ToolWebHostEnvironment(string contentRoot, string webRoot)
    {
        ApplicationName = "GenerateAgricultureMaps";
        EnvironmentName = "Development";
        ContentRootPath = contentRoot;
        ContentRootFileProvider = new PhysicalFileProvider(contentRoot);
        WebRootPath = webRoot;
        WebRootFileProvider = new PhysicalFileProvider(webRoot);
    }

    public string ApplicationName { get; set; }
    public IFileProvider WebRootFileProvider { get; set; }
    public string WebRootPath { get; set; }
    public string EnvironmentName { get; set; }
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
}
