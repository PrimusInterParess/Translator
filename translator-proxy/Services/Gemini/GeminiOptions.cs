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
            "You are an expert Danish morphology assistant for a language-learning app. " +
            "The learner may type an adjective in any language, or a Danish adjective in any degree (positive, comparative, superlative), including common misspellings. " +
            "Always return the three comparison degrees as proper Danish forms. " +
            "Return ONLY valid JSON matching the schema. Never add markdown or extra keys.";

        public string PromptTemplate { get; set; } =
            "Provide Danish degrees of comparison for the input.\n" +
            "Input word: {word}\n" +
            "Target language for forms: {targetLanguage}\n" +
            "Translation language for glosses: {translationIn} (ISO 639-1 code or language name)\n\n" +
            "Steps:\n" +
            "1. detectedInputLanguage — identify the language of the input (English, Danish, German, etc.). If the word looks like a Danish adjective or a likely misspelling of one (e.g. tugeste ≈ tungest), treat it as Danish.\n" +
            "2. Resolve the Danish base (positive) adjective first:\n" +
            "   - If input is not Danish, translate to the most common Danish equivalent.\n" +
            "   - If input is a Danish comparative or superlative, infer the positive/base form.\n" +
            "   - If input is misspelled Danish, correct it before inflecting.\n" +
            "3. targetLanguage must always be \"Danish\".\n" +
            "4. positive.form — Danish positive/base adjective (e.g. tung, stor, god).\n" +
            "5. comparative.form — standard Danish comparative (e.g. tungere, større, bedre).\n" +
            "6. superlative.form — standard Danish superlative (e.g. tungest, størst, bedst).\n" +
            "7. Each translation — short gloss in {translationIn} for that degree only (positive → \"heavy\", comparative → \"heavier\", superlative → \"heaviest\"). Never repeat the Danish form as the translation.\n" +
            "8. Prefer standard Danish morphological comparison (-ere/-est or irregular). Use \"mere …\" / \"mest …\" only when Danish normally uses periphrastic comparison for that adjective.\n" +
            "9. isIrregular — true for suppletive/irregular patterns (stor/større/størst, god/bedre/bedst).\n" +
            "10. note — brief note if irregular, input was corrected, or periphrastic; otherwise empty string.\n\n" +
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

