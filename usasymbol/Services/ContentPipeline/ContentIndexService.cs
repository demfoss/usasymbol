using USASymbol.Models.ContentPipeline;
using USASymbol.Services.ContentPipeline.Utils;

namespace USASymbol.Services.ContentPipeline;

public sealed class ContentIndexService
{
    private readonly FileScanUtility _fileScanUtility;

    public ContentIndexService(FileScanUtility fileScanUtility)
    {
        _fileScanUtility = fileScanUtility;
    }

    public Task<IReadOnlyList<ContentIndexEntryModel>> BuildAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        IReadOnlyList<ContentIndexEntryModel> entries = _fileScanUtility.ScanYamlContentIndex();
        return Task.FromResult(entries);
    }
}
