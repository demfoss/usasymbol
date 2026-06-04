using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Usasymbol.Views.Shared.Components
{
    public class StateHubMapViewModel
    {
        public string StateName     { get; set; } = string.Empty;
        public string Slug          { get; set; } = string.Empty;
        public string Abbreviation  { get; set; } = string.Empty;
        public string Capital       { get; set; } = string.Empty;
        public string? Region       { get; set; }
        public int    Fips          { get; set; }
        public double CapitalLat    { get; set; }
        public double CapitalLon    { get; set; }
        public string StateSlugsJson  { get; set; } = "{}";
        public string FipsToAbbrJson  { get; set; } = "{}";
    }

    public class StateHubMapViewComponent : ViewComponent
    {
        private static readonly Dictionary<string, int> FipsCodes = new()
        {
            ["AL"]=1,  ["AK"]=2,  ["AZ"]=4,  ["AR"]=5,  ["CA"]=6,  ["CO"]=8,  ["CT"]=9,
            ["DE"]=10, ["FL"]=12, ["GA"]=13, ["HI"]=15, ["ID"]=16, ["IL"]=17, ["IN"]=18,
            ["IA"]=19, ["KS"]=20, ["KY"]=21, ["LA"]=22, ["ME"]=23, ["MD"]=24, ["MA"]=25,
            ["MI"]=26, ["MN"]=27, ["MS"]=28, ["MO"]=29, ["MT"]=30, ["NE"]=31, ["NV"]=32,
            ["NH"]=33, ["NJ"]=34, ["NM"]=35, ["NY"]=36, ["NC"]=37, ["ND"]=38, ["OH"]=39,
            ["OK"]=40, ["OR"]=41, ["PA"]=42, ["RI"]=44, ["SC"]=45, ["SD"]=46, ["TN"]=47,
            ["TX"]=48, ["UT"]=49, ["VT"]=50, ["VA"]=51, ["WA"]=53, ["WV"]=54, ["WI"]=55,
            ["WY"]=56,
        };

        private static readonly Dictionary<int, string> StateSlugs = new()
        {
            [1]="alabama",       [2]="alaska",         [4]="arizona",       [5]="arkansas",
            [6]="california",    [8]="colorado",       [9]="connecticut",   [10]="delaware",
            [12]="florida",      [13]="georgia",       [15]="hawaii",       [16]="idaho",
            [17]="illinois",     [18]="indiana",       [19]="iowa",         [20]="kansas",
            [21]="kentucky",     [22]="louisiana",     [23]="maine",        [24]="maryland",
            [25]="massachusetts",[26]="michigan",      [27]="minnesota",    [28]="mississippi",
            [29]="missouri",     [30]="montana",       [31]="nebraska",     [32]="nevada",
            [33]="new-hampshire",[34]="new-jersey",    [35]="new-mexico",   [36]="new-york",
            [37]="north-carolina",[38]="north-dakota", [39]="ohio",         [40]="oklahoma",
            [41]="oregon",       [42]="pennsylvania",  [44]="rhode-island", [45]="south-carolina",
            [46]="south-dakota", [47]="tennessee",     [48]="texas",        [49]="utah",
            [50]="vermont",      [51]="virginia",      [53]="washington",   [54]="west-virginia",
            [55]="wisconsin",    [56]="wyoming",
        };

        private static readonly Dictionary<string, (double Lat, double Lon)> CapitalCoords = new()
        {
            ["AL"] = (32.361, -86.279),  // Montgomery
            ["AK"] = (58.301, -134.420), // Juneau
            ["AZ"] = (33.448, -112.074), // Phoenix
            ["AR"] = (34.736, -92.331),  // Little Rock
            ["CA"] = (38.556, -121.469), // Sacramento
            ["CO"] = (39.714, -104.984), // Denver
            ["CT"] = (41.763, -72.685),  // Hartford
            ["DE"] = (39.158, -75.524),  // Dover
            ["FL"] = (30.455, -84.253),  // Tallahassee
            ["GA"] = (33.755, -84.390),  // Atlanta
            ["HI"] = (21.305, -157.858), // Honolulu
            ["ID"] = (43.614, -116.202), // Boise
            ["IL"] = (39.798, -89.655),  // Springfield
            ["IN"] = (39.768, -86.158),  // Indianapolis
            ["IA"] = (41.591, -93.604),  // Des Moines
            ["KS"] = (39.056, -95.689),  // Topeka
            ["KY"] = (38.197, -84.861),  // Frankfort
            ["LA"] = (30.458, -91.140),  // Baton Rouge
            ["ME"] = (44.325, -69.731),  // Augusta
            ["MD"] = (38.978, -76.490),  // Annapolis
            ["MA"] = (42.358, -71.064),  // Boston
            ["MI"] = (42.732, -84.556),  // Lansing
            ["MN"] = (44.950, -93.094),  // Saint Paul
            ["MS"] = (32.322, -90.207),  // Jackson
            ["MO"] = (38.572, -92.189),  // Jefferson City
            ["MT"] = (46.596, -112.027), // Helena
            ["NE"] = (40.809, -96.680),  // Lincoln
            ["NV"] = (39.160, -119.754), // Carson City
            ["NH"] = (43.206, -71.538),  // Concord
            ["NJ"] = (40.221, -74.756),  // Trenton
            ["NM"] = (35.667, -105.964), // Santa Fe
            ["NY"] = (42.659, -73.781),  // Albany
            ["NC"] = (35.771, -78.638),  // Raleigh
            ["ND"] = (46.813, -100.779), // Bismarck
            ["OH"] = (39.962, -83.000),  // Columbus
            ["OK"] = (35.482, -97.534),  // Oklahoma City
            ["OR"] = (44.923, -123.046), // Salem
            ["PA"] = (40.269, -76.875),  // Harrisburg
            ["RI"] = (41.823, -71.423),  // Providence
            ["SC"] = (34.000, -81.035),  // Columbia
            ["SD"] = (44.367, -100.336), // Pierre
            ["TN"] = (36.165, -86.784),  // Nashville
            ["TX"] = (30.267, -97.743),  // Austin
            ["UT"] = (40.778, -111.891), // Salt Lake City
            ["VT"] = (44.260, -72.576),  // Montpelier
            ["VA"] = (37.543, -77.434),  // Richmond
            ["WA"] = (47.042, -122.893), // Olympia
            ["WV"] = (38.348, -81.633),  // Charleston
            ["WI"] = (43.073, -89.384),  // Madison
            ["WY"] = (41.145, -104.802), // Cheyenne
        };

        private static readonly Dictionary<int, string> FipsToAbbr =
            FipsCodes.ToDictionary(kv => kv.Value, kv => kv.Key);

        private static readonly string _slugsJson =
            JsonSerializer.Serialize(StateSlugs, new JsonSerializerOptions { WriteIndented = false });

        private static readonly string _abbrJson =
            JsonSerializer.Serialize(FipsToAbbr, new JsonSerializerOptions { WriteIndented = false });

        public IViewComponentResult Invoke(string abbreviation, string stateName, string capital, string? region, string? slug = null)
        {
            var key = abbreviation.ToUpperInvariant();
            FipsCodes.TryGetValue(key, out var fips);
            CapitalCoords.TryGetValue(key, out var coords);

            return View("~/Views/Shared/Components/StateHubMap.cshtml", new StateHubMapViewModel
            {
                Abbreviation   = abbreviation,
                Slug           = slug ?? abbreviation.ToLowerInvariant(),
                StateName      = stateName,
                Capital        = capital,
                Region         = region,
                Fips           = fips,
                CapitalLat     = coords.Lat,
                CapitalLon     = coords.Lon,
                StateSlugsJson = _slugsJson,
                FipsToAbbrJson = _abbrJson,
            });
        }
    }
}
