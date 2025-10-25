using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class MarkdownService : IMarkdownService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly MarkdownPipeline _pipeline;
        private readonly IDeserializer _yamlDeserializer;

        public MarkdownService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;

            // Настройка Markdig с расширениями
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .Build();

            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<SymbolContent?> GetSymbolContentAsync(string state, string symbolType)
        {
            var cacheKey = $"content-{state}-{symbolType}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, "Content", "states", state, $"{symbolType}.md");

                if (!File.Exists(path))
                    return null;

                var markdown = await File.ReadAllTextAsync(path);

                // Парсим документ
                var document = Markdown.Parse(markdown, _pipeline);

                // Извлекаем YAML frontmatter
                var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
                Dictionary<string, string>? metadata = null;

                if (yamlBlock != null)
                {
                    var yaml = markdown.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
                    var yamlContent = yaml.Trim('-', '\n', '\r');

                    try
                    {
                        metadata = _yamlDeserializer.Deserialize<Dictionary<string, string>>(yamlContent);
                    }
                    catch
                    {
                        metadata = new Dictionary<string, string>();
                    }
                }

                // Конвертируем Markdown в HTML
                var html = Markdown.ToHtml(markdown, _pipeline);

                return new SymbolContent
                {
                    Html = html,
                    Metadata = metadata ?? new Dictionary<string, string>(),
                    LastModified = File.GetLastWriteTime(path)
                };
            });
        }
    }
}
