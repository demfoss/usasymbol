using System.Text.RegularExpressions;
using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline;

public sealed class PipelinePreflightService
{
    private static readonly Regex PlaceholderTokenRegex = new(@"\[[A-Z0-9_:-]+\]", RegexOptions.Compiled);

    public IReadOnlyList<string> Validate(ManualInputModel input)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(input.Category))
        {
            issues.Add("Category is missing.");
        }

        if (string.IsNullOrWhiteSpace(input.PrimaryKeyword))
        {
            issues.Add("Primary keyword is missing.");
        }

        if (string.IsNullOrWhiteSpace(input.TopicOrState))
        {
            issues.Add("Topic or state is missing.");
        }

        if (string.IsNullOrWhiteSpace(input.YamlSkeleton))
        {
            issues.Add("YAML skeleton is missing.");
            return issues;
        }

        if (PlaceholderTokenRegex.IsMatch(input.PrimaryKeyword))
        {
            issues.Add("Primary keyword still contains placeholder tokens.");
        }

        if (PlaceholderTokenRegex.IsMatch(input.TopicOrState))
        {
            issues.Add("Topic or state still contains placeholder tokens.");
        }

        if (!HasField(input.YamlSkeleton, "title") &&
            !HasNestedField(input.YamlSkeleton, "seo", "title") &&
            !HasField(input.YamlSkeleton, "seo_title"))
        {
            issues.Add("YAML skeleton is missing a title field.");
        }

        if (!HasField(input.YamlSkeleton, "seo_description") &&
            !HasNestedField(input.YamlSkeleton, "seo", "description") &&
            !HasField(input.YamlSkeleton, "description"))
        {
            issues.Add("YAML skeleton is missing an SEO description field.");
        }

        if (!HasField(input.YamlSkeleton, "intro") &&
            !HasField(input.YamlSkeleton, "intro_text") &&
            !HasNestedField(input.YamlSkeleton, "page", "intro"))
        {
            issues.Add("YAML skeleton is missing an intro/opening field.");
        }

        if (string.Equals(input.Category, "surnames", StringComparison.OrdinalIgnoreCase))
        {
            ValidateSurnamesSkeleton(input.YamlSkeleton, issues);
        }

        return issues;
    }

    private static void ValidateSurnamesSkeleton(string skeleton, List<string> issues)
    {
        var requiredFields = new[]
        {
            "type",
            "state",
            "state_slug",
            "population",
            "data_year",
            "surnames",
            "unique_surnames",
            "etymology_groups",
            "faq",
            "sources"
        };

        foreach (var field in requiredFields)
        {
            if (!HasField(skeleton, field))
            {
                issues.Add($"Surnames skeleton is missing `{field}`.");
            }
        }

        if (!HasNestedField(skeleton, "page", "h1"))
        {
            issues.Add("Surnames skeleton is missing `page.h1`.");
        }

        if (!HasNestedField(skeleton, "page", "heritage_title"))
        {
            issues.Add("Surnames skeleton is missing `page.heritage_title`.");
        }

        if (!HasNestedField(skeleton, "page", "heritage_body"))
        {
            issues.Add("Surnames skeleton is missing `page.heritage_body`.");
        }

        if (!HasNestedField(skeleton, "page", "fun_fact"))
        {
            issues.Add("Surnames skeleton is missing `page.fun_fact`.");
        }

    }

    private static bool HasField(string yamlText, string fieldName)
    {
        return Regex.IsMatch(
            yamlText,
            $@"(?m)^\s*{Regex.Escape(fieldName)}\s*:",
            RegexOptions.IgnoreCase);
    }

    private static bool HasNestedField(string yamlText, string parentField, string childField)
    {
        var pattern =
            $@"(?ms)^\s*{Regex.Escape(parentField)}\s*:\s*$.*?^\s{{2,}}{Regex.Escape(childField)}\s*:";

        return Regex.IsMatch(yamlText, pattern, RegexOptions.IgnoreCase);
    }

}
