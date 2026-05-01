using System.Security.Cryptography;
using System.Text;

namespace USASymbol.Services.ContentPipeline.Utils;

public sealed class TextFingerprintUtility
{
    public string Build(string text)
    {
        var normalized = (text ?? string.Empty).Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }
}
