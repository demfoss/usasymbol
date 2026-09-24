using System.Text.RegularExpressions;

namespace USASymbol.Services;

/// <summary>Derives topical hashtags for a ranking from its slug; an explicit `tags:` list in the YAML wins.</summary>
public static class RankingTopicTags
{
    private const int MaxTags = 4;

    // Terms are matched against hyphen-delimited slug tokens ("-term-"), never raw substrings.
    private static readonly (string Label, string[] Terms)[] Rules =
    [
        ("Landlord & Tenant", ["abandoned-property", "squatters", "eviction", "tenant", "landlord", "security-deposit", "rent-increase", "late-fee", "rental-application", "notice-to-vacate", "rent-control", "adverse-possession"]),
        ("Housing & Property", ["abandoned-property", "squatters", "homestead", "zoning", "home-value", "homeownership", "foreclosure", "hoa", "home-insurance", "average-rent", "moving-cost", "property-tax", "rainwater-harvesting", "noise-ordinance"]),
        ("Driving Laws", ["car-inspections", "car-seat", "dash-cam", "dui-strictness", "front-license-plate", "maximum-speed", "motorcycle-helmet", "radar-detector", "seatbelt", "texting-while-driving", "window-tint", "sunday-car-sales", "self-serve-gas", "jaywalking", "sleeping-in-your-car", "purple-paint"]),
        ("Cars & Roads", ["car", "cars", "driving", "commute", "road-quality", "toll-roads", "rush-hour", "traffic-congestion", "public-transit", "bike-friendliness", "ev-charging", "electric-vehicle", "gas-stations", "states-by-gas-price", "gas-tax", "vehicle-property-tax", "motorcycle-helmet", "dui-fatalities", "fatal-car-accidents", "railroad-miles", "largest-airport"]),
        ("Guns & Weapons", ["gun", "guns", "firearm", "firearms", "ammunition", "magazine-capacity", "knife", "butterfly-knife", "flamethrower", "concealed-carry", "stand-your-ground", "mass-shootings", "school-shootings", "gun-deaths", "gun-ownership", "hunting-license-sales"]),
        ("Alcohol", ["alcohol", "liquor", "dry-states", "drinking-age", "distilling", "happy-hour", "beer", "wine", "breweries", "brewery", "dui-strictness", "dui-fatalities"]),
        ("Drugs & Tobacco", ["marijuana", "drug", "drugs", "meth", "opioid", "thc", "vaping", "cigarette", "overdose", "recreational-drug-testing"]),
        ("Family & Marriage", ["marriage", "marital", "divorce", "cousin", "incest", "alienation-of-affection", "age-of-consent", "singles", "dating", "remarriage", "family-size", "household-size", "childcare", "left-home-alone", "teen-birth", "stay-at-home-parent", "baby-names", "domestic-violence"]),
        ("Privacy & Recording", ["one-party-consent", "dash-cam", "drone", "duty-to-inform"]),
        ("Crime & Safety", ["crime", "murder", "robbery", "robberies", "burglary", "assault", "arson", "theft", "stolen", "police", "sex-offenders", "rape", "shootings", "homicides", "trafficking", "fraud", "scam", "recidivism", "prisons", "incarceration", "executions", "death-penalty", "hate-crimes", "exonerations", "corrupt", "dangerous", "safest", "felony", "civil-asset-forfeiture", "missing-persons", "porch-pirates", "citizens-arrest", "lockpick", "gun-deaths", "meth-use"]),
        ("Taxes", ["tax", "taxes", "529-plans", "tax-refund", "income-tax", "property-tax", "sales-tax", "death-tax", "capital-gains-tax", "gas-tax", "tax-burden", "social-security"]),
        ("Jobs & Wages", ["salary", "minimum-wage", "union-membership", "right-to-work", "occupational-licensing", "unemployment", "median-income", "remote-work", "most-common-job", "largest-employer", "living-wage", "esthetician-license", "employees"]),
        ("Schools & Education", ["school", "schools", "education", "educated", "homeschool", "kindergarten", "teacher", "college", "sat-scores", "act-score", "student-loan", "graduation", "class-size", "banned-books", "bar-exams", "ged", "k12"]),
        ("Health & Healthcare", ["health", "healthcare", "hospitals", "insurance-cost", "mortality", "obesity", "life-expectancy", "uninsured", "doctor", "nursing", "mental-health", "suicide", "flu", "allergies", "ambulance", "assisted-living", "therapy"]),
        ("Weather & Disasters", ["snowfall", "humidity", "hail", "fog", "lightning", "rainy", "hurricane", "tornado", "wildfire", "flood", "earthquake", "windiest", "hottest", "coldest", "driest", "first-snow", "northern-lights", "volcanoes"]),
        ("Voting & Elections", ["voter", "electoral", "red-and-blue", "voting"]),
        ("Military & Veterans", ["military", "veterans", "army", "bases"]),
        ("Restaurants & Fast Food", ["fast-food", "mcdonalds", "burger-king", "chick-fil-a", "chipotle", "culvers", "dairy-queen", "dominos", "dunkin", "dutch-bros", "in-n-out", "jersey-mikes", "kfc", "pizza-hut", "popeyes", "raising-canes", "sonic", "starbucks", "subway", "taco-bell", "waffle-house", "wendys", "whataburger", "wingstop", "restaurants"]),
        ("Store Locations", ["walmart", "target", "costco", "cvs", "walgreens", "dollar-general", "dollar-tree", "home-depot", "lowes", "kroger", "publix", "aldi", "trader-joes", "7-eleven", "sheetz", "wawa", "buc-ees", "banks", "bank-of-america", "chase-bank", "wells-fargo", "locations"]),
        ("Sports Teams", ["nfl", "nba", "mlb", "nhl", "mls", "wnba", "ncaa", "minor-league", "college-football", "college-basketball", "super-bowl", "nascar", "ufc", "sports-team"]),
        ("Outdoors & Recreation", ["fishing", "hunting", "camping", "national-parks", "national-park", "national-forests", "national-monuments", "ski-resorts", "golf", "boating", "public-land", "stargazing", "bear-population", "lighthouses"]),
        ("Regions of the U.S.", ["belt", "states-bordering", "new-england", "midwest", "northeast", "southeast", "southwest", "pacific-northwest", "mid-atlantic", "great-lakes", "appalachian", "deep-south", "wild-west", "four-corners", "landlocked", "south-states", "west-states", "mississippi-river"]),
        ("Population & People", ["population", "median-age", "generation", "migration", "homeless", "average-height", "average-iq", "most-spoken-language", "happiest", "introverted", "largest-city"]),
        ("Cost of Living", ["cost-of-living", "grocery-bill", "utility-bill", "electric-bill", "purchasing-power", "poverty", "median-income", "car-payment", "credit-card-debt", "credit-score", "home-value", "average-rent", "car-insurance", "home-insurance"]),
    ];

