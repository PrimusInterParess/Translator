using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using translator_proxy.Models;

namespace translator_proxy.Services;

public sealed class MyMemoryTranslateService : ITranslateService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyMemoryTranslateService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TranslateServiceResult> TranslateAsync(TranslateRequest? req, CancellationToken cancellationToken)
    {
        var text = (req?.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TranslateServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = MyMemoryConstants.ErrMissingText }
            );
        }

        if (text.Length > MyMemoryConstants.MaxTextLength)
        {
            return new TranslateServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(MyMemoryConstants.ErrTextTooLongFormat, MyMemoryConstants.MaxTextLength)
                }
            );
        }

        var source = (req?.Source ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            return new TranslateServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = MyMemoryConstants.ErrMissingSourceLang }
            );
        }

        var target = (req?.Target ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            return new TranslateServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = MyMemoryConstants.ErrMissingTargetLang }
            );
        }

        var de = (req?.Email ?? string.Empty).Trim();
        var deParam = string.IsNullOrWhiteSpace(de) ? string.Empty : $"&de={Uri.EscapeDataString(de)}";
        var url =
            $"{MyMemoryConstants.BaseUrl}?q={Uri.EscapeDataString(text)}" +
            $"&langpair={Uri.EscapeDataString(source)}|{Uri.EscapeDataString(target)}" +
            deParam;

        var http = _httpClientFactory.CreateClient();
        using var response = await http.GetAsync(url, cancellationToken);

        JsonElement? json = null;
        try
        {
            json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        }
        catch
        {
            json = null;
        }

        var errorMsg = ExtractMyMemoryError(json);

        if (!response.IsSuccessStatusCode)
        {
            return new TranslateServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new
                {
                    ok = false,
                    error = !string.IsNullOrWhiteSpace(errorMsg)
                        ? errorMsg
                        : $"{MyMemoryConstants.HttpStatusFallbackPrefix}{(int)response.StatusCode}"
                }
            );
        }

        if (!string.IsNullOrWhiteSpace(errorMsg))
        {
            return new TranslateServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = errorMsg }
            );
        }

        if (json is not { } root ||
            !root.TryGetProperty(MyMemoryConstants.JsonPropResponseData, out var responseData) ||
            !responseData.TryGetProperty(MyMemoryConstants.JsonPropTranslatedText, out var translatedTextEl) ||
            translatedTextEl.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(translatedTextEl.GetString()))
        {
            return new TranslateServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = MyMemoryConstants.ErrUnexpectedApiResponse }
            );
        }

        return new TranslateServiceResult(
            StatusCode: StatusCodes.Status200OK,
            Body: new
            {
                ok = true,
                translatedText = translatedTextEl.GetString()!
            }
        );
    }

    private static string ExtractMyMemoryError(JsonElement? json)
    {
        if (json is not { } root) return string.Empty;

        try
        {
            if (root.TryGetProperty(MyMemoryConstants.JsonPropResponseStatus, out var statusEl) &&
                statusEl.ValueKind == JsonValueKind.Number &&
                statusEl.TryGetInt32(out var status) &&
                status != 200)
            {
                if (root.TryGetProperty(MyMemoryConstants.JsonPropResponseDetails, out var detailsEl) &&
                    detailsEl.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(detailsEl.GetString()))
                {
                    return detailsEl.GetString()!;
                }

                if (root.TryGetProperty(MyMemoryConstants.JsonPropMessage, out var msgEl) &&
                    msgEl.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(msgEl.GetString()))
                {
                    return msgEl.GetString()!;
                }

                return $"{MyMemoryConstants.HttpStatusFallbackPrefix}{status}";
            }
        }
        catch
        {
            // ignore
        }

        return string.Empty;
    }
}

