using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface INicknameService
    {
        Task<NicknameContent?> GetNicknameContentAsync(string stateSlug);
    }
}