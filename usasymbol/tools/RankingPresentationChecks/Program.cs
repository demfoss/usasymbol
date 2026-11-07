using System.Globalization;
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
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Options;
using USASymbol.Services;
using USASymbol.Services.Images;

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
var service = new RankingsContentService(NullLogger<PageContentService>.Instance, builder.Environment, cache);
var checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    checks++;
}
bool Near(double actual, double expected) => Math.Abs(actual - expected) < .000001;

var area = await service.GetContentAsync("geography", "area-codes-by-state")
    ?? throw new InvalidOperationException("Area codes fixture could not be loaded.");
var ranking = RankingPresentation.Create(area.Table)!;
var extremes = new RankingExtremesPresentation(area, ranking, true, true);
Check(ranking.Ordered.Count == 50, "Real fixture contains all 50 states.");
Check(area.Table!.Columns.Single(c => c.Key == "area_codes").Type == "text", "Code lists must remain wrappable text.");
Check(extremes.Bottom!.Grouped && extremes.Bottom.Rows.Count == 11, "The lowest group includes all 11 tied states.");
Check(extremes.Bottom.Groups.Single().Heading == "States with just 1 area code (11)", "The tied group has a meaningful unit and count.");
Check(extremes.Bottom.Rows[0].State == "Alaska" && extremes.Bottom.Rows[^1].State == "Wyoming", "Ties run alphabetically, including in the bottom list.");
Check(extremes.Top!.Grouped && extremes.Top.Rows.Count == 12, "The four states tied at rank 9 are not cut at ten.");
Check(extremes.Top.Rows.Count(p => p.Value == 10) == 4, "The complete top boundary group is shown.");
Check(Near(extremes.Bar(1).Width, 100d / 41) && Near(extremes.Bar(41).Width, 100), "Both sides share a zero-to-41 scale.");
var alaska = ranking.Ordered.Single(p => p.State == "Alaska");
Check(ranking.For(alaska.Row)!.Label == "T-40" && ranking.For(alaska.Row)!.TieCount == 11, "Competition rank and tie size are correct.");
Check(ranking.For(alaska.Row)!.Tooltip.Contains("10 other states"), "The rank tooltip explains the tie.");
var takeaways = RankingTakeawaysBuilder.Build(new PageDetailViewModel { Content = area })!;
Check(takeaways.Options.Single(p => p.Slug == "alaska").Context == ranking.For(alaska.Row)!.Context,
    "The state picker and table share the same rank context.");
Check(takeaways.Options.Single(p => p.Slug == "alaska").RankLabel == "T-40", "The state picker exposes the same visible tied rank.");

PageContent Fixture(double[] values, string direction = "desc", string key = "value")
{
    var table = new PageTable
    {
        DefaultColumn = key, SortDefault = direction,
        Columns = new List<TableColumn> { new() { Key = key, Label = key, Type = "number", Format = "N1" } },
        Rows = values.Select((value, i) => new TableRow { Data = new Dictionary<string, object?>
        {
            [key] = value, ["state"] = ranking.Ordered[i].State, ["state_slug"] = ranking.Ordered[i].StateSlug
        } }).ToList()
    };
    return new PageContent { Tables = new List<PageTable> { table }, ExtremesMode = "both" };
}
RankingExtremesPresentation Present(PageContent content) => new(content, RankingPresentation.Create(content.Table)!, true, true);
var acrossFifth = Present(Fixture(new double[] { 20, 19, 18, 17, 10, 10, 10, 10, 10, 10, 10, 5, 4, 3, 2, 1 }));
Check(acrossFifth.Top!.Grouped && acrossFifth.Top.Rows.Count == 11, "A tie spanning fifth through eleventh place is complete.");
var equal = Present(Fixture(Enumerable.Repeat(1d, 14).ToArray()));
Check(equal.Top!.Rows.Count == 14 && equal.Bottom!.Rows.Count == 14, "All-equal fixtures never discard states.");
Check(equal.Ranking.Ordered.All(p => equal.Ranking.For(p.Row)!.Label == "T-1"), "All-equal ranks are T-1.");
var zero = Present(Fixture(Enumerable.Repeat(0d, 14).ToArray()));
Check(Near(zero.Bar(0).Width, 0), "All-zero bars are empty, not full.");
var mixed = Present(Fixture(new double[] { -10, 0, 10, 20 }));
Check(Near(mixed.Bar(0).Width, 0) && Near(mixed.Bar(-10).Left, 0), "Negative values extend left of the shared zero baseline.");
var asc = Present(Fixture(new double[] { 8, 2, 2, 10, 1 }, "asc"));
Check(!asc.Top!.Highest && asc.Top.Rows[0].Value == 1 && asc.Bottom!.Rows[0].Value == 10, "Ascending rankings label both extremes correctly.");
Check(asc.Ranking.Ordered.Where(p => p.Value == 2).Select(p => p.State).SequenceEqual(
    asc.Ranking.Ordered.Where(p => p.Value == 2).Select(p => p.State).OrderBy(name => name, StringComparer.OrdinalIgnoreCase)),
    "Ascending ties also stay alphabetical.");
