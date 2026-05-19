using System.Text.Json.Nodes;

namespace translator_proxy.Services.Llm;

internal static class VerbFormsGeminiSchema
{
    private static JsonObject StringType() => new() { ["type"] = "STRING" };

    internal static JsonObject BuildResponseSchema()
    {
        return new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["infinitive"] = StringType(),
                ["meaning"] = StringType(),
                ["present"] = StringType(),
                ["past"] = StringType(),
                ["pastParticiple"] = StringType(),
                ["imperative"] = StringType()
            },
            ["required"] = new JsonArray { "infinitive", "meaning", "present", "past", "pastParticiple", "imperative" }
        };
    }
}
