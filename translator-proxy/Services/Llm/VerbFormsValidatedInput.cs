namespace translator_proxy.Services.Llm;

internal sealed record VerbFormsValidatedInput(
    string CleanedVerb,
    string MeaningIn);
