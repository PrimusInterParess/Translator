using System.Text.Json;
using System.Text.Json.Serialization;

namespace translator_proxy.Services.Llm;

internal static class VerbFormsJsonParser
{
    internal sealed record VerbFormsParseResult(
        string Infinitive,
        string Meaning,
        string Present,
        string Past,
        string PastParticiple,
        string Imperative)
    {
        public object ToApiBody() => new
        {
            ok = true,
            infinitive = Infinitive,
            meaning = Meaning,
            present = Present,
            past = Past,
            pastParticiple = PastParticiple,
            imperative = Imperative
        };
    }

    internal static bool TryParse(string? raw, string badJsonError, out VerbFormsParseResult? result, out string error)
    {
        result = null;
        error = badJsonError;

        var jsonText = LlmJsonHelper.NormalizeJsonText(raw);
        if (string.IsNullOrWhiteSpace(jsonText))
            return false;

        try
        {
            var dto = JsonSerializer.Deserialize<VerbFormsDto>(
                jsonText,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            );

            if (dto is null)
                return false;

            var infinitive = (dto.Infinitive ?? string.Empty).Trim();
            var meaning = (dto.Meaning ?? string.Empty).Trim();
            var present = (dto.Present ?? string.Empty).Trim();
            var past = (dto.Past ?? string.Empty).Trim();
            var pastParticiple = (dto.PastParticiple ?? string.Empty).Trim();
            var imperative = (dto.Imperative ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(infinitive) ||
                string.IsNullOrWhiteSpace(meaning) ||
                string.IsNullOrWhiteSpace(present) ||
                string.IsNullOrWhiteSpace(past) ||
                string.IsNullOrWhiteSpace(pastParticiple) ||
                string.IsNullOrWhiteSpace(imperative))
            {
                error = LlmConstants.ErrUnexpectedApiResponse;
                return false;
            }

            result = new VerbFormsParseResult(
                Infinitive: DanishVerbNormalizer.Normalize(infinitive),
                Meaning: meaning,
                Present: present,
                Past: past,
                PastParticiple: pastParticiple,
                Imperative: imperative
            );
            error = string.Empty;
            return true;
        }
        catch
        {
            error = badJsonError;
            return false;
        }
    }

    private sealed record VerbFormsDto(
        [property: JsonPropertyName("infinitive")] string? Infinitive,
        [property: JsonPropertyName("meaning")] string? Meaning,
        [property: JsonPropertyName("present")] string? Present,
        [property: JsonPropertyName("past")] string? Past,
        [property: JsonPropertyName("pastParticiple")] string? PastParticiple,
        [property: JsonPropertyName("imperative")] string? Imperative
    );
}
