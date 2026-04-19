using usasymbol.Services.Interface;
using USASymbol.Models;

namespace USASymbol.Services
{
    public class SymbolCanonicalService : ISymbolCanonicalService
    {
        private readonly ISymbolService _symbolService;

        public SymbolCanonicalService(ISymbolService symbolService)
        {
            _symbolService = symbolService;
        }

        public async Task<Symbol?> ResolveCanonicalSymbolAsync(State state, string symbolType)
        {
            ArgumentNullException.ThrowIfNull(state);

            var normalizedType = NormalizeSymbolType(symbolType);

            if (IsSingleSymbolType(normalizedType))
                return await _symbolService.GetSymbolAsync(state.Id, normalizedType);

            var candidates = (await _symbolService.GetSymbolsByStateAsync(state.Id))
                .Where(s => string.Equals(s.Type, normalizedType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (candidates.Count == 0)
                return null;

            if (candidates.Count == 1)
                return candidates[0];

            return normalizedType switch
            {
                "mammal" => PickPrimaryMammal(candidates),
                "beverage" => PickPrimaryBeverage(candidates),
                _ => candidates
                    .OrderBy(s => s.AdoptedYear ?? int.MaxValue)
                    .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault()
            };
        }

        private static bool IsSingleSymbolType(string symbolType) => symbolType switch
        {
            "bird" => true,
            "color" => true,
            "dinosaur" => true,
            "firearm" => true,
            "flag" => true,
            "flower" => true,
            "motto" => true,
            "nickname" => true,
            "tree" => true,
            _ => false
        };

        private static string NormalizeSymbolType(string symbolType)
        {
            var normalized = (symbolType ?? string.Empty).Trim().ToLowerInvariant();

            return normalized switch
            {
                "birds" => "bird",
                "colors" => "color",
                "dinosaurs" => "dinosaur",
                "firearms" => "firearm",
                "flags" => "flag",
                "flowers" => "flower",
                "mammals" => "mammal",
                "mottos" => "motto",
                "nicknames" => "nickname",
                "trees" => "tree",
                "beverages" => "beverage",
                "license-plates" => "license-plate",
                "license-plate-slogans" => "license-plate",
                _ => normalized
            };
        }

        private static Symbol PickPrimaryMammal(List<Symbol> candidates)
        {
            return candidates
                .OrderBy(GetMammalPriority)
                .ThenBy(s => s.AdoptedYear ?? int.MaxValue)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static int GetMammalPriority(Symbol symbol)
        {
            var designation = (symbol.Designation ?? string.Empty).ToLowerInvariant();

            if (designation.Contains("state mammal") || designation.Contains("state animal"))
                return 0;

            if (designation.Contains("state land mammal") ||
                designation.Contains("state wild animal") ||
                designation.Contains("state wildlife animal") ||
                designation.Contains("state game animal") ||
                designation.Contains("state small mammal") ||
                designation.Contains("state large mammal"))
                return 1;

            if (designation.Contains("state marine mammal") ||
                designation.Contains("state saltwater mammal") ||
                designation.Contains("state water mammal") ||
                designation.Contains("migratory marine mammal") ||
                designation.Contains("state flying mammal") ||
                designation.Contains("state wildcat") ||
                designation.Contains("state marsupial"))
                return 2;

            if (designation.Contains("state dog") ||
                designation.Contains("state cat") ||
                designation.Contains("state horse") ||
                designation.Contains("state pet") ||
                designation.Contains("honorary equine") ||
                designation.Contains("heritage"))
                return 3;

            return 9;
        }

        private static Symbol PickPrimaryBeverage(List<Symbol> candidates)
        {
            return candidates
                .OrderBy(GetBeveragePriority)
                .ThenBy(s => s.AdoptedYear ?? int.MaxValue)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static int GetBeveragePriority(Symbol symbol)
        {
            var designation = (symbol.Designation ?? string.Empty).ToLowerInvariant();

            if (designation.Contains("state beverage") || designation.Contains("state drink"))
                return 0;

            if (designation.Contains("state juice") || designation.Contains("state soft drink"))
                return 1;

            if (designation.Contains("state hospitality beverage") ||
                designation.Contains("state cocktail") ||
                designation.Contains("state spirit"))
                return 2;

            return 9;
        }
    }
}
