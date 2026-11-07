using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace USASymbol.Controllers;

/// <summary>
/// Reports the visitor's U.S. state for the "How does your state compare?" preselect.
/// Source order:
///   1. CDN headers (Cloudflare CF-IPCountry + CF-Region-Code) - nothing leaves the server.
///   2. Optional IP lookup, only when VisitorLocation:IpLookup is true in configuration. It sends the
///      visitor's IP to a third-party service (ipapi.co), so enable it only after updating the Privacy page.
/// Nothing is stored; lookups are cached in memory per IP for a day.
/// </summary>
[ApiController]
[Route("api/visitor/state")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VisitorLocationController : ControllerBase
{
    private static readonly Regex Postal = new("^[A-Z]{2}$", RegexOptions.Compiled);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };

    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public VisitorLocationController(IMemoryCache cache, IConfiguration config, IWebHostEnvironment env)
    {
        _cache = cache;
        _config = config;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? debug, CancellationToken cancellationToken)
    {
        // Local testing without a CDN: /api/visitor/state?debug=OH (Development only).
        if (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(debug) && Postal.IsMatch(debug.ToUpperInvariant()))
            return Ok(new { postal = debug.ToUpperInvariant() });

        var country = Request.Headers["CF-IPCountry"].ToString().Trim().ToUpperInvariant();
        var region = Request.Headers["CF-Region-Code"].ToString().Trim().ToUpperInvariant();
        if (country.Length > 0)
            return Ok(new { postal = country == "US" && Postal.IsMatch(region) ? region : null });

        if (!_config.GetValue("VisitorLocation:IpLookup", false))
            return Ok(new { postal = (string?)null });

        var ip = HttpContext.Connection.RemoteIpAddress;
        if (ip is null || IPAddress.IsLoopback(ip) || IsPrivate(ip))
            return Ok(new { postal = (string?)null });

        var key = "visitor-state:" + ip;
        if (!_cache.TryGetValue(key, out string? postal))
        {
            postal = await LookupAsync(ip, cancellationToken);
            _cache.Set(key, postal, TimeSpan.FromHours(24));
        }
        return Ok(new { postal });
    }

    private static async Task<string?> LookupAsync(IPAddress ip, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync($"https://ipapi.co/{ip}/json/", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            var country = root.TryGetProperty("country_code", out var c) ? c.GetString() : null;
            var region = root.TryGetProperty("region_code", out var r) ? r.GetString()?.ToUpperInvariant() : null;
            return country == "US" && region is not null && Postal.IsMatch(region) ? region : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        var b = ip.GetAddressBytes();
        return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
               (b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254));
    }
}
