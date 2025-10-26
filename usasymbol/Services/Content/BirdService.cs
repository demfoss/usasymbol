using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models.Content;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services.Content
{
    public interface IBirdService
    {
        Task<BirdContent?> GetBirdContentAsync(string state);
    }

    public class BirdService : IBirdService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly MarkdownPipeline _pipeline;
        private readonly IDeserializer _yamlDeserializer;

        public BirdService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;

            // Настройка Markdig с расширениями
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .Build();

            // Настройка YAML deserializer с underscore naming convention
            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }

        public async Task<BirdContent?> GetBirdContentAsync(string state)
        {
            var cacheKey = $"bird-content-{state}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, "Content", "states", state, "bird.md");

                if (!File.Exists(path))
                    return null;

                var fileContent = await File.ReadAllTextAsync(path);

                // Парсим документ
                var document = Markdown.Parse(fileContent, _pipeline);

                // Извлекаем YAML frontmatter
                var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

                if (yamlBlock == null)
                    return null;

                // Извлекаем YAML и контент отдельно
                var yamlStartIndex = yamlBlock.Span.Start;
                var yamlEndIndex = yamlBlock.Span.End + 1; // +1 для включения последнего символа

                // Получаем YAML строку
                var yamlString = fileContent.Substring(yamlStartIndex, yamlBlock.Span.Length);
                yamlString = yamlString.Trim('-', '\n', '\r').Trim();

                // Получаем Markdown контент (после YAML)
                var markdownContent = fileContent.Substring(yamlEndIndex).Trim();

                // Десериализуем YAML в временный объект
                var yamlData = _yamlDeserializer.Deserialize<YamlBirdData>(yamlString);

                if (yamlData == null)
                    return null;

                // Конвертируем Markdown в HTML
                var html = Markdown.ToHtml(markdownContent, _pipeline);

                // Создаем BirdContent объект
                var birdContent = new BirdContent
                {
                    Title = yamlData.title ?? string.Empty,
                    ScientificName = yamlData.scientific_name ?? string.Empty,
                    AdoptedYear = yamlData.adopted_year,
                    DatePublished = ParseDate(yamlData.date_published),
                    DateModified = ParseDate(yamlData.date_modified),
                    Author = yamlData.author ?? string.Empty,
                    Habitat = yamlData.habitat ?? string.Empty,
                    DistinctiveFeature = yamlData.distinctive_feature ?? string.Empty,
                    ConservationStatus = yamlData.conservation_status ?? string.Empty,
                    Size = yamlData.size ?? string.Empty,
                    Wingspan = yamlData.wingspan ?? string.Empty,
                    Weight = yamlData.weight ?? string.Empty,
                    Diet = yamlData.diet ?? string.Empty,
                    Nesting = yamlData.nesting ?? string.Empty,
                    Range = yamlData.range ?? string.Empty,
                    HtmlContent = html,
                    LastModified = File.GetLastWriteTime(path)
                };

                // Обработка списка shared_states
                if (yamlData.shared_states != null)
                {
                    birdContent.SharedStates = yamlData.shared_states;
                }

                // Обработка sources
                if (yamlData.sources != null)
                {
                    foreach (var source in yamlData.sources)
                    {
                        birdContent.Sources.Add(new BirdSource
                        {
                            Name = source.name ?? string.Empty,
                            Url = source.url ?? string.Empty,
                            Description = source.description ?? string.Empty
                        });
                    }
                }

                return birdContent;
            });
        }

        private DateTime? ParseDate(string? dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParse(dateString, out var date))
                return date;

            return null;
        }

        // Временный класс для десериализации YAML
        private class YamlBirdData
        {
            public string? title { get; set; }
            public string? scientific_name { get; set; }
            public int? adopted_year { get; set; }
            public string? date_published { get; set; }
            public string? date_modified { get; set; }
            public string? author { get; set; }
            public List<string>? shared_states { get; set; }
            public string? habitat { get; set; }
            public string? distinctive_feature { get; set; }
            public string? conservation_status { get; set; }
            public string? size { get; set; }
            public string? wingspan { get; set; }
            public string? weight { get; set; }
            public string? diet { get; set; }
            public string? nesting { get; set; }
            public string? range { get; set; }
            public List<YamlSource>? sources { get; set; }
        }

        private class YamlSource
        {
            public string? name { get; set; }
            public string? url { get; set; }
            public string? description { get; set; }
        }
    }
}