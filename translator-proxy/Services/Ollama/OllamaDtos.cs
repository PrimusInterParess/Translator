using System.Text.Json.Serialization;

namespace translator_proxy.Services.Ollama;

public sealed record OllamaChatCompletionResponse(
    [property: JsonPropertyName("choices")] OllamaChoice[]? Choices,
    [property: JsonPropertyName("error")] OllamaError? Error
);

public sealed record OllamaError(
    [property: JsonPropertyName("message")] string? Message
);

public sealed record OllamaChoice(
    [property: JsonPropertyName("message")] OllamaMessage? Message
);

public sealed record OllamaMessage(
    [property: JsonPropertyName("content")] string? Content
);