    private static readonly Dictionary<string, string> Blurbs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Landlord & Tenant"] = "how state law treats renters and landlords, from security deposits and eviction notices to rent increases and entry rules",
        ["Housing & Property"] ="landlord and tenant rules, property rights, and housing costs that differ from state to state",
        ["Driving Laws"] = "the traffic and vehicle laws that change when you cross a state line, from speed limits to seatbelts and window tint",
        ["Cars & Roads"] = "driving, roads, commuting, and car ownership across the states",
        ["Guns & Weapons"] = "state firearm, ammunition, and weapon laws and the statistics around them",
        ["Alcohol"] = "state alcohol laws and drinking habits, from drinking ages to dry counties and happy hour rules",
        ["Drugs & Tobacco"] = "marijuana, opioid, vaping, and tobacco laws and use rates across the states",
        ["Family & Marriage"] = "marriage, divorce, family, and age-based legal rules across the states",
        ["Privacy & Recording"] = "recording, consent, drone, and privacy rules that vary by state",
        ["Crime & Safety"] = "crime rates, policing, and public safety by state",
        ["Taxes"] = "income, sales, property, and other state tax burdens compared side by side",
        ["Jobs & Wages"] = "pay, employment, and workplace rules across the states",
        ["Schools & Education"] = "school quality, spending, testing, and education rules by state",
        ["Health & Healthcare"] = "health outcomes, healthcare costs, and access to care by state",
        ["Weather & Disasters"] = "climate extremes and natural hazards across the United States",
        ["Sports Teams"] = "professional and college teams, titles, and fan culture by state",
        ["Outdoors & Recreation"] = "parks, public land, fishing, hunting, and outdoor recreation by state",
    };

    private static readonly Regex NonSlug = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public static string Slug(string label) =>
        NonSlug.Replace(label.ToLowerInvariant().Replace(".", "").Replace("'", ""), "-").Trim('-');

    public static string Blurb(string label) =>
        Blurbs.GetValueOrDefault(label, $"{label.ToLowerInvariant()} across the United States");

    public static IReadOnlyList<string> Derive(string slug, IEnumerable<string>? explicitTags = null)
    {
        var explicitList = (explicitTags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxTags).ToList();
        if (explicitList.Count > 0) return explicitList;

        var padded = "-" + NonSlug.Replace(slug.ToLowerInvariant(), "-").Trim('-') + "-";
        return Rules
            .Where(rule => rule.Terms.Any(term => padded.Contains("-" + term + "-", StringComparison.Ordinal)))
            .Select(rule => rule.Label)
            .Take(MaxTags)
            .ToList();
    }

    private static readonly HashSet<string> StopTokens = new(StringComparer.Ordinal)
    {
        "by", "state", "states", "in", "the", "of", "us", "usa", "and", "for", "per", "to", "with", "a", "an",
        "laws", "law", "rate", "rates", "most", "best", "average", "capita", "number", "list", "ranking"
    };

    public static HashSet<string> SlugTokens(string slug) => NonSlug.Replace(slug.ToLowerInvariant(), " ")
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Where(token => token.Length > 2 && !StopTokens.Contains(token))
        .ToHashSet(StringComparer.Ordinal);
}
