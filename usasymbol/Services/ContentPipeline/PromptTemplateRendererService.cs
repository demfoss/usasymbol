using System.Text.Json;
using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline;

public sealed class PromptTemplateRendererService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string RenderGenerator(string template, PromptPayloadModel payload)
    {
        return Replace(template, "{{PROMPT_PAYLOAD}}", SerializePayload(payload));
    }

    public string RenderFinisher(string template, PromptPayloadModel payload, string draftYaml)
    {
        return Replace(
            Replace(template, "{{PROMPT_PAYLOAD}}", SerializePayload(payload)),
            "{{DRAFT_YAML}}",
            string.IsNullOrWhiteSpace(draftYaml) ? "<<generator output yaml>>" : draftYaml.Trim());
    }

    private static string SerializePayload(PromptPayloadModel payload)
    {
        var model = new
        {
            category = payload.Category,
            primary_keyword = payload.PrimaryKeyword,
            topic_or_state = payload.TopicOrState,
            yaml_skeleton = payload.YamlSkeleton,
            source_notes = payload.SourceNotes,
            optional_extra_instruction = payload.OptionalExtraInstruction,
            default_secondary_queries = payload.DefaultSecondaryQueries,
            title_examples = payload.TitleExamples,
            seo_description_examples = payload.SeoDescriptionExamples,
            internal_link_candidates = payload.InternalLinks.Select(x => new
            {
                title = x.Title,
                url = x.Url,
                anchor_hint = x.AnchorHint,
                reason = x.Reason
            }).ToArray(),
            recent_pattern_warnings = payload.RecentPatternWarnings,
            images_enabled = payload.ImagesEnabled,
            suggested_output_path = payload.SuggestedOutputPath
        };

        return JsonSerializer.Serialize(model, JsonOptions);
    }

    private static string Replace(string template, string token, string value)
    {
        return (template ?? string.Empty).Replace(token, value ?? string.Empty, StringComparison.Ordinal);
    }
}
