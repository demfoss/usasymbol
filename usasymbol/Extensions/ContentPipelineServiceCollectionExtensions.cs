using Microsoft.Extensions.Options;
using USASymbol.Models.ContentPipeline;
using USASymbol.Services.ContentPipeline;
using USASymbol.Services.ContentPipeline.Runners;
using USASymbol.Services.ContentPipeline.Utils;

namespace USASymbol.Extensions;

public static class ContentPipelineServiceCollectionExtensions
{
    public static IServiceCollection AddContentPipeline(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var requireClaudeApiKey = environment.IsDevelopment()
            || configuration.GetValue<bool>("ContentPipeline:EnabledOutsideDevelopment");

        services
            .AddOptions<ContentPipelineOptions>()
            .Bind(configuration.GetSection(ContentPipelineOptions.SectionName))
            .ValidateOnStart();
        services
            .AddOptions<ContentPipelineClaudeOptions>()
            .Bind(configuration.GetSection(ContentPipelineClaudeOptions.SectionName))
            .Validate(
                options => !requireClaudeApiKey || !string.IsNullOrWhiteSpace(options.ApiKey),
                "AiPipeline:Claude:ApiKey is required.")
            .ValidateOnStart();

        services.AddScoped<ContentPipelineAccessService>();
        services.AddSingleton<PipelineJobTrackerService>();
        services.AddScoped<PipelineExampleService>();
        services.AddScoped<CategoryConfigService>();
        services.AddScoped<BuildPacketService>();
        services.AddScoped<ContentIndexService>();
        services.AddScoped<InternalLinksService>();
        services.AddScoped<PatternMemoryService>();
        services.AddScoped<PromptTemplateRendererService>();
        services.AddScoped<PipelineResponseParserService>();
        services.AddScoped<PipelineOutputService>();
        services.AddScoped<PipelinePreflightService>();
        services.AddHttpClient<AnthropicContentClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ContentPipelineClaudeOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.EndsWith("/") ? options.BaseUrl : $"{options.BaseUrl}/");
            client.Timeout = TimeSpan.FromMinutes(4);
        });
        services.AddScoped<SimilarityService>();
        services.AddScoped<YamlValidatorService>();
        services.AddScoped<WriterRunner>();
        services.AddScoped<FinisherRunner>();
        services.AddScoped<PipelineRunner>();
        services.AddScoped<TextFingerprintUtility>();
        services.AddScoped<SlugUtility>();
        services.AddScoped<FileScanUtility>();

        return services;
    }
}
