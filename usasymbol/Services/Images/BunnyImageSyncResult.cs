namespace USASymbol.Services.Images;

public sealed class BunnyImageSyncResult
{
    public int Scanned { get; set; }
    public int Uploaded { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
}
