using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using USASymbol.Data;
using USASymbol.Models.Content;
using Usasymbol.ViewModels;

namespace Usasymbol.Helpers
{
    public static class MapBlockHelper
    {
        public static async Task<IHtmlContent> RenderMapBlockAsync(
            this IHtmlHelper html,
            PageMap? map,
            PageTable? table = null,
            string? slug = null)
        {
            if (map == null) return HtmlString.Empty;

            var model = new MapBlockViewModel
            {
                Slug          = slug ?? string.Empty,
                Title         = map.Title,
                Image         = map.Image,
                ImageAlt      = map.ImageAlt,
                Caption       = map.Caption ?? string.Empty,
                IsCategorical = ChoroplethBuilder.IsCategoricalMap(map),
                FilterGroups  = map.Filters
                    .Where(filter => !string.IsNullOrWhiteSpace(filter.Key))
                    .Select(filter => new MapBlockViewModel.FilterGroup
                    {
                        Key         = filter.Key,
                        Label       = string.IsNullOrWhiteSpace(filter.Label) ? ChoroplethBuilder.FormatLabel(filter.Key) : filter.Label,
                        Style       = string.IsNullOrWhiteSpace(filter.Style) ? "chips" : filter.Style,
                        AccentColor = string.IsNullOrWhiteSpace(filter.AccentColor) ? "#0f766e" : filter.AccentColor!,
                        UseColors   = filter.UseColors
                    })
                    .ToList()
            };

            var viewContext = html.ViewContext;
            var sp = viewContext.HttpContext.RequestServices;

            if (table?.Rows?.Count > 0)
            {
                ChoroplethResult result;

                if (!string.IsNullOrWhiteSpace(map.MetricKey))
                    result = ChoroplethBuilder.Build(map, table.Rows);
                else
                    result = ChoroplethBuilder.BuildFlat(table.Rows);

                model.Entries         = result.Entries;
                model.LightColor      = result.LightColor;
                model.DarkColor       = result.DarkColor;
                model.MinDisplayValue = result.MinDisplayValue;
                model.MaxDisplayValue = result.MaxDisplayValue;
                model.MetricLabel     = map.MetricLabel
                    ?? (!string.IsNullOrWhiteSpace(map.MetricKey)
                        ? ChoroplethBuilder.FormatLabel(map.MetricKey)
                        : string.Empty);
                model.LegendSteps     = result.LegendSteps;

                model.Entries = await EnrichEntriesAsync(model.Entries, sp);

                if (model.IsCategorical && model.FilterGroups.Count == 0 && result.LegendSteps.Count > 0)
                {
                    model.FilterGroups.Add(new MapBlockViewModel.FilterGroup
                    {
                        Key = "_category",
                        Label = string.IsNullOrWhiteSpace(model.MetricLabel) ? "Filter by status" : $"Filter by {model.MetricLabel}",
                        Style = "chips",
                        AccentColor = "#0f766e",
                        UseColors = true
                    });
                }
            }

            var componentHelper = sp.GetRequiredService<IViewComponentHelper>();

            if (componentHelper is IViewContextAware ctx)
                ctx.Contextualize(viewContext);

            return await componentHelper.InvokeAsync("MapBlock", model);
        }

        private static async Task<List<StateMapEntry>> EnrichEntriesAsync(
            List<StateMapEntry> entries,
            IServiceProvider services)
        {
            if (entries.Count == 0)
                return entries;

            var db = services.GetService<AppDbContext>();
            if (db == null)
                return entries;

            var cache = services.GetService<IMemoryCache>();
            List<USASymbol.Models.State> states;
            if (cache != null && cache.TryGetValue("states-all", out List<USASymbol.Models.State>? cachedStates) && cachedStates != null)
            {
                states = cachedStates;
            }
            else
            {
                states = await db.States.AsNoTracking().ToListAsync();
                cache?.Set("states-all", states, TimeSpan.FromHours(1));
            }

            var stateBySlug = states
                .Where(state => !string.IsNullOrWhiteSpace(state.Slug))
                .ToDictionary(state => state.Slug, state => state, StringComparer.OrdinalIgnoreCase);

            var stateByAbbr = states
                .Where(state => !string.IsNullOrWhiteSpace(state.Abbreviation))
                .ToDictionary(state => state.Abbreviation, state => state, StringComparer.OrdinalIgnoreCase);

            return entries
                .Select(entry =>
                {
                    var state = FindState(entry, stateBySlug, stateByAbbr);
                    if (state == null)
                        return entry;

                    var details = entry.Details.ToList();
                    AddDetailIfMissing(details, "Official Name", state.Name);
                    AddDetailIfMissing(details, "Abbreviation", $"[{state.Abbreviation}](/states/{state.Slug}/abbreviation)");
                    AddDetailIfMissing(details, "Capital City", state.Capital);
                    AddDetailIfMissing(details, "Population", state.Population?.ToString("N0"));
                    AddDetailIfMissing(details, "Statehood", state.StateHoodDate?.ToString("MMMM d, yyyy"));
                    AddDetailIfMissing(details, "Region", state.Region);

                    return entry with
                    {
                        StateSlug = string.IsNullOrWhiteSpace(entry.StateSlug) ? state.Slug : entry.StateSlug,
                        FlagImageUrl = entry.FlagImageUrl ?? state.FlagImageUrl,
                        Details = details
                    };
                })
                .ToList();
        }

        private static USASymbol.Models.State? FindState(
            StateMapEntry entry,
            IReadOnlyDictionary<string, USASymbol.Models.State> stateBySlug,
            IReadOnlyDictionary<string, USASymbol.Models.State> stateByAbbr)
        {
            if (!string.IsNullOrWhiteSpace(entry.StateSlug) && stateBySlug.TryGetValue(entry.StateSlug, out var bySlug))
                return bySlug;

            if (!string.IsNullOrWhiteSpace(entry.PostalCode) && stateByAbbr.TryGetValue(entry.PostalCode, out var byAbbr))
                return byAbbr;

            return null;
        }

        private static void AddDetailIfMissing(List<MapEntryDetail> details, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (details.Any(detail => string.Equals(detail.Label, label, StringComparison.OrdinalIgnoreCase)))
                return;

            details.Add(new MapEntryDetail(label, value, "state"));
        }
    }
}
