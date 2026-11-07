using USASymbol.Services;

namespace USASymbol.Models.ViewModels;

public sealed class PressFactViewModel
{
    public StateRankEntry Entry { get; init; } = null!;
    public string PositionLabel { get; init; } = string.Empty;
    public string Sentence { get; init; } = string.Empty;
}

public sealed class StatePressPackViewModel
{
    public string StateName { get; init; } = string.Empty;
    public string StateSlug { get; init; } = string.Empty;
    public string Abbreviation { get; init; } = string.Empty;
    public string? SelectedCategory { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();
    public IReadOnlyList<PressFactViewModel> Facts { get; init; } = Array.Empty<PressFactViewModel>();
    public string EmailSubject { get; init; } = string.Empty;
    public string EmailBody { get; init; } = string.Empty;
}
