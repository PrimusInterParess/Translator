namespace translator_proxy.Services;

internal static class GeminiConstants
{
    public const int MaxTextLength = 120;
    public const int MaxExplainTextLength = 500;
    public const int MaxExplainContextLength = 2000;

    public const string GeminiConfigSection = "Gemini";

    public const string GeminiApiKeyConfigPath = "Gemini:ApiKey";
    public const string GeminiModelConfigPath = "Gemini:Model";
    public const string GeminiGenerateContentBaseUrlConfigPath = "Gemini:GenerateContentBaseUrl";
    public const string GeminiApiKeyQueryParamNameConfigPath = "Gemini:ApiKeyQueryParamName";

    public const string GeminiVerbFormsSystemInstructionConfigPath = "Gemini:VerbForms:SystemInstruction";
    public const string GeminiVerbFormsPromptTemplateConfigPath = "Gemini:VerbForms:PromptTemplate";
    public const string GeminiExplainSystemInstructionConfigPath = "Gemini:Explain:SystemInstruction";
    public const string GeminiExplainPromptTemplateConfigPath = "Gemini:Explain:PromptTemplate";

    // Legacy (top-level) keys, kept for backward compatibility with existing env vars.
    public const string LegacyGeminiApiKeyConfigKey = "GEMINI_API_KEY";
    public const string LegacyGeminiModelConfigKey = "GEMINI_MODEL";

    public const string JsonPropError = "error";
    public const string JsonPropMessage = "message";

    public const string JsonPropCandidates = "candidates";
    public const string JsonPropContent = "content";
    public const string JsonPropParts = "parts";
    public const string JsonPropText = "text";

    public const string ErrMissingText = "Missing text";
    public const string ErrTextTooLongFormat = "Text too long (max {0} chars)";
    public const string ErrContextTooLongFormat = "Context too long (max {0} chars)";
    public const string ErrMissingApiKey = "Server is missing Gemini API key";

    public const string ErrGeminiEmptyResponse = "Gemini returned empty response";
    public const string ErrGeminiBadJson = "Gemini returned invalid JSON";
    public const string ErrUnexpectedApiResponse = "Unexpected API response";

    public const string HttpStatusFallbackPrefix = "HTTP ";
}

