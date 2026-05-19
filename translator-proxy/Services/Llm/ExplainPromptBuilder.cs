namespace translator_proxy.Services.Llm;

internal static class ExplainPromptBuilder
{
    internal static string Build(string template, ExplainValidatedInput input)
    {
        var fragment = string.IsNullOrWhiteSpace(input.Context) ? "(none)" : input.Context;
        return template
            .Replace("{sentence}", input.Text)
            .Replace("{fragment}", fragment)
            .Replace("{text}", input.Text)
            .Replace("{context}", fragment)
            .Replace("{sourceLang}", string.IsNullOrWhiteSpace(input.SourceLang) ? "(infer)" : input.SourceLang)
            .Replace("{explainIn}", string.IsNullOrWhiteSpace(input.ExplainIn) ? "en" : input.ExplainIn);
    }
}
