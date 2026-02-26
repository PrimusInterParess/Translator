using System.Text.Json.Serialization;

namespace translator_proxy.Services.Gemini;

public sealed record GeminiGenerateContentResponse(
    [property: JsonPropertyName("candidates")] GeminiCandidate[]? Candidates,
    [property: JsonPropertyName("error")] GeminiError? Error,
    [property: JsonPropertyName("message")] string? Message
);

public sealed record GeminiError(
    [property: JsonPropertyName("message")] string? Message
);

public sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent? Content
);

public sealed record GeminiContent(
    [property: JsonPropertyName("parts")] GeminiPart[]? Parts
);

public sealed record GeminiPart(
    [property: JsonPropertyName("text")] string? Text
);

