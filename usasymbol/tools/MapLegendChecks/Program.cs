using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Usasymbol.Helpers;
using Usasymbol.ViewModels;
using USASymbol.Models.Content;
using USASymbol.Options;
using USASymbol.Services;
using USASymbol.Services.Images;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root != null && !File.Exists(Path.Combine(root.FullName, "usasymbol.csproj"))) root = root.Parent;
if (root == null) throw new InvalidOperationException("Run from a checkout of usasymbol.");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ApplicationName = typeof(PageContentService).Assembly.GetName().Name,
    ContentRootPath = root.FullName,
    WebRootPath = Path.Combine(root.FullName, "wwwroot"),
    EnvironmentName = "Development"
});
builder.Logging.ClearProviders();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(PageContentService).Assembly);
builder.Services.AddSingleton<IImagePathNormalizer, ImagePathNormalizer>();
builder.Services.AddSingleton(Options.Create(new BunnyOptions()));
builder.Services.AddSingleton<IImageUrlService, ImageUrlService>();
await using var app = builder.Build();
using var cache = new MemoryCache(new MemoryCacheOptions());
var contentService = new RankingsContentService(NullLogger<PageContentService>.Instance, builder.Environment, cache);
var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++;
}

ChoroplethResult Map(PageContent content) => string.IsNullOrEmpty(content.Map!.MetricKey)
    ? ChoroplethBuilder.BuildFlat(content.Table!.Rows, map: content.Map)
    : ChoroplethBuilder.Build(content.Map, content.Table!.Rows,
        content.Table.Columns.FirstOrDefault(column => column.Key == content.Map.MetricKey));
async Task<PageContent> Load(string category, string slug) =>
    await contentService.GetContentAsync(category, slug) ?? throw new InvalidOperationException($"Missing {category}/{slug}");

void Validate(ChoroplethResult result, string name)
{
    Check(result.Entries.Select(e => e.PostalCode).Distinct().Count() == result.Entries.Count, $"{name}: states are unique");
    Check(result.LegendGroups.All(g => g.StateCodes.Count > 0), $"{name}: no empty legend groups");
    Check(result.LegendGroups.Select(g => g.Key).Distinct().Count() == result.LegendGroups.Count, $"{name}: unique keys");
    foreach (var equal in result.Entries.Where(e => e.NumericValue.HasValue).GroupBy(e => e.NumericValue))
        Check(equal.Select(e => e.FillColor).Distinct().Count() == 1, $"{name}: equal values have equal colours");
    foreach (var entry in result.Entries)
    {
        var memberships = result.LegendGroups.Where(g => g.StateCodes.Contains(entry.PostalCode)).ToList();
        if (entry.NumericValue.HasValue)
        {
            Check(memberships.Count == 1 && !memberships[0].IsNoData, $"{name}/{entry.PostalCode}: one numeric group");
            Check(memberships[0].Color == entry.FillColor, $"{name}/{entry.PostalCode}: legend and map colour agree");
        }
        if (memberships.Any(g => g.IsNoData))
            Check(entry.FillColor == MapLegendBuilder.NoDataColor, $"{name}/{entry.PostalCode}: consistent missing-data colour");
    }
}

var asthma = await Load("health", "asthma-rate-by-state");
var asthmaMap = Map(asthma);
Validate(asthmaMap, "asthma");
Check(asthmaMap.LegendTitle.EndsWith('%'), "Percentage unit comes from the table format.");
Check(asthmaMap.LegendGroups.Single(g => g.IsNoData).StateCodes.OrderBy(c => c).SequenceEqual(new[] { "KY", "PA" }), "Asthma reports both missing states.");
Check(asthmaMap.LegendGroups.Single(g => g.IsNoData).Tooltip.Contains("Kentucky") && asthmaMap.LegendGroups.Single(g => g.IsNoData).Tooltip.Contains("Pennsylvania"), "Missing-data tooltip gives state names.");
Check(asthmaMap.Entries.Single(e => e.PostalCode == "WV").DisplayValue == "13.9%", "State values retain percentage units.");
Check(asthmaMap.LegendGroups.Count(g => !g.IsNoData) <= 5, "Numeric legend has at most five populated groups.");

