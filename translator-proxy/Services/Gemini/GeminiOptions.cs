namespace translator_proxy.Services.Gemini;

public sealed class GeminiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-1.5-flash";
    public string GenerateContentBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";

    /// <summary>
    /// Preferred auth method for the Gemini REST API.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "x-goog-api-key";

    /// <summary>
    /// Optional legacy auth method. Leave empty to avoid putting secrets in URLs/logs.
    /// </summary>
    public string ApiKeyQueryParamName { get; set; } = "";

    public VerbFormsOptions VerbForms { get; set; } = new();

    public sealed class VerbFormsOptions
    {
        public string SystemInstruction { get; set; } =
            "You extract Danish verb forms. Return ONLY valid JSON matching the schema.";

        public string PromptTemplate { get; set; } =
            "Verb: {verb}";
    }
}

