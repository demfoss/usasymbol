using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace USASymbol.Services.ContentPipeline;

public sealed class PipelineResponseParserService
{
    public string ExtractYaml(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return string.Empty;
        }

        var text = responseText.Trim();
        var fencedYaml = ExtractFencedYaml(text);
        if (!string.IsNullOrWhiteSpace(fencedYaml))
        {
            return fencedYaml;
        }

        text = StripCodeFences(text);

        var finalYamlMatch = Regex.Match(
            text,
            @"^\s*(?:1\.\s*)?Final YAML\s*:?\s*(?<yaml>[\s\S]+?)(?:^\s*(?:2\.\s*)?Short notes\s*:?\s*$|$)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        if (finalYamlMatch.Success)
        {
            return finalYamlMatch.Groups["yaml"].Value.Trim();
        }

        var patchedYamlMatch = Regex.Match(
            text,
            @"^\s*(?:-\s*)?patched YAML only\s*:?\s*(?<yaml>[\s\S]+)$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        if (patchedYamlMatch.Success)
        {
            return patchedYamlMatch.Groups["yaml"].Value.Trim();
        }

        var extractedYaml = TryExtractValidYamlBlock(text);
        return string.IsNullOrWhiteSpace(extractedYaml)
            ? text.Trim()
            : extractedYaml;
    }

    public string ExtractNotes(string responseText)
    {
        return string.Empty;
    }

    private static string StripCodeFences(string text)
    {
        text = Regex.Replace(text, @"^\s*```[a-zA-Z0-9_-]*\s*", string.Empty);
        text = Regex.Replace(text, @"\s*```\s*$", string.Empty);
        return text.Trim();
    }

    private static string ExtractFencedYaml(string text)
    {
        var yamlFenceMatch = Regex.Match(
            text,
            @"```(?:yaml|yml)?\s*(?<yaml>[\s\S]*?)```",
            RegexOptions.IgnoreCase);

        return yamlFenceMatch.Success
            ? yamlFenceMatch.Groups["yaml"].Value.Trim()
            : string.Empty;
    }

    private static string TryExtractValidYamlBlock(string text)
    {
        if (IsValidYamlMapping(text))
        {
            return text.Trim();
        }

        var lines = text
            .Replace("\r\n", "\n")
            .Split('\n');

        var candidateStarts = lines
            .Select((line, index) => new { line, index })
            .Where(x => Regex.IsMatch(x.line, @"^[A-Za-z0-9_-]+\s*:\s*"))
            .Select(x => x.index)
            .ToArray();

        foreach (var start in candidateStarts)
        {
            for (var endExclusive = lines.Length; endExclusive > start; endExclusive--)
            {
                var candidate = string.Join('\n', lines[start..endExclusive]).Trim();
                if (IsValidYamlMapping(candidate))
                {
                    return candidate;
                }
            }
        }

        return string.Empty;
    }

    private static bool IsValidYamlMapping(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        try
        {
            using var reader = new StringReader(candidate);
            var stream = new YamlStream();
            stream.Load(reader);
            return stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode;
        }
        catch
        {
            return false;
        }
    }
}
