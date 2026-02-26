namespace translator_proxy.Models;

public record ExplainRequest(
    string? Text,
    string? Context,
    string? SourceLang,
    string? ExplainIn
);

