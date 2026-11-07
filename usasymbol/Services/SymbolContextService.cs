using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Data;
using USASymbol.Extensions;
using USASymbol.Helpers;
using USASymbol.Models;
using USASymbol.Models.ViewModels;

namespace USASymbol.Services
{
    /// <summary>
    /// Computes page context for a state symbol (uniqueness, adoption rank, peers, tile map, prev/next)
    /// purely from the Symbols table. No hand-written data.
    /// </summary>
    public class SymbolContextService
    {
        private sealed record Row(int SymbolId, int StateId, string Name, string Slug, string Type, int? Year,
            string? Designation, string StateName, string StateSlug, string Abbr, string? Region);

        private static readonly char[] NameSeparators = { ' ', '-', '\'' };

        private static readonly string[][] TileLayout =
        {
            new[] { "AK", "", "", "", "", "", "", "", "", "", "ME" },
            new[] { "", "", "", "", "", "", "", "", "", "VT", "NH" },
            new[] { "WA", "ID", "MT", "ND", "MN", "WI", "IL", "MI", "NY", "RI", "MA" },
            new[] { "OR", "NV", "WY", "SD", "IA", "IN", "OH", "PA", "NJ", "CT", "" },
            new[] { "CA", "UT", "CO", "NE", "MO", "KY", "WV", "VA", "MD", "DE", "" },
            new[] { "", "AZ", "NM", "KS", "AR", "TN", "NC", "SC", "", "", "" },
            new[] { "", "", "OK", "LA", "MS", "AL", "GA", "", "", "", "" },
            new[] { "HI", "", "TX", "", "", "", "", "", "FL", "", "DC" },
        };

        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        public SymbolContextService(AppDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        private async Task<List<Row>> AllRowsAsync()
        {
            return (await _cache.GetOrCreateAsync("symbol-context-rows", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3);
                return await _db.Symbols.AsNoTracking()
                    .Where(s => s.State != null)
                    .Select(s => new Row(s.Id, s.StateId, s.Name, s.Slug, s.Type, s.AdoptedYear, s.Designation,
                        s.State!.Name, s.State.Slug, s.State.Abbreviation, s.State.Region))
                    .ToListAsync();
            }))!;
        }

