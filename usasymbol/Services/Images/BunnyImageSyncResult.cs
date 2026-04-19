namespace USASymbol.Services.Images;

public sealed class BunnyImageSyncResult
{
    public int Uploaded { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
}
