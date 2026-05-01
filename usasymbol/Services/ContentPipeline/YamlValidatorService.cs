using System.Text.RegularExpressions;
using USASymbol.Models.ContentPipeline;
using YamlDotNet.RepresentationModel;

namespace USASymbol.Services.ContentPipeline;

public sealed class YamlValidatorService
{
    public Task<PipelineCheckReportModel> RunChecksAsync(
        PromptPayloadModel payload,
        string? yamlText,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(yamlText))
        {
            return Task.FromResult(new PipelineCheckReportModel
            {
                WasRun = false,
                IsSuccess = false,
                Summary = "Checks are ready but were skipped because no generated YAML was provided yet."
            });
        }

        var issues = new List<PipelineCheckIssueModel>();
        YamlMappingNode? root = null;

        try
        {
            using var reader = new StringReader(yamlText);
            var stream = new YamlStream();
            stream.Load(reader);

            if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlMappingNode mapping)
            {
                issues.Add(Issue("yaml.invalid", "error", "YAML structure is empty or not a mapping.", "structure"));
            }
            else
            {
                root = mapping;
            }
        }
        catch (Exception ex)
        {
            issues.Add(Issue("yaml.invalid", "error", ex.Message, "structure"));
        }

        if (root is not null)
        {
            var isSurnames = string.Equals(GetScalarByPath(root, "type"), "surnames", StringComparison.OrdinalIgnoreCase);
            var titleField = isSurnames
                ? GetFirstScalar(root, "seo.title", "title", "seo_title", "seo.title_text")
                : GetFirstScalar(root, "title", "seo.title", "seo_title", "seo.title_text");
            var descriptionField = isSurnames
                ? GetFirstScalar(root, "seo.description", "seo_description", "description")
                : GetFirstScalar(root, "seo_description", "seo.description", "description");
            var introField = GetFirstScalar(root, "intro", "intro_text", "page.intro");
            var bodyText = CollectText(root);
            var faqItems = CountFaqItems(root);
            var linkCount = Regex.Matches(yamlText, @"\[[^\]]+\]\([^)]+\)").Count;
            ValidateSurnamesFields(root, issues);

            if (string.IsNullOrWhiteSpace(titleField.Value))
            {
                issues.Add(Issue("title.empty", "error", "Title is empty.", titleField.Path));
            }

            if (string.IsNullOrWhiteSpace(descriptionField.Value))
            {
                issues.Add(Issue("description.empty", "error", "SEO description is empty.", descriptionField.Path));
            }

            if (!string.IsNullOrWhiteSpace(titleField.Value) &&
                !ContainsNaturalVariant(titleField.Value, payload.PrimaryKeyword))
            {
                issues.Add(Issue("keyword.title", "error", "Primary keyword is missing from title.", titleField.Path));
            }

            if (!string.IsNullOrWhiteSpace(descriptionField.Value) &&
                !ContainsNaturalVariant(descriptionField.Value, payload.PrimaryKeyword))
            {
                issues.Add(Issue("keyword.description", "error", "Primary keyword or a close variant is missing from SEO description.", descriptionField.Path));
            }

            var h1Field = GetFirstScalar(root, "h1", "page.h1");
            if (!string.IsNullOrWhiteSpace(titleField.Value) &&
                !string.IsNullOrWhiteSpace(h1Field.Value) &&
                string.Equals(NormalizeComparableText(titleField.Value), NormalizeComparableText(h1Field.Value), StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Issue("title.duplicate_h1", "warning", "SEO title duplicates the H1. Use a distinct title angle.", titleField.Path));
            }

            var introAndBody = string.Join(
                "\n",
                new[] { introField.Value, bodyText }.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!ContainsNaturalVariant(introAndBody, payload.PrimaryKeyword))
            {
                issues.Add(Issue("keyword.body", "error", "Primary keyword or a close variant is missing from intro/body.", introField.Path));
            }

            if (payload.InternalLinks.Count > 0 && linkCount == 0)
            {
                issues.Add(Issue("links.missing", "warning", "Internal links look relevant but none were inserted.", "body"));
            }

            if (faqItems > 0 && faqItems < 2)
            {
                issues.Add(Issue("faq.weak", "warning", "FAQ exists but looks too thin to justify its section.", "faq"));
            }

            if (bodyText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 2200)
            {
                issues.Add(Issue("length.bloated", "warning", "Article looks bloated for this compact pipeline.", "body"));
            }

        }

        return Task.FromResult(new PipelineCheckReportModel
        {
            WasRun = true,
            IsSuccess = issues.All(x => !string.Equals(x.Severity, "error", StringComparison.OrdinalIgnoreCase)),
            Summary = issues.Count == 0
                ? "Checks passed."
                : $"Checks found {issues.Count} issue(s).",
            Issues = issues
        });
    }

    private static PipelineCheckIssueModel Issue(string code, string severity, string message, string targetFragment)
    {
        return new PipelineCheckIssueModel
        {
            Code = code,
            Severity = severity,
            Message = message,
            TargetFragment = targetFragment
        };
    }

    private static (string Path, string Value) GetFirstScalar(YamlMappingNode node, params string[] paths)
    {
        foreach (var path in paths)
        {
            var value = GetScalarByPath(node, path);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return (path, value);
            }
        }

        return (paths.FirstOrDefault() ?? string.Empty, string.Empty);
    }

    private static string GetScalarByPath(YamlMappingNode node, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        YamlNode? current = node;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is not YamlMappingNode mapping ||
                !mapping.Children.TryGetValue(new YamlScalarNode(segment), out current))
            {
                return string.Empty;
            }
        }

        return (current as YamlScalarNode)?.Value?.Trim() ?? string.Empty;
    }

    private static int CountFaqItems(YamlMappingNode node)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode("faq"), out var faqNode) || faqNode is not YamlSequenceNode sequence)
        {
            return 0;
        }

        return sequence.Children.Count;
    }

    private static void ValidateSurnamesFields(YamlMappingNode root, List<PipelineCheckIssueModel> issues)
    {
        if (!string.Equals(GetScalarByPath(root, "type"), "surnames", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ValidateOrigins(root, "surnames", issues);
        ValidateOrigins(root, "unique_surnames", issues);
        ValidateEtymologyIcons(root, issues);
        ValidateSurnamesStructure(root, issues);
    }

    private static void ValidateSurnamesStructure(YamlMappingNode root, List<PipelineCheckIssueModel> issues)
    {
        RequireScalar(root, "state", issues);
        RequireScalar(root, "state_slug", issues);
        RequireScalar(root, "population", issues);
        RequireScalar(root, "data_year", issues);
        RequireScalar(root, "seo.title", issues);
        RequireScalar(root, "seo.description", issues);
        RequireScalar(root, "page.h1", issues);
        RequireScalar(root, "page.intro", issues);
        RequireScalar(root, "page.heritage_title", issues);
        RequireScalar(root, "page.heritage_body", issues);
        RequireScalar(root, "page.fun_fact", issues);

        ValidateSequenceCount(root, "surnames", 20, 20, issues);
        ValidateSequenceCount(root, "unique_surnames", 5, null, issues);
        ValidateSequenceCount(root, "etymology_groups", 3, 3, issues);
        ValidateSequenceCount(root, "faq", 2, null, issues);
        ValidateSequenceCount(root, "sources", 3, null, issues);

        ValidateEntryFields(root, "surnames", ["rank", "name", "count", "ratio", "origin", "type", "meaning"], issues);
        ValidateEntryFields(root, "unique_surnames", ["name", "state_rank", "national_rank", "origin", "why"], issues);
        ValidateEntryFields(root, "etymology_groups", ["type", "icon", "description", "examples"], issues);
        ValidateEntryFields(root, "faq", ["question", "answer"], issues);
        ValidateEntryFields(root, "sources", ["name", "url", "description"], issues);
    }

    private static void RequireScalar(YamlMappingNode root, string path, List<PipelineCheckIssueModel> issues)
    {
        if (string.IsNullOrWhiteSpace(GetScalarByPath(root, path)))
        {
            issues.Add(Issue("surnames.structure", "error", $"Surnames YAML is missing `{path}`.", path));
        }
    }

    private static void ValidateSequenceCount(
        YamlMappingNode root,
        string key,
        int minCount,
        int? exactCount,
        List<PipelineCheckIssueModel> issues)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode(key), out var node) ||
            node is not YamlSequenceNode sequence)
        {
            issues.Add(Issue("surnames.structure", "error", $"Surnames YAML is missing `{key}` list.", key));
            return;
        }

        var count = sequence.Children.Count;
        if (exactCount.HasValue && count != exactCount.Value)
        {
            issues.Add(Issue("surnames.structure", "error", $"`{key}` must contain exactly {exactCount.Value} item(s); found {count}.", key));
            return;
        }

        if (!exactCount.HasValue && count < minCount)
        {
            issues.Add(Issue("surnames.structure", "error", $"`{key}` must contain at least {minCount} item(s); found {count}.", key));
        }
    }

    private static void ValidateEntryFields(
        YamlMappingNode root,
        string key,
        IReadOnlyList<string> requiredFields,
        List<PipelineCheckIssueModel> issues)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode(key), out var node) ||
            node is not YamlSequenceNode sequence)
        {
            return;
        }

        var index = 0;
        foreach (var child in sequence.Children.OfType<YamlMappingNode>())
        {
            index++;
            foreach (var field in requiredFields)
            {
                if (!child.Children.ContainsKey(new YamlScalarNode(field)))
                {
                    issues.Add(Issue("surnames.structure", "error", $"`{key}` item {index} is missing `{field}`.", $"{key}.{field}"));
                }
            }
        }
    }

    private static void ValidateOrigins(
        YamlMappingNode root,
        string sequenceKey,
        List<PipelineCheckIssueModel> issues)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode(sequenceKey), out var node) ||
            node is not YamlSequenceNode sequence)
        {
            return;
        }

        foreach (var child in sequence.Children.OfType<YamlMappingNode>())
        {
            var origin = GetScalarByPath(child, "origin");
            if (string.IsNullOrWhiteSpace(origin))
            {
                continue;
            }

            if (!IsSingleLowercaseToken(origin))
            {
                issues.Add(Issue(
                    "surnames.origin",
                    "error",
                    $"Origin `{origin}` must be one lowercase origin label only. No slashes, spaces, capitals, or qualifiers.",
                    $"{sequenceKey}.origin"));
            }
        }
    }

    private static bool IsSingleLowercaseToken(string value)
    {
        return Regex.IsMatch(value, "^[a-z][a-z0-9]*$");
    }

    private static void ValidateEtymologyIcons(YamlMappingNode root, List<PipelineCheckIssueModel> issues)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode("etymology_groups"), out var node) ||
            node is not YamlSequenceNode sequence)
        {
            return;
        }

        foreach (var child in sequence.Children.OfType<YamlMappingNode>())
        {
            var icon = GetScalarByPath(child, "icon");
            if (!string.IsNullOrWhiteSpace(icon) &&
                !icon.StartsWith("fa-", StringComparison.OrdinalIgnoreCase) &&
                !icon.StartsWith("fas ", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Issue(
                    "surnames.icon",
                    "error",
                    $"Icon `{icon}` is not a Font Awesome class. Use classes like `fa-solid fa-hammer`.",
                    "etymology_groups.icon"));
            }
        }
    }

    private static string CollectText(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode scalar => scalar.Value ?? string.Empty,
            YamlSequenceNode sequence => string.Join("\n", sequence.Children.Select(CollectText)),
            YamlMappingNode mapping => string.Join("\n", mapping.Children.Values.Select(CollectText)),
            _ => string.Empty
        };
    }

    private static bool ContainsNaturalVariant(string text, string keyword)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword))
        {
            return false;
        }

        if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var parts = keyword
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 2)
            .ToArray();

        return parts.Length > 0 && parts.Count(part => text.Contains(part, StringComparison.OrdinalIgnoreCase)) >= Math.Min(2, parts.Length);
    }

    private static string NormalizeComparableText(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

}
