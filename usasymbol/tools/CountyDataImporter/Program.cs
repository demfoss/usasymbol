using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.VisualBasic.FileIO;

const string CensusBase = "https://www2.census.gov/programs-surveys/acs/summary_file/2024/table-based-SF/data/5YRData";
const string BlsUrl = "https://downloadt.bls.gov/pub/time.series/la/la.data.64.County";
const string ChrUrl = "https://www.countyhealthrankings.org/sites/default/files/media/document/analytic_data2025_v3.csv";

var options = ImportOptions.Parse(args);
var repoRoot = options.RepoRoot ?? FindRepoRoot(Environment.CurrentDirectory);
var cacheDirectory = Path.Combine(repoRoot, "tools", "CountyDataImporter", "cache");
var outputDirectory = Path.Combine(repoRoot, "Content", "places", "counties");
var runtimePath = Path.Combine(outputDirectory, "county-metrics.json");
var seedPath = Path.Combine(repoRoot, "wwwroot", "maps", "county-data.json");

Directory.CreateDirectory(cacheDirectory);
Directory.CreateDirectory(outputDirectory);

using var client = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(30)
};
client.DefaultRequestHeaders.UserAgent.ParseAdd("USASymbolCountyImporter/1.0");

var sourceFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["acs-population"] = await DownloadAsync(client, $"{CensusBase}/acsdt5y2024-b01003.dat", cacheDirectory, options.Refresh),
    ["acs-education"] = await DownloadAsync(client, $"{CensusBase}/acsdt5y2024-b15003.dat", cacheDirectory, options.Refresh),
    ["acs-income"] = await DownloadAsync(client, $"{CensusBase}/acsdt5y2024-b19013.dat", cacheDirectory, options.Refresh),
    ["acs-rent"] = await DownloadAsync(client, $"{CensusBase}/acsdt5y2024-b25064.dat", cacheDirectory, options.Refresh),
    ["acs-home-value"] = await DownloadAsync(client, $"{CensusBase}/acsdt5y2024-b25077.dat", cacheDirectory, options.Refresh),
    ["bls-laus"] = await DownloadAsync(client, BlsUrl, cacheDirectory, options.Refresh),
    ["chr"] = await DownloadAsync(client, ChrUrl, cacheDirectory, options.Refresh)
};

var counties = LoadSeed(seedPath);
Console.WriteLine($"Seed county records: {counties.Count:N0}");

ReadAcsTable(sourceFiles["acs-population"], counties, (record, fields) =>
{
    record.Population = ParseMappedNumber(fields, "B01003_E001");
});
ReadAcsTable(sourceFiles["acs-income"], counties, (record, fields) =>
{
    record.MedianHouseholdIncome = ParseMappedNumber(fields, "B19013_E001");
});
ReadAcsTable(sourceFiles["acs-rent"], counties, (record, fields) =>
{
    record.MedianGrossRent = ParseMappedNumber(fields, "B25064_E001");
});
ReadAcsTable(sourceFiles["acs-home-value"], counties, (record, fields) =>
{
    record.MedianHomeValue = ParseMappedNumber(fields, "B25077_E001");
});
ReadAcsTable(sourceFiles["acs-education"], counties, (record, fields) =>
{
    var denominator = ParseMappedNumber(fields, "B15003_E001");
    var numerator = new[]
        {
            "B15003_E022", "B15003_E023", "B15003_E024", "B15003_E025"
        }
        .Select(key => ParseMappedNumber(fields, key))
        .Where(value => value.HasValue)
        .Sum(value => value!.Value);

    if (denominator > 0)
        record.CollegeEducatedRate = 100d * numerator / denominator.Value;
});

var lausYear = ReadLaus(sourceFiles["bls-laus"], counties);
ReadCountyHealthRankings(sourceFiles["chr"], counties);

foreach (var stateGroup in counties.Values.GroupBy(record => record.StateFips))
{
    var largest = stateGroup
        .OrderByDescending(record => record.Population ?? 0)
        .ThenBy(record => record.Fips, StringComparer.Ordinal)
        .FirstOrDefault();
    foreach (var county in stateGroup)
        county.Published = county.Population >= 100_000 || ReferenceEquals(county, largest);
}

var sources = BuildSources(lausYear);
var sourceFingerprint = BuildSourceFingerprint(sourceFiles.Values);
var generatedOn = ResolveGeneratedOn(runtimePath, sourceFingerprint);
var jsonCounties = counties.Values
    .OrderBy(record => record.Fips, StringComparer.Ordinal)
    .ToDictionary(
        record => record.Fips,
        record => BuildRuntimeCounty(record),
        StringComparer.Ordinal);
