using Microsoft.AspNetCore.Mvc;
using usasymbol.Services.Interface;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Controllers;

public sealed class PressController(
    StateRankIndexService rankIndex,
    IStateService stateService) : Controller
{
    [HttpGet("/press/state/{slug}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "category" })]
    public async Task<IActionResult> State(string slug, string? category)
    {
        var state = await stateService.GetStateBySlugAsync(slug);
        if (state == null) return NotFound();

        var allEntries = await rankIndex.GetStateEntriesAsync(state.Slug);
        var categories = allEntries
            .Select(entry => entry.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        var selectedCategory = categories.FirstOrDefault(value =>
            string.Equals(value, category, StringComparison.OrdinalIgnoreCase));
        var entries = (selectedCategory == null
                ? allEntries
                : allEntries.Where(entry => string.Equals(entry.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)))
            .Take(30)
            .ToList();

        var facts = entries.Select(entry => BuildFact(state.Name, entry)).ToList();
        var emailFacts = facts.Take(5).Select(fact => $"- {fact.Sentence} https://usasymbol.com{fact.Entry.Url}");
        var subject = $"New state data: {state.Name} stands out in {facts.Count} rankings";
        var body = $"Hi,\n\nWe analyzed current state-level datasets and found several figures that may be useful for {state.Name} readers:\n\n" +
                   string.Join("\n", emailFacts) +
                   "\n\nEach ranking includes the underlying table, methodology, and source. You're welcome to cite or embed the map with attribution to USA Symbol.\n\nBest,\nUSA Symbol";

        ViewData["Title"] = $"{state.Name} Press Data Pack | USA Symbol";
        ViewData["Description"] = $"Verified state ranking facts and source links for {state.Name}.";
        ViewData["Robots"] = "noindex, nofollow";
        ViewData["NoCanonical"] = true;

        return View(new StatePressPackViewModel
        {
            StateName = state.Name,
            StateSlug = state.Slug,
            Abbreviation = state.Abbreviation,
            SelectedCategory = selectedCategory,
            Categories = categories,
            Facts = facts,
            EmailSubject = subject,
            EmailBody = body
        });
    }

    private static PressFactViewModel BuildFact(string stateName, StateRankEntry entry)
    {
        var position = entry.IsLowest
            ? entry.TieCount > 1 ? "tied for the lowest" : "the lowest"
            : entry.Rank switch
            {
                1 when entry.TieCount > 1 => "tied for the highest",
                1 => "the highest",
                2 when entry.TieCount > 1 => "tied for the second-highest",
                2 => "the second-highest",
                3 when entry.TieCount > 1 => "tied for the third-highest",
                3 => "the third-highest",
                _ => $"#{entry.Rank}"
            };

        return new PressFactViewModel
        {
            Entry = entry,
            PositionLabel = position,
            Sentence = $"{stateName} has {position} value in “{entry.Title}” ({entry.Value}, among {entry.Total} states)."
        };
    }
}
