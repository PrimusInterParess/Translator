namespace translator_proxy.Services.Ollama;

public interface IOllamaClient
{
    Task<OllamaChatResult> ChatAsync(
        string systemMessage,
        string userMessage,
        bool jsonMode,
        CancellationToken cancellationToken);
}

public sealed record OllamaChatResult(
    bool Ok,
    int? UpstreamStatusCode,
    string? Error,
    string? Content
);
