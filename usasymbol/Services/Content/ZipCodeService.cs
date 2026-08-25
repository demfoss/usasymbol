using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models;
using usasymbol.Services.Interface;

namespace USASymbol.Services.Content
{
    public class ZipCodeService : IZipCodeService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        private string DataFile => Path.Combine(_env.ContentRootPath, "Content", "data", "us-zip-codes.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ZipCodeService(IWebHostEnvironment env, IMemoryCache cache)
        {
            _env = env;
            _cache = cache;
        }

        // The site only publishes pages for the 50 states (no DC), so DC ZIP codes
        // are filtered out at load time.
        private List<ZipCodeEntry> LoadAll()
        {
            return _cache.GetOrCreate("zip-codes-all", entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                if (!File.Exists(DataFile)) return new List<ZipCodeEntry>();

                var json = File.ReadAllText(DataFile);
                var rows = JsonSerializer.Deserialize<List<ZipCodeEntry>>(json, JsonOptions) ?? new List<ZipCodeEntry>();

                return rows.Where(r => UsStatesReference.Names.ContainsKey(r.State)).ToList();
            }) ?? new List<ZipCodeEntry>();
        }

        private Dictionary<string, List<ZipCodeEntry>> ByZipIndex()
        {
            return _cache.GetOrCreate("zip-codes-by-zip", entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);
                return LoadAll().GroupBy(r => r.Zip).ToDictionary(g => g.Key, g => g.ToList());
            }) ?? new Dictionary<string, List<ZipCodeEntry>>();
        }

        private static ZipCodeResult ToResult(ZipCodeEntry r)
        {
            var stateName = UsStatesReference.NameFor(r.State) ?? r.State;
            return new ZipCodeResult
            {
                Zip = r.Zip,
                City = r.City,
                County = r.County,
                StateCode = r.State,
                StateName = stateName,
                StateSlug = UsStatesReference.SlugFor(stateName),
                FlagUrl = UsStatesReference.FlagUrlFor(r.State),
                Lat = r.Lat,
                Lng = r.Lng,
                Population = r.Population
            };
        }

        public List<ZipCodeResult> GetRandom(int count, string? stateCode)
        {
            count = Math.Clamp(count, 1, 100);

            var pool = LoadAll();

            if (!string.IsNullOrWhiteSpace(stateCode))
            {
                var code = stateCode.Trim().ToUpperInvariant();
                pool = pool.Where(r => r.State == code).ToList();
            }

            if (pool.Count == 0) return new List<ZipCodeResult>();

            var rng = Random.Shared;
            var picks = pool.Count <= count
                ? pool
                : pool.OrderBy(_ => rng.Next()).Take(count).ToList();

            return picks.Select(ToResult).OrderBy(_ => rng.Next()).ToList();
        }

        public List<ZipCodeResult> GetRandomCities(int count, string? stateCode)
        {
            count = Math.Clamp(count, 1, 100);

            var pool = LoadAll();

            if (!string.IsNullOrWhiteSpace(stateCode))
            {
                var code = stateCode.Trim().ToUpperInvariant();
                pool = pool.Where(r => r.State == code).ToList();
            }

            // Collapse to unique city+state combinations (keep the lowest ZIP as the representative).
            var uniqueCities = pool
                .GroupBy(r => (r.City.Trim().ToLowerInvariant(), r.State))
                .Select(g => g.OrderBy(r => r.Zip).First())
                .ToList();

            if (uniqueCities.Count == 0) return new List<ZipCodeResult>();

            var rng = Random.Shared;
            var picks = uniqueCities.Count <= count
                ? uniqueCities
                : uniqueCities.OrderBy(_ => rng.Next()).Take(count).ToList();

            return picks.Select(ToResult).OrderBy(_ => rng.Next()).ToList();
        }

        public ZipCodeResult? GetByZip(string zip)
        {
            var key = (zip ?? "").Trim().PadLeft(5, '0');
            if (!ByZipIndex().TryGetValue(key, out var matches) || matches.Count == 0) return null;
            return ToResult(matches[0]);
        }

        public List<ZipCodeResult> FindByCity(string city, string? stateCode)
        {
            var pool = LoadAll();
            var needle = (city ?? "").Trim();
            if (needle.Length == 0) return new List<ZipCodeResult>();

            var matches = pool.Where(r => string.Equals(r.City, needle, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(stateCode))
            {
                var code = stateCode.Trim().ToUpperInvariant();
                matches = matches.Where(r => r.State == code);
            }

            return matches
                .GroupBy(r => (r.City.Trim().ToLowerInvariant(), r.State, r.County))
                .Select(g => ToResult(g.First()))
                .Take(20)
                .ToList();
        }

        public List<StateSummary> GetStateSummaries()
        {
            var pool = LoadAll();

            return pool
                .GroupBy(r => r.State)
                .Select(g =>
                {
                    var stateName = UsStatesReference.NameFor(g.Key) ?? g.Key;
                    return new StateSummary
                    {
                        Code = g.Key,
                        Name = stateName,
                        Slug = UsStatesReference.SlugFor(stateName),
                        FlagUrl = UsStatesReference.FlagUrlFor(g.Key),
                        ZipCount = g.Count()
                    };
                })
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// Sums population across every ZIP Code Tabulation Area matching a city+state — an
        /// estimate, since a city can span several ZCTAs and ZCTA boundaries don't exactly
        /// match city limits.
        /// </summary>
        public (int? Population, List<ZipCodeResult> Zips)? GetCityPopulation(string city, string stateCode)
        {
            var pool = LoadAll();
            var needle = (city ?? "").Trim();
            var code = (stateCode ?? "").Trim().ToUpperInvariant();
            if (needle.Length == 0 || code.Length == 0) return null;

            var matches = pool
                .Where(r => string.Equals(r.City, needle, StringComparison.OrdinalIgnoreCase) && r.State == code)
                .ToList();

            if (matches.Count == 0) return null;

            int? total = matches.Any(m => m.Population.HasValue)
                ? matches.Sum(m => m.Population ?? 0)
                : null;

            return (total, matches.Select(ToResult).ToList());
        }

        public FakeAddressResult GenerateRandomAddress(string? stateCode)
        {
            var place = GetRandom(1, stateCode).FirstOrDefault() ?? GetRandom(1, null).First();

            return new FakeAddressResult
            {
                Street = FakeAddressGenerator.GenerateStreetLine(),
                City = place.City,
                StateCode = place.StateCode,
                StateName = place.StateName,
                StateSlug = place.StateSlug,
                Zip = place.Zip,
                FlagUrl = place.FlagUrl
            };
        }

        public DistanceResult? GetDistance(string fromZip, string toZip)
        {
            var from = GetByZip(fromZip);
            var to = GetByZip(toZip);
            if (from == null || to == null) return null;

            double? miles = null;
            double? km = null;

            if (from.Lat.HasValue && from.Lng.HasValue && to.Lat.HasValue && to.Lng.HasValue)
            {
                km = GeoMath.HaversineKm(from.Lat.Value, from.Lng.Value, to.Lat.Value, to.Lng.Value);
                miles = GeoMath.HaversineMiles(from.Lat.Value, from.Lng.Value, to.Lat.Value, to.Lng.Value);
            }

            return new DistanceResult
            {
                From = from,
                To = to,
                Miles = miles.HasValue ? Math.Round(miles.Value, 1) : null,
                Kilometers = km.HasValue ? Math.Round(km.Value, 1) : null
            };
        }

    }
}