var runtime = new RuntimeCountyData
{
    SchemaVersion = 1,
    GeneratedOn = generatedOn,
    SourceFingerprint = sourceFingerprint,
    Sources = sources,
    Counties = jsonCounties
};
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
await File.WriteAllTextAsync(runtimePath, JsonSerializer.Serialize(runtime, jsonOptions), new UTF8Encoding(false));

var written = 0;
var writtenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var county in counties.Values.OrderBy(record => record.Fips, StringComparer.Ordinal))
{
    var state = StateCatalog.ByFips[county.StateFips];
    var stateDirectory = Path.Combine(outputDirectory, state.Slug);
    Directory.CreateDirectory(stateDirectory);
    var yamlPath = Path.Combine(stateDirectory, $"{county.Slug}.yaml");
    await File.WriteAllTextAsync(
        yamlPath,
        BuildYaml(county, state, sources, generatedOn),
        new UTF8Encoding(false));
    writtenPaths.Add(Path.GetFullPath(yamlPath));
    written++;
}

var removedStaleFiles = 0;
var outputRoot = Path.GetFullPath(outputDirectory) + Path.DirectorySeparatorChar;
foreach (var yamlPath in Directory.EnumerateFiles(outputDirectory, "*.yaml", System.IO.SearchOption.AllDirectories))
{
    var resolvedPath = Path.GetFullPath(yamlPath);
    if (writtenPaths.Contains(resolvedPath) ||
        !resolvedPath.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    using var reader = new StreamReader(resolvedPath);
    if (!string.Equals(reader.ReadLine(), "schemaVersion: 1", StringComparison.Ordinal))
        continue;
    reader.Close();
    File.Delete(resolvedPath);
    removedStaleFiles++;
}

var coverage = new Dictionary<string, int>
{
    ["population"] = counties.Values.Count(x => x.Population.HasValue),
    ["income"] = counties.Values.Count(x => x.MedianHouseholdIncome.HasValue),
    ["homeValue"] = counties.Values.Count(x => x.MedianHomeValue.HasValue),
    ["rent"] = counties.Values.Count(x => x.MedianGrossRent.HasValue),
    ["education"] = counties.Values.Count(x => x.CollegeEducatedRate.HasValue),
    ["unemployment"] = counties.Values.Count(x => x.UnemploymentRate.HasValue),
    ["lifeExpectancy"] = counties.Values.Count(x => x.LifeExpectancy.HasValue)
};

Console.WriteLine($"Generated YAML files: {written:N0}");
Console.WriteLine($"Removed stale generated YAML files: {removedStaleFiles:N0}");
Console.WriteLine($"Published county profiles: {counties.Values.Count(x => x.Published):N0}");
Console.WriteLine($"LAUS annual period: {lausYear}");
foreach (var item in coverage)
    Console.WriteLine($"{item.Key}: {item.Value:N0}/{counties.Count:N0}");
Console.WriteLine($"Runtime index: {runtimePath}");

static async Task<string> DownloadAsync(
    HttpClient client,
    string url,
    string cacheDirectory,
    bool refresh)
{
    var fileName = new Uri(url).Segments.Last();
    var path = Path.Combine(cacheDirectory, fileName);
    var temporaryPath = $"{path}.download";
    if (!refresh && File.Exists(path) && new FileInfo(path).Length > 0)
    {
        if (File.Exists(temporaryPath))
            File.Delete(temporaryPath);
        Console.WriteLine($"Using cached {fileName}");
        return path;
    }

    Console.WriteLine($"Downloading {fileName}...");
    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();
    await using (var input = await response.Content.ReadAsStreamAsync())
    await using (var output = new FileStream(
                     temporaryPath,
                     FileMode.Create,
                     FileAccess.Write,
                     FileShare.None,
                     1024 * 1024,
                     FileOptions.Asynchronous | FileOptions.SequentialScan))
    {
        await input.CopyToAsync(output);
    }

    File.Move(temporaryPath, path, true);
    Console.WriteLine($"Saved {fileName} ({new FileInfo(path).Length / 1_000_000d:0.0} MB)");
    return path;
}

static Dictionary<string, CountyRecord> LoadSeed(string seedPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(seedPath));
    var records = new Dictionary<string, CountyRecord>(StringComparer.Ordinal);

    foreach (var property in document.RootElement.EnumerateObject())
    {
        var fips = property.Name;
        if (fips.Length != 5 || !StateCatalog.ByFips.TryGetValue(fips[..2], out var state))
            continue;

        var rawName = property.Value.TryGetProperty("n", out var nameElement)
            ? nameElement.GetString() ?? string.Empty
            : string.Empty;
        var seedPopulation = property.Value.TryGetProperty("p", out var populationElement) &&
                             populationElement.TryGetInt64(out var population)
            ? population
            : (long?)null;
        var name = BuildDisplayName(state.Abbreviation, fips, rawName);
        records[fips] = new CountyRecord
        {
            Fips = fips,
            StateFips = fips[..2],
            Name = name,
            Slug = Slugify(name),
            Population = seedPopulation
        };
    }

    return records;
}

