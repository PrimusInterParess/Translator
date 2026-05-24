using System.Text.Json;
using System.Text.Json.Serialization;

namespace translator_proxy.Services.Llm;

internal static class DegreeComparisonJsonParser
{
    internal sealed record DegreeForm(string Form, string Translation);

    internal sealed record DegreeComparisonParseResult(
        string DetectedInputLanguage,
        string TargetLanguage,
        DegreeForm Positive,
        DegreeForm Comparative,
        DegreeForm Superlative,
        bool IsIrregular,
        string? Note)
    {
        public object ToApiBody() => new
        {
            ok = true,
            detectedInputLanguage = DetectedInputLanguage,
            targetLanguage = TargetLanguage,
            positive = new { form = Positive.Form, translation = Positive.Translation },
            comparative = new { form = Comparative.Form, translation = Comparative.Translation },
            superlative = new { form = Superlative.Form, translation = Superlative.Translation },
            isIrregular = IsIrregular,
            note = Note ?? string.Empty
        };
    }

    internal static bool TryParse(
        string? raw,
        string badJsonError,
        out DegreeComparisonParseResult? result,
        out string error)
    {
        result = null;
        error = badJsonError;

        var jsonText = LlmJsonHelper.NormalizeJsonText(raw);
        if (string.IsNullOrWhiteSpace(jsonText))
            return false;

        try
        {
            var dto = JsonSerializer.Deserialize<DegreeComparisonDto>(
                jsonText,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (dto is null)
                return false;

            var detectedInputLanguage = (dto.DetectedInputLanguage ?? string.Empty).Trim();
            var targetLanguage = (dto.TargetLanguage ?? string.Empty).Trim();
            var positive = ParseForm(dto.Positive);
            var comparative = ParseForm(dto.Comparative);
            var superlative = ParseForm(dto.Superlative);
            var note = (dto.Note ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(detectedInputLanguage) ||
                string.IsNullOrWhiteSpace(targetLanguage) ||
                positive is null ||
                comparative is null ||
                superlative is null)
            {
                error = LlmConstants.ErrUnexpectedApiResponse;
                return false;
            }

            result = new DegreeComparisonParseResult(
                DetectedInputLanguage: detectedInputLanguage,
                TargetLanguage: targetLanguage,
                Positive: positive,
                Comparative: comparative,
                Superlative: superlative,
                IsIrregular: dto.IsIrregular,
                Note: string.IsNullOrWhiteSpace(note) ? null : note);
            error = string.Empty;
            return true;
        }
        catch
        {
            error = badJsonError;
            return false;
        }
    }

    private static DegreeForm? ParseForm(DegreeFormDto? dto)
    {
        if (dto is null)
            return null;

        var form = (dto.Form ?? string.Empty).Trim();
        var translation = (dto.Translation ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(form) || string.IsNullOrWhiteSpace(translation))
            return null;

        return new DegreeForm(form, translation);
    }

    private sealed record DegreeComparisonDto(
        [property: JsonPropertyName("detectedInputLanguage")] string? DetectedInputLanguage,
        [property: JsonPropertyName("targetLanguage")] string? TargetLanguage,
        [property: JsonPropertyName("positive")] DegreeFormDto? Positive,
        [property: JsonPropertyName("comparative")] DegreeFormDto? Comparative,
        [property: JsonPropertyName("superlative")] DegreeFormDto? Superlative,
        [property: JsonPropertyName("isIrregular")] bool IsIrregular,
        [property: JsonPropertyName("note")] string? Note);

    private sealed record DegreeFormDto(
        [property: JsonPropertyName("form")] string? Form,
        [property: JsonPropertyName("translation")] string? Translation);
}
