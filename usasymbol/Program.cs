using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using USASymbol.Options;
using usasymbol.Services;
using usasymbol.Services.Interface;
using USASymbol.Data;
using USASymbol.Services;
using USASymbol.Services.Images;
using USASymbol.Services.Interface;
using USASymbol.Services.ContentPipeline;
using USASymbol.Services.Yaml;
using USASymbol.Extensions;
using YamlParse = USASymbol.Services.YamlParse;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services
    .AddOptions<BunnyOptions>()
    .Bind(builder.Configuration.GetSection(BunnyOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.CdnBaseUrl), "Bunny:CdnBaseUrl is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.LocalImagesBasePath), "Bunny:LocalImagesBasePath is required.")
    .ValidateOnStart();
builder.Services.AddContentPipeline(builder.Configuration, builder.Environment);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IImagePathNormalizer, ImagePathNormalizer>();
builder.Services.AddSingleton<IImageUrlService, ImageUrlService>();
builder.Services.AddHttpClient<BunnyImageSyncService>();
builder.Services.AddTransient<BunnyImageSyncRunner>();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IStateService, StateService>();
builder.Services.AddScoped<IStateHubContentService, StateHubContentService>();
builder.Services.AddScoped<ISymbolCanonicalService, SymbolCanonicalService>();

builder.Services.AddScoped<IMottoService, MottoService>();
builder.Services.AddScoped<INicknameService, NicknameService>();
builder.Services.AddScoped<IFlowerService, FlowerService>();
builder.Services.AddScoped<IFlagService, FlagService>();
builder.Services.AddScoped<ITreeService, TreeService>();
builder.Services.AddScoped<IMammalService, MammalService>();
builder.Services.AddScoped<IColorService, ColorService>();
builder.Services.AddScoped<IDinosaurService, DinosaurService>();
builder.Services.AddScoped<IBeverageService, BeverageService>();
builder.Services.AddScoped<ILicensePlateService, LicensePlateService>();
builder.Services.AddScoped<ISealService, SealService>();
builder.Services.AddHostedService<IndexNowBackgroundService>();


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IBorderService, BorderService>();
builder.Services.AddScoped<ISurnamesService, SurnamesService>();
builder.Services.AddScoped<IRankingsContentService, RankingsContentService>();
builder.Services.AddScoped<IListingsContentService, ListingsContentService>();
builder.Services.AddScoped<ICollectionsContentService, CollectionsContentService>();
builder.Services.AddScoped<ILatestContentRailService, LatestContentRailService>();




builder.Services.AddScoped<IBirdService, BirdService>();
builder.Services.AddScoped<IFirearmService, FirearmService>();
builder.Services.AddScoped<IComparisonStatsService, ComparisonStatsService>();
builder.Services.AddScoped<IComparisonService, ComparisonService>();
builder.Services.AddSingleton<QuizService>();



builder.Services.AddScoped<ISymbolService, YamlParse>();
builder.Services.AddSingleton<YamlContentLoader>();
builder.Services.AddScoped<SitemapBuilder>();
builder.Services.AddScoped<SitemapCacheService>();
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var isImageSyncCommand = args.Any(x =>
    string.Equals(x, "--sync-bunny-images", StringComparison.OrdinalIgnoreCase));

var app = builder.Build();

if (isImageSyncCommand)
{
    using var scope = app.Services.CreateScope();
    var runner = scope.ServiceProvider.GetRequiredService<BunnyImageSyncRunner>();
    var exitCode = await runner.RunAsync(CancellationToken.None);
    Environment.ExitCode = exitCode;
    return;
}

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    MarkdownExtensions.ResetAutoLinkContext(context.Request.Path.Value);
    await next();
});





app.MapGet("/sitemap.xml", async (SitemapCacheService cache) =>
{
    var xml = await cache.GetSitemapIndexAsync();
    return Results.Content(xml, "application/xml");
});

app.MapGet("/sitemap-main.xml", async (SitemapCacheService cache) =>
{
    var xml = await cache.GetMainSitemapAsync();
    return Results.Content(xml, "application/xml");
});

app.MapGet("/sitemap-compare.xml", async (SitemapCacheService cache) =>
{
    var xml = await cache.GetCompareSitemapAsync();
    return Results.Content(xml, "application/xml");
});





