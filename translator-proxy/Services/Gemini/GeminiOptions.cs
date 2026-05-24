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
    public DegreeComparisonOptions DegreeComparison { get; set; } = new();
    public ExplainOptions Explain { get; set; } = new();

    public sealed class VerbFormsOptions
    {
        public string SystemInstruction { get; set; } =
            "You extract Danish verb forms for a language-learning popup. " +
            "The input can be a Danish verb, a conjugated form, or an infinitive with/without 'at'. " +
            "Infer the dictionary infinitive and give the most useful core sense. " +
            "Return ONLY valid JSON matching the schema. Never add markdown or extra keys.";

        public string PromptTemplate { get; set; } =
            "Return Danish verb forms for the input.\n" +
            "Input: {verb}\n" +
            "Meaning language: {meaningIn} (ISO 639-1 code or language name)\n\n" +
            "Rules:\n" +
            "- infinitive: dictionary infinitive without \"at\".\n" +
            "- meaning: short learner-friendly gloss for the sense most likely intended by the input, not a list of all senses.\n" +
            "- present: simple present form.\n" +
            "- past: simple past form.\n" +
            "- pastParticiple: participle only, without auxiliary (for example \"spist\", not \"har spist\").\n" +
            "- imperative: normal imperative form.\n" +
            "- If the input is not already infinitive, still return the infinitive for that verb.\n" +
            "- If a form is ambiguous, choose the most common modern Danish form.\n\n" +
            "Verb: {verb}";
    }

    public sealed class DegreeComparisonOptions
    {
        public string SystemInstruction { get; set; } =
            "You are a linguistic tool for degrees of comparison. " +
            "Detect the input language automatically. " +
            "Return comparison forms in the requested target language. " +
            "Return ONLY valid JSON matching the schema. Never add markdown or extra keys.";

        public string PromptTemplate { get; set; } =
            "Provide degrees of comparison for the input word.\n" +
            "Input word: {word}\n" +
            "Target language for forms: {targetLanguage}\n" +
            "Translation language: {translationIn} (ISO 639-1 code or language name)\n\n" +
            "Rules:\n" +
            "- Detect the input language automatically.\n" +
            "- If the input is not already in the target language, translate to the target language first, then provide comparison degrees in the target language.\n" +
            "- positive, comparative, superlative: each with form (in target language) and translation (in translation language).\n" +
            "- If the comparison is irregular, set isIrregular to true and explain briefly in note.\n" +
            "- If the target language uses periphrastic comparison for this adjective, use the natural periphrastic forms.\n" +
            "- If note is not needed, return an empty string.\n\n" +
            "Word: {word}";
    }

    public sealed class ExplainOptions
    {
        public string SystemInstruction { get; set; } =
            "You are an expert Danish tutor. Infer usage patterns only from the learner's sentence and part — do not assume fixed vocabulary. " +
            "Return ONLY valid JSON. Plain text with newlines, no markdown.";

        public string PromptTemplate { get; set; } =
            "Explain Danish for a learner. Write all strings in {explainIn}.\n\n" +
            "Sentence: {sentence}\nPart to explain: {fragment}\n\n" +
            "Analyze only the provided text.\n\n" +
            "sentenceTranslation = full sentence.\n" +
            "translation = PART gloss only.\n" +
            "inYourSentence = part's role in this sentence.\n" +
            "whenUsed = numbered usage patterns inferred from the input, with Danish examples per pattern; note confusable constructions only if relevant.\n" +
            "whyDifferent = contrast patterns and learner mistakes.\n" +
            "examples = 5-7 {context, source, meaning}; each entry a different use case for the part; context = use-case label; no duplicate use cases; at most one user's sentence.";
    }
}

