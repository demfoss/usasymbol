using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;

namespace Usasymbol.Helpers
{
    public static class PageDataHeroBuilder
    {
        private static readonly string[] TitleKeys =
        {
            "album", "actor", "musician", "winner", "item", "name", "title",
            "answer", "symbol", "flower", "bird", "tree", "employer", "company",
            "team", "brand", "food", "law", "nickname", "model", "vehicle"
        };

        private static readonly string[] SubtitleKeys =
        {
            "artist", "subtitle", "category", "type", "designation", "genre",
            "party", "location", "city", "year"
        };

        private static readonly HashSet<string> IgnoredKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "rank", "position", "order", "#", "state", "state_name", "state_slug",
            "postal", "postal_code", "abbreviation", "notes", "note", "description",
            "summary", "url", "link", "image", "hero_image", "symbol_image"
        };

        private static readonly (string Name, string Slug, string Postal, int Column, int Row)[] StateLayout =
        {
            ("Alaska", "alaska", "AK", 1, 1), ("Maine", "maine", "ME", 11, 1),
            ("Vermont", "vermont", "VT", 10, 2), ("New Hampshire", "new-hampshire", "NH", 11, 2),
            ("Washington", "washington", "WA", 1, 3), ("Idaho", "idaho", "ID", 2, 3),
            ("Montana", "montana", "MT", 3, 3), ("North Dakota", "north-dakota", "ND", 4, 3),
            ("Minnesota", "minnesota", "MN", 5, 3), ("Illinois", "illinois", "IL", 6, 3),
            ("Wisconsin", "wisconsin", "WI", 7, 3), ("Michigan", "michigan", "MI", 8, 3),
            ("New York", "new-york", "NY", 9, 3), ("Rhode Island", "rhode-island", "RI", 10, 3),
            ("Massachusetts", "massachusetts", "MA", 11, 3),
            ("Oregon", "oregon", "OR", 1, 4), ("Nevada", "nevada", "NV", 2, 4),
            ("Wyoming", "wyoming", "WY", 3, 4), ("South Dakota", "south-dakota", "SD", 4, 4),
            ("Iowa", "iowa", "IA", 5, 4), ("Indiana", "indiana", "IN", 6, 4),
            ("Ohio", "ohio", "OH", 7, 4), ("Pennsylvania", "pennsylvania", "PA", 8, 4),
            ("New Jersey", "new-jersey", "NJ", 9, 4), ("Connecticut", "connecticut", "CT", 10, 4),
            ("California", "california", "CA", 1, 5), ("Utah", "utah", "UT", 2, 5),
            ("Colorado", "colorado", "CO", 3, 5), ("Nebraska", "nebraska", "NE", 4, 5),
            ("Missouri", "missouri", "MO", 5, 5), ("Kentucky", "kentucky", "KY", 6, 5),
            ("West Virginia", "west-virginia", "WV", 7, 5), ("Virginia", "virginia", "VA", 8, 5),
            ("Maryland", "maryland", "MD", 9, 5), ("Delaware", "delaware", "DE", 10, 5),
            ("Arizona", "arizona", "AZ", 2, 6), ("New Mexico", "new-mexico", "NM", 3, 6),
            ("Kansas", "kansas", "KS", 4, 6), ("Arkansas", "arkansas", "AR", 5, 6),
            ("Tennessee", "tennessee", "TN", 6, 6), ("North Carolina", "north-carolina", "NC", 7, 6),
            ("South Carolina", "south-carolina", "SC", 8, 6),
            ("Oklahoma", "oklahoma", "OK", 3, 7), ("Louisiana", "louisiana", "LA", 4, 7),
            ("Mississippi", "mississippi", "MS", 5, 7), ("Alabama", "alabama", "AL", 6, 7),
            ("Georgia", "georgia", "GA", 7, 7),
            ("Hawaii", "hawaii", "HI", 1, 8), ("Texas", "texas", "TX", 3, 8),
            ("Florida", "florida", "FL", 8, 8)
        };

