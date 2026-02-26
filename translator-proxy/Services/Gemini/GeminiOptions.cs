namespace translator_proxy.Services.Gemini;

public sealed class GeminiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-1.5-flash";
    public string GenerateContentBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    public string ApiKeyQueryParamName { get; set; } = "key";

    public VerbFormsOptions VerbForms { get; set; } = new();

    public sealed class VerbFormsOptions
    {
        public string SystemInstruction { get; set; } =
            "You extract Danish verb forms. Return ONLY valid JSON matching the schema.";

        public string PromptTemplate { get; set; } =
            "Verb: {verb}";
    }
}

