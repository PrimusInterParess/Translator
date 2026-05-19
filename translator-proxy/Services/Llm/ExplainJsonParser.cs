using System.Text.Json;
using translator_proxy.Services;

namespace translator_proxy.Services.Llm;

internal static class ExplainJsonParser
{
    internal sealed record ExplainParseResult(
        string SentenceTranslation,
        string Translation,
        string InYourSentence,
        string WhenUsed,
        string WhyDifferent,
        object[] Examples)
    {
        public object ToApiBody(string sentence, string fragment, string explainIn) => new
        {
            ok = true,
            meta = new
            {
                sentence,
                fragment,
                explainIn
            },
            sentenceTranslation = SentenceTranslation,
            translation = Translation,
            inYourSentence = InYourSentence,
            whenUsed = WhenUsed,
            whyDifferent = WhyDifferent,
            examples = Examples
        };
    }

    public static bool TryParse(string? raw, out ExplainParseResult? result, out string error)
    {
        result = null;
        error = LlmConstants.ErrLlmBadJson;

        var jsonText = LlmJsonHelper.ExtractJsonObject(LlmJsonHelper.NormalizeJsonText(raw));
        if (string.IsNullOrWhiteSpace(jsonText))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            var sentenceTranslation = ReadString(root, "sentenceTranslation");
            var translation = ReadString(root, "translation");
            var inYourSentence = ReadString(root, "inYourSentence");
            var whenUsed = ReadString(root, "whenUsed");
            var whyDifferent = ReadString(root, "whyDifferent");

            if (string.IsNullOrWhiteSpace(translation) ||
                string.IsNullOrWhiteSpace(inYourSentence) ||
                string.IsNullOrWhiteSpace(whenUsed) ||
                string.IsNullOrWhiteSpace(whyDifferent))
            {
                error = LlmConstants.ErrUnexpectedApiResponse;
                return false;
            }

            if (string.IsNullOrWhiteSpace(sentenceTranslation))
                sentenceTranslation = translation;

            result = new ExplainParseResult(
                SentenceTranslation: sentenceTranslation,
                Translation: translation,
                InYourSentence: inYourSentence,
                WhenUsed: whenUsed,
                WhyDifferent: whyDifferent,
                Examples: ReadExamples(root)
            );

            error = string.Empty;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static object[] ReadExamples(JsonElement root)
    {
        if (!TryGetProperty(root, "examples", out var el) || el.ValueKind != JsonValueKind.Array)
            return Array.Empty<object>();

        var examples = new List<object>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var source = ReadString(item, "source");
            var meaning = ReadString(item, "meaning");
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(meaning))
                continue;

            var context = ReadString(item, "context");
            examples.Add(string.IsNullOrWhiteSpace(context)
                ? new { source, meaning }
                : new { source, meaning, context });
        }

        return examples.ToArray();
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement el)
    {
        if (root.TryGetProperty(name, out el))
            return true;

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                el = prop.Value;
                return true;
            }
        }

        el = default;
        return false;
    }

    private static string ReadString(JsonElement root, string name)
    {
        if (!TryGetProperty(root, name, out var el))
            return string.Empty;

        return ReadStringElement(el);
    }

    private static string ReadStringElement(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => (el.GetString() ?? string.Empty).Trim(),
            JsonValueKind.Number => el.GetRawText().Trim(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => el.ToString().Trim()
        };
    }
}