static void ReadAcsTable(
    string path,
    IReadOnlyDictionary<string, CountyRecord> counties,
    Action<CountyRecord, IReadOnlyDictionary<string, string>> apply)
{
    using var reader = new StreamReader(path);
    var headerLine = reader.ReadLine() ?? throw new InvalidDataException($"Missing ACS header: {path}");
    var headers = headerLine.Split('|');
    var values = new Dictionary<string, string>(StringComparer.Ordinal);
    var matched = 0;

    while (reader.ReadLine() is { } line)
    {
        if (!line.StartsWith("0500000US", StringComparison.Ordinal))
            continue;
        var fields = line.Split('|');
        if (fields.Length != headers.Length)
            continue;
        var fips = fields[0][9..];
        if (!counties.TryGetValue(fips, out var county))
            continue;

        values.Clear();
        for (var index = 1; index < headers.Length; index++)
            values[headers[index]] = fields[index];
        apply(county, values);
        matched++;
    }

    Console.WriteLine($"{Path.GetFileName(path)}: matched {matched:N0} counties");
}

static int ReadLaus(string path, IReadOnlyDictionary<string, CountyRecord> counties)
{
    var latestYear = 0;
    using var reader = new StreamReader(path);
    _ = reader.ReadLine();
    while (reader.ReadLine() is { } line)
    {
        var fields = line.Split('\t');
        if (fields.Length < 4)
            continue;
        var seriesId = fields[0].Trim();
        if (seriesId.Length != 20 ||
            !seriesId.StartsWith("LAUCN", StringComparison.Ordinal) ||
            !string.Equals(fields[2].Trim(), "M13", StringComparison.Ordinal))
        {
            continue;
        }

        var fips = seriesId.Substring(5, 5);
        if (!counties.TryGetValue(fips, out var county) ||
            !int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) ||
            !double.TryParse(fields[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            continue;
        }

        var measure = seriesId[^2..];
        if (year < county.LausYear)
            continue;
        if (year > county.LausYear)
        {
            county.LausYear = year;
            county.UnemploymentRate = null;
            county.Unemployed = null;
            county.Employment = null;
            county.LaborForce = null;
        }

        switch (measure)
        {
            case "03": county.UnemploymentRate = value; break;
            case "04": county.Unemployed = value; break;
            case "05": county.Employment = value; break;
            case "06": county.LaborForce = value; break;
        }
        latestYear = Math.Max(latestYear, year);
    }

    Console.WriteLine($"{Path.GetFileName(path)}: latest annual period {latestYear}");
    return latestYear;
}

static void ReadCountyHealthRankings(
    string path,
    IReadOnlyDictionary<string, CountyRecord> counties)
{
    using var parser = new TextFieldParser(path)
    {
        TextFieldType = FieldType.Delimited,
        HasFieldsEnclosedInQuotes = true,
        TrimWhiteSpace = false
    };
    parser.SetDelimiters(",");
    _ = parser.ReadFields();
    var headers = parser.ReadFields() ?? throw new InvalidDataException("Missing CHR machine header.");
    var indexes = headers
        .Select((name, index) => (name, index))
        .ToDictionary(item => item.name, item => item.index, StringComparer.OrdinalIgnoreCase);
    var matched = 0;

    while (!parser.EndOfData)
    {
        var fields = parser.ReadFields();
        if (fields is null)
            continue;
        var fips = Field(fields, indexes, "fipscode");
        if (fips is null || !counties.TryGetValue(fips, out var county))
            continue;

        county.PoorFairHealthRate = AsPercent(ParseCsvNumber(fields, indexes, "v002_rawvalue"));
        county.PrimaryCareRatio = ParseCsvNumber(fields, indexes, "v004_other_data_1");
        county.MentalHealthProviderRatio = ParseCsvNumber(fields, indexes, "v062_other_data_1");
        county.UninsuredRate = AsPercent(ParseCsvNumber(fields, indexes, "v085_rawvalue"));
        county.LifeExpectancy = ParseCsvNumber(fields, indexes, "v147_rawvalue");
        matched++;
    }

    Console.WriteLine($"{Path.GetFileName(path)}: matched {matched:N0} counties");
}

static double? ParseMappedNumber(IReadOnlyDictionary<string, string> values, string key) =>
    values.TryGetValue(key, out var value) ? ParseNumericValue(value) : null;

static double? ParseCsvNumber(string[] fields, IReadOnlyDictionary<string, int> indexes, string key)
{
    if (!indexes.TryGetValue(key, out var index) || index >= fields.Length)
        return null;
    return ParseNumericValue(fields[index]);
}

static double? ParseNumericValue(string? value)
{
    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
        double.IsNaN(number) ||
        double.IsInfinity(number) ||
        number <= -100_000_000)
    {
        return null;
    }
    return number;
}

