namespace translator_proxy.Services;

internal static class TtsConstants
{
    public const int MaxTextLength = 500;

    // Preferred config path: Tts:Google:ApiKey (env var: Tts__Google__ApiKey)
    public const string GoogleTtsApiKeyConfigKey = "Tts:Google:ApiKey";
    // Legacy config key (still supported)
    public const string LegacyGoogleTtsApiKeyConfigKey = "GOOGLE_TTS_API_KEY";

    public const string GoogleTtsSynthesizeBaseUrl = "https://texttospeech.googleapis.com/v1/text:synthesize";
    public const string GoogleTtsApiKeyQueryParamName = "key";

    public const string GoogleTtsAudioEncodingMp3 = "MP3";
    public const string OutputMimeTypeMpeg = "audio/mpeg";

    public const string JsonPropError = "error";
    public const string JsonPropMessage = "message";
    public const string JsonPropAudioContent = "audioContent";

    public const string ErrMissingText = "Missing text";
    public const string ErrTextTooLongFormat = "Text too long (max {0} chars)";

    public const string ErrMissingApiKey = "Server is missing Google TTS API key";
    public const string ErrGoogleReturnedEmptyAudio = "Google TTS returned empty audio";

    public const string HttpStatusFallbackPrefix = "HTTP ";
}

