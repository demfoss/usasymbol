using Microsoft.Extensions.Caching.Memory;
using usasymbol.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace usasymbol.Services
{
    public class QuizService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly IDeserializer _deserializer;

        private string QuizzesFolder => Path.Combine(_env.ContentRootPath, "content", "quizzes");

        public QuizService(IWebHostEnvironment env, IMemoryCache cache)
        {
            _env = env;
            _cache = cache;

            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public List<Quiz> GetAll() =>
            GetAllAsync().GetAwaiter().GetResult();

        public Quiz? GetBySlug(string slug) =>
            GetBySlugAsync(slug).GetAwaiter().GetResult();

        public Task<List<Quiz>> GetAllAsync()
        {
            var cacheKey = "quizzes-all";

            return _cache.GetOrCreateAsync<List<Quiz>>(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(6);

                var quizzes = new List<Quiz>();
                if (!Directory.Exists(QuizzesFolder)) return Task.FromResult(quizzes);

                foreach (var file in Directory.GetFiles(QuizzesFolder, "*.yaml"))
                {
                    try
                    {
                        var yaml = File.ReadAllText(file);
                        var quiz = _deserializer.Deserialize<Quiz>(yaml);
                        if (quiz != null) quizzes.Add(quiz);
                    }
                    catch
                    {

                    }
                }

                return Task.FromResult(quizzes);
            })!;
        }

        public Task<Quiz?> GetBySlugAsync(string slug)
        {
            var cacheKey = $"quiz-{slug}";

            return _cache.GetOrCreateAsync<Quiz?>(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                if (!Directory.Exists(QuizzesFolder)) return Task.FromResult<Quiz?>(null);

                var path = Path.Combine(QuizzesFolder, $"{slug}.yaml");
                if (!File.Exists(path))
                {
                    var file = Directory.GetFiles(QuizzesFolder, "*.yaml")
                        .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                            .Equals(slug, StringComparison.OrdinalIgnoreCase));

                    if (file == null) return Task.FromResult<Quiz?>(null);
                    path = file;
                }

                try
                {
                    var yaml = File.ReadAllText(path);
                    var quiz = _deserializer.Deserialize<Quiz>(yaml);
                    return Task.FromResult<Quiz?>(quiz);
                }
                catch
                {
                    return Task.FromResult<Quiz?>(null);
                }
            })!;
        }
    }
}