var area = await Load("geography", "area-codes-by-state");
var areaMap = Map(area);
Validate(areaMap, "area codes");
var ones = areaMap.Entries.Where(e => e.NumericValue == 1).ToList();
Check(ones.Count == 11 && areaMap.LegendGroups.Count(g => ones.Any(e => g.StateCodes.Contains(e.PostalCode))) == 1, "All eleven tied states stay together.");
Check(areaMap.LegendGroups.Single(g => g.StateCodes.Contains(ones[0].PostalCode)).StateCodes.Count == 11, "The repeated minimum gets its own group.");
Check(areaMap.LegendGroups.All(g => !g.IsNoData), "No missing-data button when all states are present.");

var cancer = await Load("health", "cancer-rate-by-state");
Check(Map(cancer).LegendTitle.Contains("per 100,000"), "Rate denominator remains in the legend heading.");
var salary = await Load("economy", "nurse-salary-by-state");
Check(Map(salary).LegendGroups.Where(g => !g.IsNoData).All(g => g.Range.Contains('$')), "Salary bounds infer currency from C0 formatting.");
var legal = await Load("law", "marijuana-legality-by-state");
var legalMap = Map(legal);
Validate(legalMap, "legal status");
Check(legalMap.LegendGroups.First().Label == "Fully Legal", "Configured category order is retained.");
Check(legalMap.LegendGroups.All(g => g.Label is not "Lowest" and not "Highest"), "Categories never receive numeric order labels.");

List<StateMapEntry> Entries(params double?[] values) => values.Select((value, index) => new StateMapEntry(
    $"S{index}", $"State {index}", $"state-{index}", value, index + 1,
    value?.ToString(CultureInfo.InvariantCulture) ?? "No data", "#000000", Array.Empty<MapEntryDetail>())).ToList();
ChoroplethResult Numeric(double?[] values, PageMap? map = null, TableColumn? column = null) =>
    MapLegendBuilder.Numeric(Entries(values), map ?? new PageMap { MetricLabel = "Test metric" }, column);
var equal = Numeric(new double?[] { 1, 1, 1 });
Check(equal.LegendGroups.Count == 1 && equal.LegendGroups[0].Label == "1" && equal.Entries.All(e => e.FillColor != MapLegendBuilder.NoDataColor), "All-equal values remain real data in one group.");
var zero = Numeric(new double?[] { 0, 0, null });
Check(zero.LegendGroups.Single(g => !g.IsNoData).Label == "0", "Zero is a value, not missing data.");
Check(Numeric(new double?[] { null, null }).LegendGroups.Single().IsNoData, "An all-missing metric is explicit.");
var sparse = Numeric(new double?[] { 1, 1, 2, 3, 3 });
Check(sparse.LegendGroups.Select(g => g.Label).SequenceEqual(new[] { "1", "2", "3" }), "Few distinct values use exact labels, not artificial quantiles.");
var boundary = Numeric(new double?[] { 7, 8.9, 9, 9.9, 10, 10.9, 11, 11.49, 11.5, 14 },
    new PageMap { ValueType = "percent", MetricLabel = "Adults", Breaks = new() { 9, 10, 11, 11.5 } });
Check(boundary.LegendGroups[1].StateCodes.SequenceEqual(new[] { "S2", "S3" }), "Exact boundary belongs to the next group.");
Check(boundary.LegendGroups[3].Range == "11–<11.5%", "Fractional ranges clearly exclude the upper boundary.");
Check(boundary.LegendGroups[^1].StateCodes.Contains("S8") && boundary.LegendGroups[^1].StateCodes.Contains("S9"), "Last group includes boundary and maximum.");
var negative = Numeric(new double?[] { -10, -5, 0, 2, 5, 10, 20 }, new PageMap { ColorScheme = "diverging" });
Check(negative.LegendGroups.Any(g => g.Label == "Near zero") && negative.LegendGroups.All(g => !g.IsNoData), "Signed maps preserve their zero reference, separately from missing data.");
var reference = Numeric(new double?[] { 5, 8, 10, 12, 15 }, new PageMap
    { LegendMode = "reference", ReferenceValue = 10, ReferenceLabel = "U.S. average", ReferenceTolerance = 1 });
