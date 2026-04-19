
using Microsoft.AspNetCore.Html;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace USASymbol.Helpers
{
    public static class IndexNowHelper
    {
        private static readonly HttpClient _http = new();

        public static async Task SubmitAsync(params string[] urls)
        {
            var payload = new
            {
                host = "usasymbol.com",
                key = "ffe9613e91e74da0b78c2544f33ad6ae",
                keyLocation = "https://usasymbol.com/ffe9613e91e74da0b78c2544f33ad6ae.txt",
                urlList = urls
            };

            var json = JsonSerializer.Serialize(payload);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _http.PostAsync("https://api.indexnow.org/indexnow", content);
        }
    }
}