        public static PageDataHeroViewModel Build(
            PageDetailViewModel pageModel,
            string kindLabel,
            string hubUrl,
            string categoryLabel,
            string categoryUrl,
            string tableAnchor)
        {
            var content = pageModel.Content;
            var table = content?.Table;
            var items = table == null
                ? new List<PageDataHeroItem>()
                : table.Rows.Select((row, index) => BuildItem(table, content, row, index)).ToList();
            var stateItems = items
                .Where(item => !string.IsNullOrWhiteSpace(item.StateSlug))
                .GroupBy(item => item.StateSlug, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var hasRank = table?.Columns.Any(column =>
                string.Equals(column.Key, "rank", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(column.Type, "rank", StringComparison.OrdinalIgnoreCase)) == true ||
                table?.Rows.Any(row => row.Has("rank")) == true;
            var normalizedTitle = content?.Page?.H1 ?? string.Empty;
            var isByState = Regex.IsMatch(
                normalizedTitle,
                @"\b(by|from|in)\s+(each|every)?\s*state\b|\bevery\s+state\b|\bstates?\s+by\b",
                RegexOptions.IgnoreCase);
            var explicitVariant = NormalizeVariant(content?.HeroVariant);

            // TileMap ("browse your state") is reserved for pages where every state's own
            // answer is the point (symbol listings like flags/birds/mottos). Actual rankings
            // are about the top of the list, even when their title says "by State", so they
            // get the hero-light chart instead of the map.
            var variant = explicitVariant ??
                (!pageModel.IsRanking && isByState && stateItems.Count >= 3
                    ? PageDataHeroVariant.TileMap
                    : hasRank && items.Count >= 3
                        ? PageDataHeroVariant.TopChart
                        : PageDataHeroVariant.Compact);

            var model = new PageDataHeroViewModel
            {
                PageModel = pageModel,
                Variant = variant,
                KindLabel = kindLabel,
                HubUrl = hubUrl,
                CategoryLabel = categoryLabel,
                CategoryUrl = categoryUrl,
                Title = string.IsNullOrWhiteSpace(content?.Page?.H1) ? kindLabel : content.Page.H1,
                Description = content?.Seo?.Description ?? string.Empty,
                SourceName = content?.Page?.Sources?.FirstOrDefault()?.Name ?? string.Empty,
                DateModified = content?.DateModified,
                EntryCount = pageModel.TotalRowsCount,
                MetricLabel = ResolveMetricLabel(table, content),
                TableAnchor = tableAnchor,
                HasMethodology = !string.IsNullOrWhiteSpace(content?.Page?.Methodology) || pageModel.HasSources,
                Items = items.Take(5).ToList()
            };

            model.StateTiles = StateLayout.Select(state => new PageDataHeroStateTile
            {
                Name = state.Name,
                Slug = state.Slug,
                PostalCode = state.Postal,
                Column = state.Column,
                Row = state.Row,
                Item = stateItems.GetValueOrDefault(state.Slug)
            }).ToList();
            model.StatesRepresented = model.StateTiles.Count(tile => tile.Item != null);

            return model;
        }

        private static PageDataHeroItem BuildItem(
            PageTable table,
            PageContent? content,
            TableRow row,
            int index)
        {
            var state = First(row, "state", "state_name");
            var stateSlug = First(row, "state_slug");
            if (string.IsNullOrWhiteSpace(stateSlug) && !string.IsNullOrWhiteSpace(state))
                stateSlug = Slugify(state);

            var titleKey = TitleKeys.FirstOrDefault(row.Has);
            if (string.IsNullOrWhiteSpace(titleKey))
            {
                titleKey = table.Columns.FirstOrDefault(column =>
                    !IgnoredKeys.Contains(column.Key) &&
                    string.Equals(column.Type, "text", StringComparison.OrdinalIgnoreCase) &&
                    row.Has(column.Key))?.Key;
            }

            var title = string.IsNullOrWhiteSpace(titleKey) ? state : row.GetString(titleKey);
            var subtitleParts = new List<string>();
            foreach (var key in SubtitleKeys)
            {
                if (string.Equals(key, titleKey, StringComparison.OrdinalIgnoreCase) || !row.Has(key))
                    continue;
                var value = row.GetString(key);
                if (!string.IsNullOrWhiteSpace(value) &&
                    !subtitleParts.Contains(value, StringComparer.OrdinalIgnoreCase))
                    subtitleParts.Add(value);
                if (subtitleParts.Count == 2)
                    break;
            }

            var metricKey = ResolveMetricKey(table, content, titleKey);
            var metricValue = string.IsNullOrWhiteSpace(metricKey) ? string.Empty : row.GetString(metricKey);
            var numericValue = ParseNumber(metricValue);
            var rankText = First(row, "rank", "position", "order");
            var rank = int.TryParse(rankText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRank)
                ? parsedRank
                : index + 1;

            return new PageDataHeroItem
            {
                Rank = rank,
                State = state,
                StateSlug = stateSlug,
                PostalCode = First(row, "postal_code", "postal", "abbreviation"),
                Title = string.IsNullOrWhiteSpace(title) ? $"Entry {rank}" : title,
                Subtitle = string.Join(" · ", subtitleParts),
                Value = metricValue,
                NumericValue = numericValue,
                Url = ResolveDetailUrl(row, content, stateSlug)
            };
        }

        // Mirrors Components/Tables/_Table.cshtml's URL priority so map tiles land on the same
        // detail page a table row would (e.g. /states/alaska/sport/dog-mushing), not just the
        // generic state page, whenever the table has enough to build one.
        private static string ResolveDetailUrl(TableRow row, PageContent? content, string stateSlug)
        {
            var explicitUrl = row.GetString("symbol_url");
            if (!string.IsNullOrWhiteSpace(explicitUrl))
                return explicitUrl;

            var customUrl = row.GetString("custom_url");
            if (!string.IsNullOrWhiteSpace(customUrl))
                return customUrl;

            var detailType = content?.DetailType;
            var symbolSlug = row.GetString("symbol_slug");

            if (string.IsNullOrWhiteSpace(symbolSlug) && !string.IsNullOrWhiteSpace(detailType))
                symbolSlug = row.GetString($"{detailType}_slug");

            if (string.IsNullOrWhiteSpace(symbolSlug) && string.Equals(detailType, "flag", StringComparison.OrdinalIgnoreCase))
                symbolSlug = $"{stateSlug}-state-flag";

            return !string.IsNullOrWhiteSpace(symbolSlug) && !string.IsNullOrWhiteSpace(detailType) && !string.IsNullOrWhiteSpace(stateSlug)
                ? $"/states/{stateSlug}/{detailType}/{symbolSlug}"
                : string.Empty;
        }

        private static string ResolveMetricKey(PageTable table, PageContent? content, string? titleKey)
        {
            var candidates = new[]
            {
                content.Map?.MetricKey,
                table.DefaultColumn
            }.Where(key => !string.IsNullOrWhiteSpace(key));

            foreach (var candidate in candidates)
            {
                if (!string.Equals(candidate, titleKey, StringComparison.OrdinalIgnoreCase) &&
                    table.Rows.Any(row => row.Has(candidate!)))
                    return candidate!;
            }

            return table.Columns.LastOrDefault(column =>
                !IgnoredKeys.Contains(column.Key) &&
                !string.Equals(column.Key, titleKey, StringComparison.OrdinalIgnoreCase) &&
                !column.Key.Contains("year", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(column.Type, "number", StringComparison.OrdinalIgnoreCase))?.Key ?? string.Empty;
        }

        private static string ResolveMetricLabel(PageTable? table, PageContent? content)
        {
            if (!string.IsNullOrWhiteSpace(content?.Map?.MetricLabel))
                return content.Map.MetricLabel;

            if (table == null)
                return "Value";

            var metricKey = ResolveMetricKey(table, content, null);
            return table.Columns.FirstOrDefault(column =>
                string.Equals(column.Key, metricKey, StringComparison.OrdinalIgnoreCase))?.Label ?? "Value";
        }

        private static PageDataHeroVariant? NormalizeVariant(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "compact" or "utility" or "h3" => PageDataHeroVariant.Compact,
                "light" or "top-chart" or "chart" => PageDataHeroVariant.TopChart,
                "top-strip" or "top5" or "top-5" => PageDataHeroVariant.TopStrip,
                "tile-map" or "map-tiles" or "map" => PageDataHeroVariant.TileMap,
                _ => null
            };
        }

        private static string First(TableRow row, params string[] keys)
        {
            return keys.Select(row.GetString).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private static double? ParseNumber(string value)
        {
            var normalized = Regex.Replace(value ?? string.Empty, @"[^\d.\-]", string.Empty);
            return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;
        }

        private static string Slugify(string value)
        {
            var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            normalized = new string(normalized
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                .ToArray());
            normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "-");
            return normalized.Trim('-');
        }
    }
}
