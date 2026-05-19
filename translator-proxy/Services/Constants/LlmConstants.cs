namespace translator_proxy.Services;

internal static class LlmConstants
{
    public const int MaxExplainTextLength = 500;
    public const int MaxExplainContextLength = 2000;

    public const string ErrMissingOllamaModel = "Server is missing Ollama:Model configuration";
    public const string ErrMissingOllamaBaseUrl = "Server is missing Ollama:BaseUrl configuration";
    public const string ErrMissingOllamaExplainSystemInstruction = "Server is missing Ollama:Explain:SystemInstruction configuration";
    public const string ErrMissingOllamaExplainPromptTemplate = "Server is missing Ollama:Explain:PromptTemplate configuration";
    public const string ErrMissingOllamaVerbFormsSystemInstruction = "Server is missing Ollama:VerbForms:SystemInstruction configuration";
    public const string ErrMissingOllamaVerbFormsPromptTemplate = "Server is missing Ollama:VerbForms:PromptTemplate configuration";

    public const string ErrMissingText = "Missing text";
    public const string ErrTextTooLongFormat = "Text too long (max {0} chars)";
    public const string ErrContextTooLongFormat = "Context too long (max {0} chars)";
    public const string ErrLlmEmptyResponse = "LLM returned empty response";
    public const string ErrLlmBadJson = "LLM returned invalid JSON";
    public const string ErrUnexpectedApiResponse = "Unexpected API response";
    public const string HttpStatusFallbackPrefix = "HTTP ";

    public const string ExplainJsonSchema =
        "Return a single JSON object with exactly these keys:\n" +
        "- sentenceTranslation (string: full translation of the sentence in box 1)\n" +
        "- translation (string: short gloss of the PART in box 2 only — not the whole clause; if no part, same as sentenceTranslation)\n" +
        "- inYourSentence (string: what the part contributes in this sentence)\n" +
        "- whenUsed (string: structured usage lesson — numbered patterns inferred from the input, with brief Danish examples per pattern)\n" +
        "- whyDifferent (string: contrast patterns, learner mistakes, which pattern fits the user's sentence)\n" +
        "- examples (array of 5-7 objects: context, source, meaning — each entry a different USE CASE for the part; context labels the use case; no duplicate use cases; at most one may mirror the user's sentence)\n" +
        "Write all strings in the requested explain language. No markdown, no code fences, no extra keys.";

    public const string VerbFormsJsonSchema =
        "Return a single JSON object with exactly these top-level keys:\n" +
        "- infinitive (string: dictionary infinitive without 'at')\n" +
        "- meaning (string: short learner-friendly gloss in the requested language)\n" +
        "- present (string: simple present form)\n" +
        "- past (string: simple past form)\n" +
        "- pastParticiple (string: participle only, no auxiliary)\n" +
        "- imperative (string: normal imperative form)\n" +
        "No markdown, no code fences, no extra keys.";
}
