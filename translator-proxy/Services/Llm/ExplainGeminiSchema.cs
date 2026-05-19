using System.Text.Json.Nodes;

namespace translator_proxy.Services.Llm;

internal static class ExplainGeminiSchema
{
    private static JsonObject StringType() => new() { ["type"] = "STRING" };

    public static JsonObject BuildResponseSchema()
    {
        var exampleItem = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["context"] = StringType(),
                ["source"] = StringType(),
                ["meaning"] = StringType()
            },
            ["required"] = new JsonArray { "context", "source", "meaning" }
        };

        return new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["sentenceTranslation"] = StringType(),
                ["translation"] = StringType(),
                ["inYourSentence"] = StringType(),
                ["whenUsed"] = StringType(),
                ["whyDifferent"] = StringType(),
                ["examples"] = new JsonObject
                {
                    ["type"] = "ARRAY",
                    ["items"] = exampleItem
                }
            },
            ["required"] = new JsonArray
            {
                "sentenceTranslation",
                "translation",
                "inYourSentence",
                "whenUsed",
                "whyDifferent",
                "examples"
            }
        };
    }
}