        private static string Norm(string s) => new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        public async Task<SymbolPageContext?> BuildAsync(ISymbolDetailViewModel model)
        {
            if (model.IsNationalSymbol || model.Symbol == null || model.State == null) return null;

            var all = await AllRowsAsync();
            var cur = all.FirstOrDefault(r => r.SymbolId == model.Symbol.Id);
            if (cur == null) return null;

            var stateName = cur.StateName;
            var noun = NounFor(model.SymbolTypeName);
            var peers = all.Where(r => r.Type == cur.Type).ToList();
            var statesWithType = peers.Select(r => r.StateId).Distinct().Count();

            var curKey = Norm(cur.Name);
            var sameName = peers.Where(r => r.StateId != cur.StateId && Norm(r.Name) == curKey)
                .GroupBy(r => r.StateId).Select(g => g.First()).ToList();

            // Types where every state has its own name (flags, seals, mottos...) make "unique" meaningless.
            var distinctNames = peers.Select(r => Norm(r.Name)).Distinct().Count();
            var trivial = peers.Count >= 20 && distinctNames >= peers.Count * 0.9;

            string? uniqueBadge = null;
            var isUnique = sameName.Count == 0;
            if (!trivial && statesWithType > 3)
            {
                uniqueBadge = isUnique
                    ? $"Unique: only {stateName} chose {cur.Name}"
                    : $"Shared with {sameName.Count} other state{(sameName.Count == 1 ? "" : "s")} ({cur.Name})";
            }

            // Adoption order: earliest year per state.
            string? rankBadge = null, ageBadge = null, oldestNewest = null;
            var dated = peers.Where(r => r.Year is > 1700)
                .GroupBy(r => r.StateId)
                .Select(g => g.OrderBy(r => r.Year).First())
                .OrderBy(r => r.Year).ThenBy(r => r.StateName)
                .ToList();
            if (cur.Year is > 1700 && dated.Count >= 8)
            {
                var rank = dated.FindIndex(r => r.StateId == cur.StateId) + 1;
                if (rank > 0)
                    rankBadge = $"{Ordinal(rank)} of {dated.Count} states to adopt a {noun}";
                var years = DateTime.UtcNow.Year - cur.Year!.Value;
                if (years >= 0) ageBadge = years == 0 ? "Adopted this year" : $"Adopted {years} year{(years == 1 ? "" : "s")} ago";
                var first = dated[0];
                var last = dated[^1];
                oldestNewest = $"Oldest: {first.StateName} ({first.Year}). Newest: {last.StateName} ({last.Year}).";
            }

            // Peers: same name first, then name overlap, then same region.
            var chosen = new List<Row>(sameName.OrderBy(r => r.Year ?? int.MaxValue).ThenBy(r => r.StateName));
            var heading = chosen.Count > 0 ? $"Also the {noun} of" : $"Other state {Plural(noun)}";
            if (chosen.Count < 5)
            {
                var tokens = cur.Name.Split(NameSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.Length >= 4).Select(t => t.ToLowerInvariant()).ToHashSet();
                if (tokens.Count > 0)
                {
                    var similar = peers
                        .Where(r => r.StateId != cur.StateId && chosen.All(c => c.StateId != r.StateId)
                            && r.Name.Split(NameSeparators, StringSplitOptions.RemoveEmptyEntries)
                                .Any(t => tokens.Contains(t.ToLowerInvariant())))
                        .GroupBy(r => r.StateId).Select(g => g.First());
                    chosen.AddRange(similar.Take(5 - chosen.Count));
                }
            }
            if (chosen.Count < 4)
            {
                var regional = peers
                    .Where(r => r.StateId != cur.StateId && chosen.All(c => c.StateId != r.StateId)
                        && string.Equals(r.Region, cur.Region, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(r => r.StateId).Select(g => g.First()).OrderBy(r => r.StateName);
                chosen.AddRange(regional.Take(4 - chosen.Count));
                if (sameName.Count == 0) heading = $"Other {Plural(noun)} in the {cur.Region ?? "U.S."}";
            }

            var peerList = chosen.Take(6).Select(r => new SymbolPeer
            {
                StateName = r.StateName,
                StateSlug = r.StateSlug,
                Abbr = r.Abbr,
                SymbolName = r.Name,
                Year = r.Year,
                Url = ToSymbol(r).ToSymbolUrl(r.StateSlug)
            }).ToList();

            // Tile map.
            var byState = peers.GroupBy(r => r.Abbr, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var stateInfo = all.GroupBy(r => r.Abbr, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var tiles = new List<SymbolTile>();
            foreach (var row in TileLayout)
            {
                foreach (var abbr in row)
                {
                    if (abbr == "") { tiles.Add(new SymbolTile()); continue; }
                    stateInfo.TryGetValue(abbr, out var info);
                    byState.TryGetValue(abbr, out var list);
                    var kind = "none";
                    Row? shown = list?.FirstOrDefault();
                    if (info != null && info.StateId == cur.StateId) { kind = "current"; shown = cur; }
                    else if (list != null)
                    {
                        var match = list.FirstOrDefault(r => Norm(r.Name) == curKey);
                        kind = match != null ? "same" : "other";
                        shown = match ?? list[0];
                    }
                    tiles.Add(new SymbolTile
                    {
                        Abbr = abbr,
                        Name = info?.StateName ?? abbr,
                        Slug = info?.StateSlug ?? "",
                        Kind = kind,
                        SymbolName = shown?.Name,
                        Url = shown != null ? ToSymbol(shown).ToSymbolUrl(shown.StateSlug) : null
                    });
                }
            }

            var caption = sameName.Count > 0
                ? $"{stateName} is one of {statesWithType} states with an official {noun}. {sameName.Count + 1} chose {cur.Name}."
                : statesWithType > 1
                    ? $"{stateName} is one of {statesWithType} states with an official {noun}, and the only one to choose {cur.Name}."
                    : $"{stateName} is the only state with an official {noun}.";

            // Prev/next inside the state.
            var mine = all.Where(r => r.StateId == cur.StateId)
                .OrderBy(r => r.Type, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var idx = mine.FindIndex(r => r.SymbolId == cur.SymbolId);
            SymbolNeighbor? Nb(int i) => i < 0 || i >= mine.Count ? null : new SymbolNeighbor
            {
                Label = SymbolTypeInfo.Label(mine[i].Type, mine[i].Designation),
                Name = mine[i].Name,
                Url = ToSymbol(mine[i]).ToSymbolUrl(mine[i].StateSlug)
            };

            var plural = string.IsNullOrWhiteSpace(model.SymbolTypePlural) ? "symbols" : model.SymbolTypePlural;
            return new SymbolPageContext
            {
                StateName = stateName,
                Noun = noun,
                HubUrl = $"/symbols/{plural}",
                HubLabel = $"Explore all {plural}",
                UniqueBadge = uniqueBadge,
                IsUnique = isUnique,
                RankBadge = rankBadge,
                AgeBadge = ageBadge,
                OldestNewest = oldestNewest,
                MapCaption = caption,
                StatesWithType = statesWithType,
                Tiles = tiles,
                PeersHeading = heading,
                Peers = peerList,
                Previous = Nb(idx - 1),
                Next = Nb(idx + 1)
            };
        }

        private static Symbol ToSymbol(Row r) => new() { Id = r.SymbolId, Type = r.Type, Slug = r.Slug, Name = r.Name };

        private static string NounFor(string? typeName)
        {
            var n = (typeName ?? "symbol").Trim();
            if (n.StartsWith("State ", StringComparison.OrdinalIgnoreCase)) n = n[6..];
            return n.ToLowerInvariant();
        }

        private static string Plural(string noun)
            => noun.EndsWith('s') ? noun
                : noun.EndsWith('y') && noun.Length > 2 && !"aeiou".Contains(noun[^2]) ? noun[..^1] + "ies"
                : noun + "s";

        private static string Ordinal(int n)
        {
            var s = (n % 100) is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
            return n.ToString(CultureInfo.InvariantCulture) + s;
        }
    }
}
