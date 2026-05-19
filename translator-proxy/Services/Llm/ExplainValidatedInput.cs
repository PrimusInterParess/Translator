namespace translator_proxy.Services.Llm;

internal sealed record ExplainValidatedInput(
    string Text,
    string Context,
    string SourceLang,
    string ExplainIn);
