using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Usasymbol.Helpers;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

var root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
var assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition) throw new InvalidOperationException(message);
}

PageDetailViewModel Fixture(object[] values, string? sort = "desc", RankingMetric? metric = null, string format = "N0")
{
    var states = new[] { ("California", "california"), ("Texas", "texas"), ("Hawaii", "hawaii"), ("Alaska", "alaska"), ("Florida", "florida") };
    return new PageDetailViewModel
    {
        Content = new PageContent
        {
            Tables = new()
            {
                new PageTable
                {
                    DefaultColumn = "value", SortDefault = sort, Metric = metric,
                    Columns = new() { new TableColumn { Key = "value", Label = "Test measure", Type = "number", Format = format } },
                    Rows = values.Select((value, i) => new TableRow
                    {
                        Data = new() { ["state"] = states[i].Item1, ["state_slug"] = states[i].Item2, ["value"] = value }
                    }).ToList()
                }
            }
        }
    };
}

RankingTakeaways Build(PageDetailViewModel model) => RankingTakeawaysBuilder.Build(model)
    ?? throw new InvalidOperationException("Expected numeric state takeaways.");

var tied = Build(Fixture(new object[] { 10, 10, 1, 1, 5 }));
Check(tied.Options.Single(o => o.Slug == "california").Rank == 1, "First-place tie rank");
Check(tied.Options.Single(o => o.Slug == "florida").Rank == 3, "Competition ranks must skip tied positions");
Check(tied.Options.Single(o => o.Slug == "hawaii").Context == "Tied for last with 1 other state.", "Last-place tie copy");

var ascending = Build(Fixture(new object[] { 1, 2, 2, 7 }, "asc"));
Check(ascending.Leader.Slug == "california", "Ascending leader must have the lowest value");
Check(ascending.Options.Single(o => o.Slug == "texas").Rank == 2, "Ascending competition rank");

var inferred = Fixture(new object[] { 7, 2, 2, 1 }, null);
foreach (var row in inferred.Content.Table!.Rows) row.Data["rank"] = row.GetNumber("value") == 1 ? 1 : 2;
Check(Build(inferred).Leader.Slug == "alaska", "Honor source ranks when sort direction is absent");

var negative = Build(Fixture(new object[] { -5, -2, 0, 4 }));
Check(negative.Options.All(o => double.IsFinite(o.Position) && o.Position is >= 0 and <= 100), "Negative-value scale");
var equal = Build(Fixture(new object[] { 5, 5, 5, 5 }));
Check(!equal.HasSpread && equal.Options.All(o => o.Context.StartsWith("All 4 states")), "All-equal ranking");
Check(Build(Fixture(new object[] { 1, 2, 3, 4 })).TypicalStatement.Contains("2.5"), "Even-sample median must retain its fraction");

var missing = Build(Fixture(new object[] { 1, 2, 3, "—" }));
Check(missing.Options.Count == 3 && missing.Summary.Contains("3 states with numeric data"), "Missing-value coverage");
var withDc = Fixture(new object[] { 1, 2, 3, 4, 9999 });
withDc.Content.Table!.Rows[4].Data["state_slug"] = "district-of-columbia";
Check(Build(withDc).TypicalStatement.Contains("2.5"), "DC must not skew the typical state");
var duplicate = Fixture(new object[] { 1, 2, 3, 4 });
duplicate.Content.Table!.Rows[3].Data["state_slug"] = "california";
Check(RankingTakeawaysBuilder.Build(duplicate) == null, "Repeated-state item tables must use the existing fallback");

var rate = Build(Fixture(new object[] { "10%", "5%", "2%", "1%" }));
Check(rate.Options.Single(o => o.Slug == "california").Statement.Contains("10%"), "Preserve raw percentage symbols");
Check(rate.Facts.All(f => !f.Text.Contains("combined total") && !f.Text.Contains("Per person")), "Never aggregate rates by default");
var salary = Build(Fixture(new object[] { 1000, 2000, 3000, 4000 }, format: "C0"));
Check(salary.Options.All(o => o.Statement.Contains('$')), "Preserve currency formatting");