static string? Field(string[] fields, IReadOnlyDictionary<string, int> indexes, string key) =>
    indexes.TryGetValue(key, out var index) && index < fields.Length ? fields[index] : null;

static double? AsPercent(double? ratio) => ratio.HasValue ? ratio.Value * 100d : null;

static RuntimeCounty BuildRuntimeCounty(CountyRecord county) => new()
{
    ParentFips = county.StateFips,
    Name = county.Name,
    Slug = county.Slug,
    Published = county.Published,
    Metrics = BuildMetrics(county)
};

static Dictionary<string, RuntimeMetric> BuildMetrics(CountyRecord county)
{
    var metrics = new Dictionary<string, RuntimeMetric>(StringComparer.Ordinal);
    Add(metrics, "population", county.Population, "residents", 0, "census_acs");
    Add(metrics, "medianHouseholdIncome", county.MedianHouseholdIncome, "usd", 1, "census_acs");
    Add(metrics, "medianHomeValue", county.MedianHomeValue, "usd", -1, "census_acs");
    Add(metrics, "medianGrossRent", county.MedianGrossRent, "usd_month", -1, "census_acs");
    Add(metrics, "collegeEducatedRate", county.CollegeEducatedRate, "percent", 1, "census_acs");
    Add(metrics, "unemploymentRate", county.UnemploymentRate, "percent", -1, "bls_laus");
    Add(metrics, "employment", county.Employment, "people", 0, "bls_laus");
    Add(metrics, "laborForce", county.LaborForce, "people", 0, "bls_laus");
    Add(metrics, "lifeExpectancy", county.LifeExpectancy, "years", 1, "county_health_rankings");
    Add(metrics, "poorFairHealthRate", county.PoorFairHealthRate, "percent", -1, "county_health_rankings");
    Add(metrics, "uninsuredRate", county.UninsuredRate, "percent", -1, "county_health_rankings");
    Add(metrics, "primaryCareRatio", county.PrimaryCareRatio, "residents_per_provider", -1, "county_health_rankings");
    Add(metrics, "mentalHealthProviderRatio", county.MentalHealthProviderRatio, "residents_per_provider", -1, "county_health_rankings");
    return metrics;
}

static void Add(
    IDictionary<string, RuntimeMetric> metrics,
    string key,
    double? value,
    string unit,
    int direction,
    string sourceId)
{
    if (!value.HasValue)
        return;
    metrics[key] = new RuntimeMetric
    {
        Raw = value.Value,
        Unit = unit,
        Direction = direction,
        SourceId = sourceId
    };
}

static Dictionary<string, RuntimeSource> BuildSources(int lausYear) => new(StringComparer.Ordinal)
{
    ["census_acs"] = new RuntimeSource
    {
        Name = "U.S. Census Bureau — American Community Survey",
        Url = "https://www.census.gov/programs-surveys/acs/data/summary-file.html",
        Period = "2020–2024 ACS 5-year",
        Release = "2024"
    },
    ["bls_laus"] = new RuntimeSource
    {
        Name = "U.S. Bureau of Labor Statistics — Local Area Unemployment Statistics",
        Url = "https://www.bls.gov/lau/data-overview.htm",
        Period = $"{lausYear} annual average",
        Release = lausYear.ToString(CultureInfo.InvariantCulture)
    },
    ["county_health_rankings"] = new RuntimeSource
    {
        Name = "County Health Rankings & Roadmaps",
        Url = "https://www.countyhealthrankings.org/health-data/methodology-and-sources/data-documentation",
        Period = "2025 annual release",
        Release = "2025"
    }
};

