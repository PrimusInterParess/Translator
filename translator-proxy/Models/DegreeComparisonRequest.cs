namespace translator_proxy.Models;

public record DegreeComparisonRequest(
    string? Text,
    string? TargetLanguage,
    string? TranslationIn
);
