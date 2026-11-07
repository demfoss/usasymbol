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

        private readonly StateSymbolQuizBuilder _stateQuizzes;

        public QuizService(IWebHostEnvironment env, IMemoryCache cache, StateSymbolQuizBuilder stateQuizzes)
        {
            _stateQuizzes = stateQuizzes;
            _env = env;
            _cache = cache;

            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        /// <summary>Gives every question a "read more" link to the state symbol page it is about, when the state is clear.</summary>
        private static void AddSymbolLinks(Quiz quiz)
        {
            foreach (var q in quiz.Questions)
            {
                if (!string.IsNullOrWhiteSpace(q.LinkUrl)) continue;
                var state = StateLinks.ResolveState(q.StateAbbreviation, q.CorrectAnswer, q.Question);
                if (state == null) continue;
                var link = StateLinks.For(quiz.Slug, state);
                q.LinkUrl = link.Url;
                q.LinkLabel = link.Label;
            }
        }

        /// <summary>Quiz of the day: one topic quiz per UTC day, the same for everyone.</summary>
        public string? GetDailyQuizSlug(DateTime? utcNow = null)
        {
            var topics = GetAll()
                .Where(q => !q.Slug.EndsWith("-state-symbols-quiz", StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => q.Slug, StringComparer.Ordinal)
                .ToList();
            if (topics.Count == 0) return null;
            var day = DateOnly.FromDateTime(utcNow ?? DateTime.UtcNow).DayNumber;
            return topics[day % topics.Count].Slug;
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
                        if (quiz != null) { AddSymbolLinks(quiz); quizzes.Add(quiz); }
                    }
                    catch
                    {

                    }
                }

                foreach (var state in StateSymbolQuizBuilder.AllStateNames)
                {
                    var stateQuiz = _stateQuizzes.BuildQuizForState(state);
                    if (stateQuiz != null) quizzes.Add(stateQuiz);
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

                var generated = _stateQuizzes.BuildQuiz(slug);
                if (generated != null) return Task.FromResult<Quiz?>(generated);

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
                    if (quiz != null) AddSymbolLinks(quiz);
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
