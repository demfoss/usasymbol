using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models;
using usasymbol.Services.Interface;

namespace USASymbol.Services.Content
{
    public class AreaCodeService : IAreaCodeService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        private string DataFile => Path.Combine(_env.ContentRootPath, "Content", "data", "area-codes.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AreaCodeService(IWebHostEnvironment env, IMemoryCache cache)
        {
            _env = env;
            _cache = cache;
        }

        private Dictionary<string, AreaCodeEntry> LoadAll()
        {
            return _cache.GetOrCreate("area-codes-all", entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                if (!File.Exists(DataFile)) return new Dictionary<string, AreaCodeEntry>();

                var json = File.ReadAllText(DataFile);
                var rows = JsonSerializer.Deserialize<List<AreaCodeEntry>>(json, JsonOptions) ?? new List<AreaCodeEntry>();

                return rows
                    .Where(r => UsStatesReference.Names.ContainsKey(r.PrimaryState))
                    .ToDictionary(r => r.AreaCode, r => r);
            }) ?? new Dictionary<string, AreaCodeEntry>();
        }

        public AreaCodeResult? GetByAreaCode(string areaCode)
        {
            var key = (areaCode ?? "").Trim();
            if (!LoadAll().TryGetValue(key, out var entry)) return null;

            var stateName = UsStatesReference.NameFor(entry.PrimaryState) ?? entry.PrimaryState;
            var otherStates = entry.States
                .Where(s => s != entry.PrimaryState)
                .Select(s => UsStatesReference.NameFor(s) ?? s)
                .OrderBy(s => s)
                .ToList();

            return new AreaCodeResult
            {
                AreaCode = entry.AreaCode,
                PrimaryStateCode = entry.PrimaryState,
                PrimaryStateName = stateName,
                PrimaryStateSlug = UsStatesReference.SlugFor(stateName),
                FlagUrl = UsStatesReference.FlagUrlFor(entry.PrimaryState),
                Cities = entry.Cities,
                SpansMultipleStates = otherStates.Count > 0,
                OtherStateNames = otherStates
            };
        }
    }
}
