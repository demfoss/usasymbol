namespace USASymbol.Services.Images;

public interface IImagePathNormalizer
{
    string Normalize(string? path);
    bool IsAbsoluteUrl(string? path);
    bool IsLocalPath(string? path);
}
