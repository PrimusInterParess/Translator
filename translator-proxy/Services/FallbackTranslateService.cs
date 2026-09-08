using Microsoft.AspNetCore.Http;
using translator_proxy.Models;

namespace translator_proxy.Services;

public sealed class FallbackTranslateService : ITranslateService
{
    private readonly MyMemoryTranslateService _myMemory;
    private readonly GeminiTranslateService _gemini;
    private readonly ILogger<FallbackTranslateService> _logger;

    public FallbackTranslateService(
        MyMemoryTranslateService myMemory,
        GeminiTranslateService gemini,
        ILogger<FallbackTranslateService> logger)
    {
        _myMemory = myMemory;
        _gemini = gemini;
        _logger = logger;
    }

    public async Task<TranslateServiceResult> TranslateAsync(TranslateRequest? req, CancellationToken cancellationToken)
    {
        TranslateServiceResult primary;
        try
        {
            primary = await _myMemory.TranslateAsync(req, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            _logger.LogWarning(ex, "MyMemory request failed; falling back to Gemini");
            return await TryGeminiAsync(req, cancellationToken, primary: null);
        }

        if (primary.StatusCode is >= 200 and <= 299)
        {
            if (!IsUntranslated(req?.Text, primary.TranslatedText))
            {
                return primary;
            }

            _logger.LogWarning("MyMemory echoed the source text; falling back to Gemini");
            return await TryGeminiAsync(req, cancellationToken, primary);
        }

        if (!ShouldFallback(primary.StatusCode))
        {
            return primary;
        }

        _logger.LogWarning("MyMemory returned {StatusCode}; falling back to Gemini", primary.StatusCode);
        return await TryGeminiAsync(req, cancellationToken, primary);
    }

    private async Task<TranslateServiceResult> TryGeminiAsync(
        TranslateRequest? req,
        CancellationToken cancellationToken,
        TranslateServiceResult? primary)
    {
        TranslateServiceResult fallback;
        try
        {
            fallback = await _gemini.TranslateAsync(req, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            _logger.LogWarning(ex, "Gemini fallback failed");
            return primary ?? new TranslateServiceResult(
                StatusCodes.Status502BadGateway,
                new { ok = false, error = "Translate failed" }
            );
        }

        if (fallback.StatusCode is >= 200 and <= 299)
        {
            return fallback;
        }

        // Missing Gemini key: keep the original MyMemory error.
        if (fallback.StatusCode == StatusCodes.Status500InternalServerError && primary is not null)
        {
            return primary;
        }

        return fallback;
    }

    private static bool ShouldFallback(int statusCode) =>
        statusCode is >= 500 or StatusCodes.Status429TooManyRequests;

    private static bool IsUntranslated(string? source, string? translated)
    {
        var a = (source ?? string.Empty).Trim();
        var b = (translated ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
