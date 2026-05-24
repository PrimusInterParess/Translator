namespace translator_proxy.Services.Llm;

internal sealed record DegreeComparisonValidatedInput(
    string CleanedWord,
    string TargetLanguage,
    string TranslationIn);
