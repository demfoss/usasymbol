using USASymbol.Models.ContentPipeline;
using USASymbol.Services.ContentPipeline.Utils;

namespace USASymbol.Services.ContentPipeline;

public sealed class SimilarityService
{
    private readonly PatternMemoryService _patternMemoryService;
    private readonly TextFingerprintUtility _textFingerprintUtility;

    public SimilarityService(PatternMemoryService patternMemoryService, TextFingerprintUtility textFingerprintUtility)
    {
        _patternMemoryService = patternMemoryService;
        _textFingerprintUtility = textFingerprintUtility;
    }

    public async Task<SimilarityReportModel> CheckAsync(
        string categoryKey,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SimilarityReportModel();
        }

        var fingerprint = _textFingerprintUtility.Build(text);
        var memory = await _patternMemoryService.LoadAsync(categoryKey, cancellationToken);
        var match = memory.FirstOrDefault(x => string.Equals(x.Fingerprint, fingerprint, StringComparison.Ordinal));

        if (match is not null)
        {
            return new SimilarityReportModel
            {
                IsWarning = true,
                Score = 1,
                ComparedPath = match.SourcePath,
                Summary = match.Summary
            };
        }

        var weaker = memory.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.Summary) &&
            text.Contains(x.Summary, StringComparison.OrdinalIgnoreCase));

        if (weaker is null)
        {
            return new SimilarityReportModel();
        }

        return new SimilarityReportModel
        {
            IsWarning = true,
            Score = 0.5,
            ComparedPath = weaker.SourcePath,
            Summary = weaker.Summary
        };
    }
}
