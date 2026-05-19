using System.Text.Json.Nodes;

namespace translator_proxy.Services.Llm;

internal static class ExplainGeminiSchema
{
    public static JsonObject BuildResponseSchema()
    {
        var stringType = new JsonObject { ["type"] = "STRING" };

        var exampleItem = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["context"] = stringType,
                ["source"] = stringType,
                ["meaning"] = stringType
            },
            ["required"] = new JsonArray { "context", "source", "meaning" }
        };

        return new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["sentenceTranslation"] = stringType,
                ["translation"] = stringType,
                ["inYourSentence"] = stringType,
                ["whenUsed"] = stringType,
                ["whyDifferent"] = stringType,
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
