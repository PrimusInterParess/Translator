using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using translator_proxy.Models;

namespace translator_proxy.Services;

public sealed class TtsService : ITtsService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public TtsService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TtsServiceResult> SynthesizeAsync(TtsRequest? req, CancellationToken cancellationToken)
    {

        var text = (req?.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TtsServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = TtsConstants.ErrMissingText }
            );
        }

        if (text.Length > TtsConstants.MaxTextLength)
        {
            return new TtsServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(TtsConstants.ErrTextTooLongFormat, TtsConstants.MaxTextLength)
                }
            );
        }

        var body = new
        {
            input = new { text },
            voice = new
            {
                languageCode = string.IsNullOrWhiteSpace(req?.LanguageCode) ? null : req!.LanguageCode!.Trim(),
                name = string.IsNullOrWhiteSpace(req?.VoiceName) ? null : req!.VoiceName!.Trim(),
            },
            audioConfig = new
            {
                audioEncoding = TtsConstants.GoogleTtsAudioEncodingMp3,
                speakingRate = req?.SpeakingRate,
                pitch = req?.Pitch,
            }
        };

        var apiKey = (
            _config[TtsConstants.GoogleTtsApiKeyConfigKey] ??
            _config[TtsConstants.LegacyGoogleTtsApiKeyConfigKey] ??
            string.Empty
        ).Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TtsServiceResult(
                StatusCode: StatusCodes.Status500InternalServerError,
                Body: new { ok = false, error = TtsConstants.ErrMissingApiKey }
            );
        }

        var url =
            $"{TtsConstants.GoogleTtsSynthesizeBaseUrl}?{TtsConstants.GoogleTtsApiKeyQueryParamName}={Uri.EscapeDataString(apiKey)}";
        var http = _httpClientFactory.CreateClient();

        using var response = await http.PostAsJsonAsync(url, body, cancellationToken);

        JsonElement? json = null;
        try
        {
            json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        }
        catch
        {
            json = null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var msg = $"{TtsConstants.HttpStatusFallbackPrefix}{(int)response.StatusCode}";
            if (json is { } root)
            {
                if (root.TryGetProperty(TtsConstants.JsonPropError, out var err) &&
                    err.TryGetProperty(TtsConstants.JsonPropMessage, out var m) &&
                    m.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(m.GetString()))
                {
                    msg = m.GetString()!;
                }
                else if (root.TryGetProperty(TtsConstants.JsonPropMessage, out var mm) &&
                         mm.ValueKind == JsonValueKind.String &&
                         !string.IsNullOrWhiteSpace(mm.GetString()))
                {
                    msg = mm.GetString()!;
                }
            }

            return new TtsServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = msg }
            );
        }

        if (json is not { } okRoot ||
            !okRoot.TryGetProperty(TtsConstants.JsonPropAudioContent, out var audioContentEl) ||
            audioContentEl.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(audioContentEl.GetString()))
        {
            return new TtsServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = TtsConstants.ErrGoogleReturnedEmptyAudio }
            );
        }

        var audioContent = audioContentEl.GetString()!;
        return new TtsServiceResult(
            StatusCode: StatusCodes.Status200OK,
            Body: new
            {
                ok = true,
                audio = new { mimeType = TtsConstants.OutputMimeTypeMpeg, base64 = audioContent }
            }
        );
    }
}