Check(reference.LegendTitle.Contains("U.S. average: 10") && reference.LegendGroups.Any(g => g.Label == "Near"), "Reference labels require explicit reference mode.");
Check(Numeric(new double?[] { 1, 2, 3, 4, 5, 6 }, new PageMap { ReferenceValue = 3 }).LegendGroups.All(g => g.Label != "Near"), "A reference alone does not change the map's meaning.");
var fractional = Numeric(new double?[] { .1, .2 }, column: new TableColumn { Format = "P1" });
Check(fractional.LegendGroups[0].Label == "10%", "P formats correctly convert fractional percentages.");
var years = Numeric(new double?[] { 1787, 1788, 1800, 1850, 1900, 1959 }, new PageMap { ValueType = "year", MetricKey = "admission_year" });
Check(years.LegendGroups.First().Label == "Earliest" && years.LegendGroups.All(g => !g.Range.Contains(',')), "Year labels have chronological wording and no thousands separators.");
var ranks = Numeric(new double?[] { 1, 10, 11, 20, 21, 50 }, new PageMap { ValueType = "rank_only" });
Check(ranks.LegendGroups[0].Label == "Top 10" && ranks.LegendGroups[0].StateCodes.Count == 2, "Rank-only maps use place bands.");
var big = Numeric(new double?[] { 1000000, 2500000, 5000000, 10000000, 40000000 });
Check(big.LegendGroups[1].Label == "2.5M", "Compact integers preserve fractional millions.");
var boolean = MapLegendBuilder.Categories(new List<StateMapEntry>
{
    new("AA", "A", "a", null, null, "Yes", "#000000", Array.Empty<MapEntryDetail>()),
    new("BB", "B", "b", null, null, "No", "#000000", Array.Empty<MapEntryDetail>()),
    new("CC", "C", "c", null, null, "N/A", "#000000", Array.Empty<MapEntryDetail>())
}, new PageMap { ValueType = "boolean" });
Check(boolean.LegendGroups.Single(g => g.Label == "No").StateCodes.Single() == "BB" && boolean.LegendGroups.Single(g => g.IsNoData).StateCodes.Single() == "CC", "No and No data are distinct.");
var ordinal = MapLegendBuilder.Categories(new List<StateMapEntry>
{
    new("AA", "A", "a", null, null, "High", "#000000", Array.Empty<MapEntryDetail>()),
    new("BB", "B", "b", null, null, "Low", "#000000", Array.Empty<MapEntryDetail>())
}, new PageMap { ValueType = "ordinal", CategoryOrder = new() { "Low", "High" } });
Check(ordinal.LegendGroups.Select(group => group.Label).SequenceEqual(new[] { "Low", "High" }), "Ordinal categories follow the configured meaning, not alphabetical order.");
var fractionalCut = Numeric(new double?[] { 0, 1, 2, 3, 4, 5 }, new PageMap { ValueType = "count", Breaks = new() { 2.5 } });
Check(fractionalCut.LegendGroups[0].Range == "Under 3" && fractionalCut.LegendGroups[0].StateCodes.Count == 3, "Count thresholds use equivalent integral boundaries.");
var precise = Numeric(new double?[] { 0, 1, 2, 3, 4, 5 }, new PageMap { ValueType = "percent", Decimals = 0, Breaks = new() { 2.5 } });
Check(precise.LegendGroups[0].Range == "Under 2.5%", "Display precision cannot round a threshold into a misleading boundary.");
var outside = Numeric(new double?[] { 1, 2, 3, 4, 5, 6 }, new PageMap { Breaks = new() { 100 } });
Check(outside.LegendGroups.Single().Range == "1–6", "Out-of-domain overrides fall back to the actual range, never infinity.");
var explicitFew = Numeric(new double?[] { 8, 10, 12 }, new PageMap { ValueType = "percent", Breaks = new() { 9, 11 } });
Check(explicitFew.LegendGroups[0].Range == "Under 9%", "Explicit thresholds remain authoritative with few distinct values.");
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
var localized = ChoroplethBuilder.Build(new PageMap { MetricKey = "value", ValueType = "percent" }, new List<TableRow>
{
    new() { Data = new() { ["state"] = "Virginia", ["postal_code"] = "VA", ["value"] = 10.2 } }
});
Check(localized.Entries.Single(entry => entry.PostalCode == "VA").NumericValue == 10.2, "Numeric inputs do not change meaning under a comma-decimal server culture.");
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

