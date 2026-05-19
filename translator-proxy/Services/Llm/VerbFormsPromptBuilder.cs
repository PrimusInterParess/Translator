namespace translator_proxy.Services.Llm;

internal static class VerbFormsPromptBuilder
{
    internal static string BuildLenient(string? template, VerbFormsValidatedInput input)
    {
        var t = (template ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t))
            return $"Verb: {input.CleanedVerb}";

        return BuildFromTemplate(t, input);
    }

    internal static bool TryBuildRequired(
        string? template,
        VerbFormsValidatedInput input,
        out string prompt,
        out string error)
    {
        var t = (template ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t))
        {
            prompt = string.Empty;
            error = LlmConstants.ErrMissingOllamaVerbFormsPromptTemplate;
            return false;
        }

        prompt = BuildFromTemplate(t, input);
        error = string.Empty;
        return true;
    }

    private static string BuildFromTemplate(string template, VerbFormsValidatedInput input)
    {
        var prompt = template
            .Replace("{verb}", input.CleanedVerb, StringComparison.OrdinalIgnoreCase)
            .Replace("{meaningIn}", string.IsNullOrWhiteSpace(input.MeaningIn) ? "en" : input.MeaningIn, StringComparison.OrdinalIgnoreCase);

        if (!template.Contains("{verb}", StringComparison.OrdinalIgnoreCase))
            prompt = $"{prompt}\nVerb: {input.CleanedVerb}";

        return prompt;
    }
}