static string BuildYaml(
    CountyRecord county,
    StateDefinition state,
    IReadOnlyDictionary<string, RuntimeSource> sources,
    string generatedOn)
{
    var builder = new StringBuilder();
    builder.AppendLine("schemaVersion: 1");
    builder.AppendLine($"fips: \"{county.Fips}\"");
    builder.AppendLine($"parentFips: \"{county.StateFips}\"");
    builder.AppendLine($"state: {state.Slug}");
    builder.AppendLine($"name: {YamlQuote(county.Name)}");
    builder.AppendLine($"slug: {county.Slug}");
    builder.AppendLine($"published: {county.Published.ToString().ToLowerInvariant()}");
    builder.AppendLine($"updatedOn: \"{generatedOn}\"");
    builder.AppendLine("sources:");
    foreach (var source in sources)
    {
        builder.AppendLine($"  {source.Key}:");
        builder.AppendLine($"    name: {YamlQuote(source.Value.Name)}");
        builder.AppendLine($"    url: {YamlQuote(source.Value.Url)}");
        builder.AppendLine($"    period: {YamlQuote(source.Value.Period)}");
    }
    builder.AppendLine("metrics:");
    foreach (var metric in BuildMetrics(county))
    {
        builder.AppendLine($"  {metric.Key}:");
        builder.AppendLine($"    raw: {metric.Value.Raw.ToString("0.##########", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"    unit: {metric.Value.Unit}");
        builder.AppendLine($"    direction: {metric.Value.Direction}");
        builder.AppendLine($"    sourceId: {metric.Value.SourceId}");
    }
    return builder.ToString();
}

static string YamlQuote(string value) =>
    $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

static string BuildDisplayName(string stateAbbreviation, string fips, string? rawName)
{
    var name = string.IsNullOrWhiteSpace(rawName) ? $"County {fips[2..]}" : rawName.Trim();
    if (name.EndsWith("County", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith("Parish", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Planning Region", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith("Borough", StringComparison.OrdinalIgnoreCase) ||
        name.EndsWith("Census Area", StringComparison.OrdinalIgnoreCase))
    {
        return name;
    }

    if (string.Equals(stateAbbreviation, "LA", StringComparison.OrdinalIgnoreCase))
        return $"{name} Parish";
    if (string.Equals(stateAbbreviation, "AK", StringComparison.OrdinalIgnoreCase))
        return name.EndsWith("City and", StringComparison.OrdinalIgnoreCase) ? $"{name} Borough" : name;
    if (int.TryParse(fips[2..], out var countyCode) && countyCode >= 510)
        return $"{name} city";
    return $"{name} County";
}

static string Slugify(string value)
{
    var normalized = value.Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(normalized.Length);
    foreach (var character in normalized)
    {
        if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            builder.Append(character);
    }
    return Regex.Replace(builder.ToString().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}

static string FindRepoRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "usasymbol.csproj")))
            return directory.FullName;
        directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("Could not find usasymbol.csproj.");
}

static string BuildSourceFingerprint(IEnumerable<string> paths)
{
    using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    foreach (var path in paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
    {
        var nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(path));
        aggregate.AppendData(nameBytes);
        using var stream = File.OpenRead(path);
        var fileHash = SHA256.HashData(stream);
        aggregate.AppendData(fileHash);
    }
    return Convert.ToHexString(aggregate.GetHashAndReset()).ToLowerInvariant();
}

static string ResolveGeneratedOn(string runtimePath, string sourceFingerprint)
{
    if (File.Exists(runtimePath))
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(runtimePath));
            var root = document.RootElement;
            if (root.TryGetProperty("sourceFingerprint", out var fingerprintElement) &&
                string.Equals(fingerprintElement.GetString(), sourceFingerprint, StringComparison.OrdinalIgnoreCase) &&
                root.TryGetProperty("generatedOn", out var generatedElement) &&
                !string.IsNullOrWhiteSpace(generatedElement.GetString()))
            {
                return generatedElement.GetString()!;
            }
        }
        catch (JsonException)
        {
            // A malformed old runtime file should not block a clean regeneration.
        }
    }
    return DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

