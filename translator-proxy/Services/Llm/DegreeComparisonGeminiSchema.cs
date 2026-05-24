using System.Text.Json.Nodes;

namespace translator_proxy.Services.Llm;

internal static class DegreeComparisonGeminiSchema
{
    private static JsonObject StringType() => new() { ["type"] = "STRING" };

    private static JsonObject DegreeFormSchema() => new()
    {
        ["type"] = "OBJECT",
        ["properties"] = new JsonObject
        {
            ["form"] = StringType(),
            ["translation"] = StringType()
        },
        ["required"] = new JsonArray { "form", "translation" }
    };

    internal static JsonObject BuildResponseSchema()
    {
        return new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["detectedInputLanguage"] = StringType(),
                ["targetLanguage"] = StringType(),
                ["positive"] = DegreeFormSchema(),
                ["comparative"] = DegreeFormSchema(),
                ["superlative"] = DegreeFormSchema(),
                ["isIrregular"] = new JsonObject { ["type"] = "BOOLEAN" },
                ["note"] = StringType()
            },
            ["required"] = new JsonArray
            {
                "detectedInputLanguage",
                "targetLanguage",
                "positive",
                "comparative",
                "superlative",
                "isIrregular",
                "note"
            }
        };
    }
}