var metric = new RankingMetric { UnitSingular = "area code", UnitPlural = "area codes", Additive = true, PopulationKey = "population", PreviousValueKey = "previous", PreviousYear = 2025 };
var enriched = Fixture(new object[] { 5, 4, 3, 2 }, metric: metric);
for (var i = 0; i < enriched.Content.Table!.Rows.Count; i++)
{
    enriched.Content.Table.Rows[i].Data["population"] = i == 1 ? 100 : 1000;
    enriched.Content.Table.Rows[i].Data["previous"] = i == 0 ? 0 : enriched.Content.Table.Rows[i].Data["value"];
}
var enrichedResult = Build(enriched);
Check(enrichedResult.Facts.Any(f => f.Text.Contains("Per person, Texas") && f.Text.Contains("25 residents")), "Population-normalized insight");
Check(enrichedResult.Facts.Any(f => f.Text.Contains("since 2025")), "Prior-year insight");
enriched.Content.Table.Rows[0].Data.Remove("population");
enriched.Content.Table.Rows[0].Data.Remove("previous");
Check(Build(enriched).Facts.All(f => !f.Text.Contains("Per person") && !f.Text.Contains("since 2025")), "Incomplete denominators and history must suppress insights");

using var cache = new MemoryCache(new MemoryCacheOptions());
var env = new CheckEnvironment { ContentRootPath = root };
var service = new PageContentService(NullLogger<PageContentService>.Instance, env, Path.Combine(root, "Content", "rankings"), cache);
var areaCodes = await service.GetContentAsync("geography", "area-codes-by-state");
Check(areaCodes?.Table?.Metric?.UnitSingular == "area code", "YAML metric metadata must reach the builder");
var area = Build(new PageDetailViewModel { Content = areaCodes! });
Check(area.Summary == "The middle half of states has between two and nine area codes.", "Area-code quartiles must come from actual data");
Check(area.TypicalStatement == "The typical state has five area codes.", "Area-code median");
Check(area.Options.Single(o => o.Slug == "hawaii").Statement == "Hawaii has 1 area code", "Singular unit in selected-state result");
Check(area.Options.Single(o => o.Slug == "hawaii").Context == "Tied for last with 10 other states.", "Hawaii tie");
Check(area.Facts.Single(f => f.States.Count > 0).States.Count == 11, "Full tied-state disclosure");
Check(area.Facts.All(f => !f.Text.Contains("Per person") && !f.Text.Contains("Newest")), "Do not invent absent facts");

var loaded = 0;
var numeric = 0;
var fallback = 0;
var unreadable = new List<string>();
foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "Content", "rankings"), "*", SearchOption.AllDirectories)
    .Where(path => Path.GetExtension(path) is ".yml" or ".yaml"))
{
    var content = await service.GetContentAsync(Path.GetFileName(Path.GetDirectoryName(path))!, Path.GetFileNameWithoutExtension(path));
    if (content == null) { unreadable.Add(Path.GetRelativePath(root, path)); continue; }
    loaded++;
    var result = RankingTakeawaysBuilder.Build(new PageDetailViewModel { Content = content });
    if (result == null) { fallback++; continue; }
    numeric++;
    Check(result.Leader.Slug != result.Alternative.Slug, $"Self-comparison: {path}");
    Check(result.Options.All(o => o.Rank >= 1 && o.Rank <= result.Options.Count && double.IsFinite(o.Position) && o.Position is >= 0 and <= 100), $"Invalid rank or scale: {path}");
    Check(!result.Summary.Contains(';') && !result.Summary.Contains("median state"), $"Template copy regression: {path}");
    Check(result.Facts.All(f => !f.Text.Contains("combined total") || content.Table!.Metric?.Additive == true), $"Unsafe aggregation: {path}");
}
Console.WriteLine($"PASS: {assertions} assertions. Corpus: {loaded} rankings loaded; {numeric} numeric state blocks; {fallback} existing fallbacks.");
foreach (var path in unreadable) Console.WriteLine($"Existing loader could not read: {path}");
Check(unreadable.Count == 0, "Some ranking content could not be loaded.");

sealed class CheckEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "RankingTakeawaysChecks";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