sealed record StateDefinition(string Fips, string Abbreviation, string Slug);

static class StateCatalog
{
    public static readonly IReadOnlyDictionary<string, StateDefinition> ByFips =
        new[]
        {
            ("01","AL","alabama"),("02","AK","alaska"),("04","AZ","arizona"),("05","AR","arkansas"),
            ("06","CA","california"),("08","CO","colorado"),("09","CT","connecticut"),("10","DE","delaware"),
            ("12","FL","florida"),("13","GA","georgia"),("15","HI","hawaii"),("16","ID","idaho"),
            ("17","IL","illinois"),("18","IN","indiana"),("19","IA","iowa"),("20","KS","kansas"),
            ("21","KY","kentucky"),("22","LA","louisiana"),("23","ME","maine"),("24","MD","maryland"),
            ("25","MA","massachusetts"),("26","MI","michigan"),("27","MN","minnesota"),("28","MS","mississippi"),
            ("29","MO","missouri"),("30","MT","montana"),("31","NE","nebraska"),("32","NV","nevada"),
            ("33","NH","new-hampshire"),("34","NJ","new-jersey"),("35","NM","new-mexico"),("36","NY","new-york"),
            ("37","NC","north-carolina"),("38","ND","north-dakota"),("39","OH","ohio"),("40","OK","oklahoma"),
            ("41","OR","oregon"),("42","PA","pennsylvania"),("44","RI","rhode-island"),("45","SC","south-carolina"),
            ("46","SD","south-dakota"),("47","TN","tennessee"),("48","TX","texas"),("49","UT","utah"),
            ("50","VT","vermont"),("51","VA","virginia"),("53","WA","washington"),("54","WV","west-virginia"),
            ("55","WI","wisconsin"),("56","WY","wyoming")
        }
        .ToDictionary(
            item => item.Item1,
            item => new StateDefinition(item.Item1, item.Item2, item.Item3),
            StringComparer.Ordinal);
}

sealed class CountyRecord
{
    public string Fips { get; init; } = string.Empty;
    public string StateFips { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool Published { get; set; }
    public double? Population { get; set; }
    public double? MedianHouseholdIncome { get; set; }
    public double? MedianHomeValue { get; set; }
    public double? MedianGrossRent { get; set; }
    public double? CollegeEducatedRate { get; set; }
    public int LausYear { get; set; }
    public double? UnemploymentRate { get; set; }
    public double? Unemployed { get; set; }
    public double? Employment { get; set; }
    public double? LaborForce { get; set; }
    public double? LifeExpectancy { get; set; }
    public double? PoorFairHealthRate { get; set; }
    public double? UninsuredRate { get; set; }
    public double? PrimaryCareRatio { get; set; }
    public double? MentalHealthProviderRatio { get; set; }
}

sealed class RuntimeCountyData
{
    public int SchemaVersion { get; init; }
    public string GeneratedOn { get; init; } = string.Empty;
    public string SourceFingerprint { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, RuntimeSource> Sources { get; init; } =
        new Dictionary<string, RuntimeSource>();
    public IReadOnlyDictionary<string, RuntimeCounty> Counties { get; init; } =
        new Dictionary<string, RuntimeCounty>();
}

sealed class RuntimeSource
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Period { get; init; } = string.Empty;
    public string Release { get; init; } = string.Empty;
}

sealed class RuntimeCounty
{
    public string ParentFips { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool Published { get; init; }
    public IReadOnlyDictionary<string, RuntimeMetric> Metrics { get; init; } =
        new Dictionary<string, RuntimeMetric>();
}

sealed class RuntimeMetric
{
    public double Raw { get; init; }
    public string Unit { get; init; } = string.Empty;
    public int Direction { get; init; }
    public string SourceId { get; init; } = string.Empty;
}

sealed class ImportOptions
{
    public string? RepoRoot { get; init; }
    public bool Refresh { get; init; }

    public static ImportOptions Parse(string[] arguments)
    {
        string? repoRoot = null;
        var refresh = false;
        for (var index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], "--refresh", StringComparison.OrdinalIgnoreCase))
                refresh = true;
            else if (string.Equals(arguments[index], "--repo", StringComparison.OrdinalIgnoreCase) &&
                     index + 1 < arguments.Length)
                repoRoot = Path.GetFullPath(arguments[++index]);
        }
        return new ImportOptions { RepoRoot = repoRoot, Refresh = refresh };
    }
}