var twoValues = Present(Fixture(new double[] { 10, 10, 10, 10, 10, 1, 1, 1, 1, 1 }));
Check(twoValues.Top!.Grouped && twoValues.Bottom!.Grouped, "Fewer than three distinct values use grouped rendering.");
var unique = Present(Fixture(Enumerable.Range(1, 16).Select(i => (double)i).ToArray()));
Check(!unique.Top!.Grouped && !unique.Bottom!.Grouped, "Varied, untied data retains bar rows.");
var cancer = await service.GetContentAsync("health", "cancer-rate-by-state")
    ?? throw new InvalidOperationException("Cancer-rate fixture could not be loaded.");
var rates = Present(cancer);
Check(rates.Diverging && Near(rates.BaselinePercent, 50), "Rates use a shared mean baseline.");
Check(Near(rates.Bar(rates.Mean).Width, 0), "The mean has no deviation bar.");
Check(rates.Bar(rates.Ranking.Insights.Lowest.Value).Left < 50 && Near(rates.Bar(rates.Ranking.Insights.Highest.Value).Left, 50),
    "Rates extend below and above the mean in opposite directions.");
Check(rates.Tone(cancer.Table!, true) == "ranking-extreme-list--bad", "Higher harmful rates retain the worse-outcome tone.");
Check(!Present(Fixture(new double[] { 1, 2, 3 }, key: "corporate_tax")).Diverging, "Substring 'rate' inside another word does not select rate bars.");

async Task<string> Render(string path, object model)
{
    using var scope = app.Services.CreateScope();
    var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    context.Request.Scheme = "http";
    context.Request.Host = new HostString("localhost");
    var action = new ActionContext(context, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor());
    var engine = scope.ServiceProvider.GetRequiredService<ICompositeViewEngine>();
    var view = engine.GetView(null, path, false);
    if (!view.Success) throw new InvalidOperationException("View not found: " + path);
    using var writer = new StringWriter(CultureInfo.InvariantCulture);
    var data = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
    var temp = new TempDataDictionary(context, scope.ServiceProvider.GetRequiredService<ITempDataProvider>());
    await view.View.RenderAsync(new ViewContext(action, view.View, data, temp, writer, new HtmlHelperOptions()));
    return writer.ToString();
}

var model = new PageDetailViewModel { Content = area };
var html = await Render("/Views/Rankings/_RankingExtremesBlock.cshtml", model);
Check(html.Contains("States with just 1 area code (11)"), "Razor renders the complete tied group.");
Check(html.Contains("11 states tied"), "The highlight card does not select an arbitrary tied state.");
Check(html.Contains("width:2.439%"), "The rendered bottom group bar uses the global scale.");
var bottomHtml = html[(html.IndexOf("id=\"ranking-extremes-lowest\"", StringComparison.Ordinal))..];
Check(Regex.Matches(bottomHtml, "href=\"/states/").Count == 11, "The bottom group renders exactly 11 state links.");
var tableHtml = await Render("/Views/Shared/Components/Tables/_Table.cshtml", model.ToTableSection());
Check(Regex.Matches(tableHtml, ">\\s*T-40\\s*</span>").Count == 11, "The full table renders every tied rank.");
Check(tableHtml.Contains("data-rank=\"40\""), "Machine-readable rank stays numeric for sorting.");
var cardsHtml = await Render("/Views/Shared/Components/Tables/_Cards.cshtml", model.ToTableSection());
Check(Regex.Matches(cardsHtml, ">\\s*T-40\\s*</span>").Count == 11, "Cards share table rank labels.");
var rateHtml = await Render("/Views/Rankings/_RankingExtremesBlock.cshtml", new PageDetailViewModel { Content = cancer });
Check(rateHtml.Contains("Each state has equal weight.") && rateHtml.Contains("ranking-extreme-row__track--diverging"),
    "Rate charts explain and render the state-mean baseline.");
var singleSide = new RankingExtremesPresentation(area, ranking, false, true);
Check(singleSide.Top == null && singleSide.Bottom!.Rows.Count == 11, "Bottom-only mode retains complete ties.");

// Standalone snapshots use the actual Razor partial and the CSS shipped to users.
var output = Path.Combine(root.FullName, "artifacts", "ranking-presentation-checks");
Directory.CreateDirectory(output);
string Document(string body) => "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
    + $"<link rel=\"stylesheet\" href=\"{new Uri(Path.Combine(root.FullName, "wwwroot/css/main.css")).AbsoluteUri}\">"
    + $"<link rel=\"stylesheet\" href=\"{new Uri(Path.Combine(root.FullName, "wwwroot/css/ranking-extremes.css")).AbsoluteUri}\">"
    + "<style>body{background:white;padding:16px}.fixture{width:100%;min-width:0;max-width:720px;margin:auto}</style><main class=\"fixture\">"
    + body.Replace("src=\"/images/", $"src=\"{new Uri(Path.Combine(root.FullName, "wwwroot/images")).AbsoluteUri}/") + "</main></html>";
await File.WriteAllTextAsync(Path.Combine(output, "area-codes.html"), Document(html));
await File.WriteAllTextAsync(Path.Combine(output, "cancer-rate.html"), Document(rateHtml));
Console.WriteLine($"Passed {checks} ranking presentation checks. Snapshots: {output}");
