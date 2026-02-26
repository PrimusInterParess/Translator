namespace translator_proxy.Services;

internal static class MyMemoryConstants
{
    public const int MaxTextLength = 500;

    public const string BaseUrl = "https://api.mymemory.translated.net/get";

    public const string JsonPropResponseData = "responseData";
    public const string JsonPropTranslatedText = "translatedText";
    public const string JsonPropResponseStatus = "responseStatus";
    public const string JsonPropResponseDetails = "responseDetails";
    public const string JsonPropMessage = "message";

    public const string ErrMissingText = "Missing text";
    public const string ErrMissingSourceLang = "Missing source language";
    public const string ErrMissingTargetLang = "Missing target language";
    public const string ErrTextTooLongFormat = "Text too long (max {0} chars)";
    public const string ErrUnexpectedApiResponse = "Unexpected API response";

    public const string HttpStatusFallbackPrefix = "HTTP ";
}

