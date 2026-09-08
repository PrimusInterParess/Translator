using System.Text.Json.Nodes;

namespace translator_proxy.Services.Llm;

internal static class TranslateGeminiSchema
{
    public static JsonObject BuildResponseSchema() => new()
    {
        ["type"] = "OBJECT",
        ["properties"] = new JsonObject
        {
            ["translatedText"] = new JsonObject { ["type"] = "STRING" }
        },
        ["required"] = new JsonArray { "translatedText" }
    };
}
