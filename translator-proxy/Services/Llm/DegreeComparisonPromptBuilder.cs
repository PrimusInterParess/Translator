namespace translator_proxy.Services.Llm;

internal static class DegreeComparisonPromptBuilder
{
    internal static string BuildLenient(string? template, DegreeComparisonValidatedInput input)
    {
        var t = (template ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t))
            return BuildFallback(input);

        return BuildFromTemplate(t, input);
    }

    internal static bool TryBuildRequired(
        string? template,
        DegreeComparisonValidatedInput input,
        out string prompt,
        out string error)
    {
        var t = (template ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t))
        {
            prompt = string.Empty;
            error = LlmConstants.ErrMissingOllamaDegreeComparisonPromptTemplate;
            return false;
        }

        prompt = BuildFromTemplate(t, input);
        error = string.Empty;
        return true;
    }

    private static string BuildFromTemplate(string template, DegreeComparisonValidatedInput input)
    {
        var prompt = template
            .Replace("{word}", input.CleanedWord, StringComparison.OrdinalIgnoreCase)
            .Replace("{targetLanguage}", input.TargetLanguage, StringComparison.OrdinalIgnoreCase)
            .Replace("{translationIn}", input.TranslationIn, StringComparison.OrdinalIgnoreCase);

        if (!template.Contains("{word}", StringComparison.OrdinalIgnoreCase))
            prompt = $"{prompt}\nWord: {input.CleanedWord}";

        return prompt;
    }

    private static string BuildFallback(DegreeComparisonValidatedInput input) =>
        $"Word: {input.CleanedWord}\nTarget language: {input.TargetLanguage}\nTranslation language: {input.TranslationIn}";
}
