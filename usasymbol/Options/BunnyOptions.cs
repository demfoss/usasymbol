namespace USASymbol.Options;

public sealed class BunnyOptions
{
    public const string SectionName = "Bunny";

    public string StorageZoneName { get; set; } = string.Empty;
    public string StorageApiKey { get; set; } = string.Empty;
    public string StorageRegion { get; set; } = string.Empty;
    public string CdnBaseUrl { get; set; } = string.Empty;
    public bool UseCdnInProduction { get; set; }
    public string LocalImagesBasePath { get; set; } = "/images";
}
