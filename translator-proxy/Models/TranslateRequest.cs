namespace translator_proxy.Models;

public record TranslateRequest(
    string? Text,
    string? Source,
    string? Target,
    string? Email
);

