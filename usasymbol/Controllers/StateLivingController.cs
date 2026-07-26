using Microsoft.AspNetCore.Mvc;
using USASymbol.Services.Interface;

namespace USASymbol.Controllers;

public class StateLivingController : Controller
{
    private readonly IStateLivingService _livingService;

    public StateLivingController(IStateLivingService livingService)
    {
        _livingService = livingService;
    }

    [HttpGet("/states/living")]
    public async Task<IActionResult> Hub()
    {
        var model = await _livingService.GetHubAsync();
        ViewData["Title"] = "Living in the U.S.: Compare All 50 States";
        ViewData["Description"] = "Browse living guides for all 50 states. Filter by affordability, safety, warm climate, and quality of life using sourced statewide data.";
        ViewData["Canonical"] = "/states/living";
        ViewData["BodyClass"] = "state-living-hub-page";
        return View(model);
    }

    [HttpGet("/states/{stateSlug}/living")]
    public async Task<IActionResult> Index(string stateSlug)
    {
        var model = await _livingService.GetAsync(stateSlug);
        if (model is null)
            return NotFound();

        ViewData["Title"] = $"Living in {model.State.Name}: Cost, Quality of Life, Pros & Cons";
        ViewData["Description"] = $"Is {model.State.Name} a good place to live? Compare cost of living, income, housing, safety, healthcare, schools, climate, and moving trade-offs.";
        ViewData["Canonical"] = $"/states/{model.State.Slug}/living";
        ViewData["OgImage"] = model.Photos.FirstOrDefault()?.ImageUrl ?? model.FlagImageUrl;
        ViewData["BodyClass"] = "state-living-page";
        return View(model);
    }
}
