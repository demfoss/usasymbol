
using System.Text.Json.Serialization;

namespace usasymbol.Models
{
    public class Quiz
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();

        public QuizSeo? Seo { get; set; }

        public string Author { get; set; } = "USA Symbol Team";
        public DateTime DatePublished { get; set; } = DateTime.Now;
        public DateTime DateModified { get; set; } = DateTime.Now;

        public List<QuizQuestion> Questions { get; set; } = new();

        public List<QuizFaq>? Faq { get; set; }
        public List<QuizSource>? Sources { get; set; }

        public string Category { get; set; } = string.Empty;
        public string EstimatedTime { get; set; } = "5-10 minutes";
        public string PerfectScoreAchievement { get; set; } = "Flag Master 🏆";


        public QuizContent? Content { get; set; }
    }

    public class QuizContent
    {
        public List<QuizSection> Sections { get; set; } = new();
    }

    public class QuizSection
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public List<string>? Paragraphs { get; set; }
        public List<string>? List { get; set; }
        public List<QuizLink>? Links { get; set; }
    }

    public class QuizLink
    {
        public string Text { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
    }

    public class QuizSeo
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> IntroParagraphs { get; set; } = new();
    }

    public class QuizQuestion
    {
        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;

        [JsonPropertyName("state_abbreviation")]
        public string StateAbbreviation { get; set; } = string.Empty;

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public List<string> Choices { get; set; } = new();

        [JsonPropertyName("correct_answer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = string.Empty;
    }

    public class QuizFaq
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class QuizSource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class QuizAttempt
    {
        public int Id { get; set; }
        public string QuizSlug { get; set; } = "";
        public string Mode { get; set; } = "";
        public int Score { get; set; }
        public int QuestionCount { get; set; }
        public int TimeSpentSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class QuizResultDto
    {
        public string? Mode { get; set; }
        public int Score { get; set; }
        public int QuestionCount { get; set; }
        public int TimeSpentSeconds { get; set; }
    }
}