using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using USASymbol.Data;
using USASymbol.Models;
using USASymbol.Services;

namespace USASymbol.Controllers;

[ApiController]
[Route("api/rankings/{category}/{slug}/engagement")]
[EnableRateLimiting("ranking-engagement")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RankingEngagementController : ControllerBase
{
    private const string VisitorCookie = "ranking_reactor";
    private static readonly Regex PathPart = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex VisitorToken = new("^[A-Za-z0-9_-]{40,64}$", RegexOptions.Compiled);
    private static readonly HashSet<string> Reactions = new(StringComparer.Ordinal)
    {
        "surprising", "expected", "useful"
    };

    private readonly AppDbContext _db;
    private readonly IRankingsContentService _rankings;

    public RankingEngagementController(AppDbContext db, IRankingsContentService rankings)
    {
        _db = db;
        _rankings = rankings;
    }

    [HttpPost("view")]
    public async Task<IActionResult> RecordView(CancellationToken cancellationToken)
    {
        var path = await ResolvePathAsync(cancellationToken);
        if (path is null) return NotFound();
        if (!IsSameOriginScriptRequest()) return BadRequest();

        var visitorHash = GetVisitorHash();
        var now = DateTime.UtcNow;
        var row = await UpsertAsync(path.Value.Category, path.Value.Slug, visitorHash, now, (engagement, isNew) =>
        {
            engagement.ViewCount = isNew ? 1 : engagement.ViewCount + 1;
            engagement.FirstViewedAt = isNew ? now : engagement.FirstViewedAt;
            engagement.LastViewedAt = now;
        }, cancellationToken);

        return Ok(await BuildResultAsync(path.Value.Category, path.Value.Slug, row.Reaction, cancellationToken));
    }

    [HttpPost("reaction")]
    [RequestSizeLimit(1024)]
    public async Task<IActionResult> RecordReaction(
        [FromBody] RankingReactionRequest request,
        CancellationToken cancellationToken)
    {
        var path = await ResolvePathAsync(cancellationToken);
        if (path is null) return NotFound();
        if (!IsSameOriginScriptRequest()) return BadRequest();

        var reaction = request.Reaction?.Trim().ToLowerInvariant();
        if (reaction is null || !Reactions.Contains(reaction))
            return BadRequest(new { error = "Invalid reaction." });

        var visitorHash = GetVisitorHash();
        var now = DateTime.UtcNow;
        var row = await UpsertAsync(path.Value.Category, path.Value.Slug, visitorHash, now, (engagement, isNew) =>
        {
            if (isNew)
            {
                engagement.ViewCount = 1;
                engagement.FirstViewedAt = now;
                engagement.LastViewedAt = now;
            }

            // One vote per visitor: an existing reaction is never overwritten.
            if (engagement.Reaction is not null) return;

            engagement.FirstReactedAt = now;
            engagement.Reaction = reaction;
            engagement.LastReactedAt = now;
        }, cancellationToken);

        return Ok(await BuildResultAsync(path.Value.Category, path.Value.Slug, row.Reaction, cancellationToken));
    }

    private async Task<object> BuildResultAsync(string category, string slug, string? mine, CancellationToken cancellationToken)
    {
        var grouped = await _db.RankingEngagements
            .AsNoTracking()
            .Where(item => item.Category == category && item.RankingSlug == slug && item.Reaction != null)
            .GroupBy(item => item.Reaction!)
            .Select(group => new { Reaction = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int Count(string key) => grouped.FirstOrDefault(item => item.Reaction == key)?.Count ?? 0;
        var counts = new Dictionary<string, int>
        {
            ["surprising"] = Count("surprising"),
            ["expected"] = Count("expected"),
            ["useful"] = Count("useful")
        };
        return new { reaction = mine, counts, total = counts.Values.Sum() };
    }

    private async Task<(string Category, string Slug)?> ResolvePathAsync(CancellationToken cancellationToken)
    {
        var category = RouteData.Values["category"]?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
        var slug = RouteData.Values["slug"]?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
        if (category.Length > 80 || slug.Length > 180 || !PathPart.IsMatch(category) || !PathPart.IsMatch(slug))
            return null;

        var content = await _rankings.GetContentAsync(category, slug);
        cancellationToken.ThrowIfCancellationRequested();
        return content is null ? null : (category, slug);
    }

    private bool IsSameOriginScriptRequest() => string.Equals(
        Request.Headers["X-Requested-With"].ToString(),
        "XMLHttpRequest",
        StringComparison.Ordinal);

    private string GetVisitorHash()
    {
        var token = Request.Cookies[VisitorCookie];
        if (string.IsNullOrWhiteSpace(token) || !VisitorToken.IsMatch(token))
        {
            token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            Response.Cookies.Append(VisitorCookie, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(180),
                Path = "/"
            });
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private async Task<RankingEngagement> UpsertAsync(
        string category,
        string slug,
        string visitorHash,
        DateTime now,
        Action<RankingEngagement, bool> update,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var row = await _db.RankingEngagements.SingleOrDefaultAsync(
                item => item.Category == category &&
                        item.RankingSlug == slug &&
                        item.VisitorHash == visitorHash,
                cancellationToken);
            var isNew = row is null;

            row ??= new RankingEngagement
            {
                Category = category,
                RankingSlug = slug,
                VisitorHash = visitorHash,
                CreatedAt = now
            };

            update(row, isNew);
            row.UpdatedAt = now;
            if (isNew) _db.RankingEngagements.Add(row);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return row;
            }
            catch (DbUpdateException) when (isNew && attempt == 0)
            {
                _db.ChangeTracker.Clear();
            }
        }

        throw new DbUpdateException("Could not update ranking engagement after a concurrent insert.");
    }
}
