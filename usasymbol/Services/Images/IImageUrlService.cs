namespace USASymbol.Services.Images;

public interface IImageUrlService
{
    string Url(string? path);
    string Url(string? path, ImagePreset preset);
    string Thumb(string? path);
    string Thumb(string? path, int width);
    string Card(string? path);
    string Card(string? path, int width);
    string Crop(string? path, int width, int height);
    string Hero(string? path);
    string Hero(string? path, int width);
    string Full(string? path);
}
