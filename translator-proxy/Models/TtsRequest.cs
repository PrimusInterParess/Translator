namespace translator_proxy.Models;

public record TtsRequest(
    string? Text,
    string? LanguageCode,
    string? VoiceName,
    double? SpeakingRate,
    double? Pitch
);

