using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface ISongService
    {
        Task<SongContent?> GetSongContentAsync(string stateSlug, string symbolSlug);
    }
}