// Audit real content through the same parser and map builder used by the website.
var audited = 0;
foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Content", "rankings"), "*.*", SearchOption.AllDirectories)
    .Where(file => file.EndsWith(".yml") || file.EndsWith(".yaml")))
{
    var content = await contentService.GetContentAsync(new DirectoryInfo(Path.GetDirectoryName(file)!).Name, Path.GetFileNameWithoutExtension(file));
    if (content?.Map == null || content.Table?.Rows.Count is not > 0) continue;
    Validate(Map(content), Path.GetFileName(file));
    audited++;
}

async Task<string> Render(MapBlockViewModel model)
{
    using var scope = app.Services.CreateScope();
    var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    var action = new ActionContext(context, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor());
    var engine = scope.ServiceProvider.GetRequiredService<ICompositeViewEngine>();
    var view = engine.GetView(null, "/Views/Shared/Components/MapBlock.cshtml", false);
    if (!view.Success) throw new InvalidOperationException("Map view could not be resolved.");
    using var writer = new StringWriter(CultureInfo.InvariantCulture);
    var data = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
    var temp = new TempDataDictionary(context, scope.ServiceProvider.GetRequiredService<ITempDataProvider>());
    await view.View.RenderAsync(new ViewContext(action, view.View, data, temp, writer, new HtmlHelperOptions()));
    return writer.ToString();
}

var output = Path.Combine(root.FullName, "artifacts", "map-legend-checks");
Directory.CreateDirectory(output);
var yearMap = MapLegendBuilder.Numeric(areaMap.Entries.Select((entry, index) => entry with
    { NumericValue = 1787 + index * 3 }).ToList(), new PageMap { ValueType = "year", MetricLabel = "Statehood year (synthetic example)" }, null);
foreach (var (slug, result) in new[] { ("asthma", asthmaMap), ("area-codes", areaMap), ("categories", legalMap), ("cancer", Map(cancer)), ("salary", Map(salary)), ("years", yearMap) })
{
    var model = new MapBlockViewModel { Entries = result.Entries, LegendGroups = result.LegendGroups, LegendTitle = result.LegendTitle,
        MetricLabel = result.LegendTitle, Title = slug, ColorCss = string.Join("", result.Entries.Select(e => $".{e.PostalCode.ToLowerInvariant()}{{fill:{e.FillColor}}}")) };
    var html = await Render(model);
    Check(Regex.Matches(html, "id=\"map-legend\"").Count == 1, $"{slug}: exactly one interactive legend");
    Check(!html.Contains("data-state-key=\"HI\""), $"{slug}: no duplicate Hawaii button");
    Check(Regex.Matches(html, "data-legend-key=").Count == result.LegendGroups.Count, $"{slug}: one button per actual group");
    var embedded = Regex.Match(html, "<script type=\"application/json\"\\s+id=\"choropleth-data\"[^>]*>(.*?)</script>", RegexOptions.Singleline);
    using var json = JsonDocument.Parse(embedded.Groups[1].Value);
    Check(json.RootElement.EnumerateObject().All(property => property.Value.TryGetProperty("lk", out _)), $"{slug}: client receives server group membership");
    var svg = File.ReadAllText(Path.Combine(root.FullName, "wwwroot", "maps", "us-states.svg"));
    html = Regex.Replace(html, "(data-svg-src=\"/maps/us-states.svg\"[\\s\\S]*?aria-label=\"[^\"]*\">)\\s*</div>", match => match.Groups[1].Value + svg + "</div>", RegexOptions.None);
    html = html.Replace("data-svg-src=\"/maps/us-states.svg\"", "");
    html = html.Replace("src=\"/js/", $"src=\"{new Uri(Path.Combine(root.FullName, "wwwroot/js")).AbsoluteUri}/");
    html = Regex.Replace(html, "\\?v=[^\"]+", "");
    var document = "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
        + $"<link rel=\"stylesheet\" href=\"{new Uri(Path.Combine(root.FullName, "wwwroot/css/main.css")).AbsoluteUri}\">"
        + "<style>body{background:white;padding:16px}main{width:100%;min-width:0;max-width:760px;margin:auto}</style><main>" + html + "</main></html>";
    await File.WriteAllTextAsync(Path.Combine(output, slug + ".html"), document);
}
Console.WriteLine($"Passed {checks:N0} map checks across {audited} real ranking maps. Snapshots: {output}");
