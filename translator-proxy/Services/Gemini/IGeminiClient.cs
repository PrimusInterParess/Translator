using System.Text.Json.Nodes;

namespace translator_proxy.Services.Gemini;

public interface IGeminiClient
{
    Task<GeminiClientResult> GenerateContentAsync(string model, JsonObject requestBody, CancellationToken cancellationToken);
}

public sealed record GeminiClientResult(
    bool Ok,
    int? UpstreamStatusCode,
    string? Error,
    string? CandidateText,
    GeminiGenerateContentResponse? Raw
);