app.Use(async (context, next) =>
{
    var request = context.Request;
    var path = request.Path.ToString();
    var isFileRequest = Path.HasExtension(path);
    var forceHttps = !app.Environment.IsDevelopment();
    var normalizedHost = request.Host.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
        ? request.Host.Host[4..]
        : request.Host.Host;
    var normalizedPath = path;

    if (normalizedPath.Length > 1 && normalizedPath.EndsWith("/"))
    {
        normalizedPath = normalizedPath.TrimEnd('/');
    }

    if (!isFileRequest && normalizedPath != normalizedPath.ToLowerInvariant())
    {
        normalizedPath = normalizedPath.ToLowerInvariant();
    }

    var needsRedirect =
        (forceHttps && !request.IsHttps) ||
        !string.Equals(request.Host.Host, normalizedHost, StringComparison.Ordinal) ||
        !string.Equals(path, normalizedPath, StringComparison.Ordinal);

    if (!needsRedirect)
    {
        await next();
        return;
    }

    var redirectHost = request.Host.Port.HasValue
        ? new HostString(normalizedHost, request.Host.Port.Value)
        : new HostString(normalizedHost);
    var redirectUrl = UriHelper.BuildAbsolute(
        forceHttps ? "https" : request.Scheme,
        redirectHost,
        PathString.Empty,
        new PathString(normalizedPath),
        request.QueryString);

    context.Response.Redirect(redirectUrl, permanent: true);
});


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(context);
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseResponseCompression();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/print"))
        {
            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        }

        await next();
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            var path = ctx.Context.Request.Path;
            var cacheControl = path.StartsWithSegments("/ads.txt")
                ? "public,max-age=3600"
                : "public,max-age=31536000,immutable";

            ctx.Context.Response.Headers.Append("Cache-Control", cacheControl);
        }
    });
}
else
{
    app.UseStaticFiles();
}

app.UseResponseCaching();
app.UseRouting();

bool pipelineEnabled;
using (var scope = app.Services.CreateScope())
{
    var contentPipelineAccess = scope.ServiceProvider.GetRequiredService<ContentPipelineAccessService>();
    pipelineEnabled = contentPipelineAccess.IsEnabled();
}

app.Use(async (context, next) =>
{
    if (!pipelineEnabled && context.Request.Path.StartsWithSegments("/pipeline"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapControllerRoute(
    name: "quizzes",
    pattern: "quizzes/{slug}",
    defaults: new { controller = "Quiz", action = "Details" });

app.MapControllerRoute(
    name: "quizzesRoot",
    pattern: "quizzes",
    defaults: new { controller = "Quiz", action = "Index" });


app.MapControllerRoute(
    name: "collectionsDetail",
    pattern: "collections/{group}/{slug}",
    defaults: new { controller = "Collections", action = "Detail" });

app.MapControllerRoute(
    name: "collectionsGroup",
    pattern: "collections/{group}",
    defaults: new { controller = "Collections", action = "Group" });

app.MapControllerRoute(
    name: "collectionsHub",
    pattern: "collections",
    defaults: new { controller = "Collections", action = "Index" });


app.MapControllerRoute(
    name: "rankingsDetail",
    pattern: "rankings/{category}/{slug}",
    defaults: new { controller = "Rankings", action = "Detail" });

app.MapControllerRoute(
    name: "rankingsCategory",
    pattern: "rankings/{category}",
    defaults: new { controller = "Rankings", action = "Category" });

app.MapControllerRoute(
    name: "rankingsHub",
    pattern: "rankings",
    defaults: new { controller = "Rankings", action = "Index" });


app.MapControllerRoute(
    name: "state",
    pattern: "states/{slug}",
    defaults: new { controller = "State", action = "Index" });

app.MapControllerRoute(
    name: "symbol",
    pattern: "states/{stateSlug}/{symbolType}",
    defaults: new { controller = "Symbol", action = "Detail" });

app.MapControllerRoute(
    name: "symbolListing",
    pattern: "symbols/{type}",
    defaults: new { controller = "Symbol", action = "Listing" });


app.MapControllerRoute(
    name: "compareHub",
    pattern: "compare-states",
    defaults: new { controller = "Compare", action = "Hub" });

app.MapControllerRoute(
    name: "compareMetric",
    pattern: "compare/{pair}/{metric}",
    defaults: new { controller = "Compare", action = "Metric" });

app.MapControllerRoute(
    name: "compareOverview",
    pattern: "compare/{pair}",
    defaults: new { controller = "Compare", action = "Overview" });

app.MapControllerRoute(
    name: "guides",
    pattern: "guides",
    defaults: new { controller = "StateGuides", action = "Index" });

app.MapControllerRoute(
    name: "stateBorders",
    pattern: "states/{stateSlug}/borders",
    defaults: new { controller = "StateGuides", action = "Borders" });

app.Run();
