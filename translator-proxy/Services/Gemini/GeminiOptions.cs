namespace translator_proxy.Services.Gemini;

public sealed class GeminiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-2.5-flash-lite";

    /// <summary>
    /// Base URL that should point to ".../{apiVersion}/models".
    /// Examples:
    /// - https://generativelanguage.googleapis.com/v1/models
    /// - https://generativelanguage.googleapis.com/v1beta/models
    /// </summary>
    public string GenerateContentBaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1/models";

    /// <summary>
    /// If upstream returns 404, try the other API version (v1 &lt;-&gt; v1beta) once.
    /// </summary>
    public bool EnableApiVersionFallback { get; set; } = true;

    /// <summary>
    /// Preferred auth method for the Gemini REST API.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "x-goog-api-key";

    /// <summary>
    /// Optional legacy auth method. Leave empty to avoid putting secrets in URLs/logs.
    /// </summary>
    public string ApiKeyQueryParamName { get; set; } = "";

    public VerbFormsOptions VerbForms { get; set; } = new();
    public ExplainOptions Explain { get; set; } = new();

    public sealed class VerbFormsOptions
    {
        public string SystemInstruction { get; set; } =
            "You extract Danish verb forms. Return ONLY valid JSON matching the schema.";

        public string PromptTemplate { get; set; } =
            "Verb: {verb}";
    }

    public sealed class ExplainOptions
    {
        public string SystemInstruction { get; set; } =
            "You explain phrases or partial sentences. Return ONLY valid JSON matching the schema.";

        public string PromptTemplate { get; set; } =
            "Explain in: {explainIn}\nSource language (if known): {sourceLang}\nText: {text}\nContext: {context}";
    }
}

