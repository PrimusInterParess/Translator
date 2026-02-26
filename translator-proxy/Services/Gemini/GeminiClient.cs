using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using System.Net;

namespace translator_proxy.Services.Gemini;

public sealed class GeminiClient : IGeminiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<GeminiOptions> _options;

    public GeminiClient(IHttpClientFactory httpClientFactory, IOptions<GeminiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<GeminiClientResult> GenerateContentAsync(string model, JsonObject requestBody, CancellationToken cancellationToken)
    {
        var opts = _options.Value;

        var apiKey = (opts.ApiKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Let callers decide how to surface missing key.
            return new GeminiClientResult(
                Ok: false,
                UpstreamStatusCode: null,
                Error: "Missing API key",
                CandidateText: null,
                Raw: null
            );
        }

        var baseUrl = NormalizeModelsBaseUrl(opts.GenerateContentBaseUrl);

        var headerName = (opts.ApiKeyHeaderName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(headerName)) headerName = "x-goog-api-key";

        // Optional legacy mode (discouraged): pass API key via query string.
        // Keep empty by default to avoid leaking secrets into logs (HttpClient logs include URLs).
        var qp = (opts.ApiKeyQueryParamName ?? string.Empty).Trim();

        var usedModel = (model ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(usedModel))
            usedModel = opts.Model;
        if (string.IsNullOrWhiteSpace(usedModel))
            usedModel = "gemini-2.5-flash-lite";

        // Allow both "gemini-2.5-flash-lite" and "models/gemini-2.5-flash-lite" inputs.
        // `GenerateContentBaseUrl` already points at ".../models".
        var slash = usedModel.LastIndexOf('/');
        if (slash >= 0 && slash < usedModel.Length - 1)
            usedModel = usedModel[(slash + 1)..];

        var http = _httpClientFactory.CreateClient();

        var url = BuildGenerateContentUrl(baseUrl, usedModel, qp, apiKey);
        var (statusCode, json) = await PostAsync(http, url, headerName, apiKey, requestBody, cancellationToken);

        if (statusCode == (int)HttpStatusCode.NotFound && opts.EnableApiVersionFallback)
        {
            var swappedBaseUrl = TrySwapApiVersion(baseUrl);
            if (!string.IsNullOrWhiteSpace(swappedBaseUrl) && !swappedBaseUrl.Equals(baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                var swappedUrl = BuildGenerateContentUrl(swappedBaseUrl, usedModel, qp, apiKey);
                var (swappedStatus, swappedJson) = await PostAsync(http, swappedUrl, headerName, apiKey, requestBody, cancellationToken);

                // Prefer the fallback response if it isn't a 404, or if it provides a clearer error payload.
                if (swappedStatus != (int)HttpStatusCode.NotFound || !string.IsNullOrWhiteSpace(ExtractUpstreamError(swappedJson)))
                {
                    return ToResult(swappedStatus, swappedJson, swappedUrl, usedModel);
                }
            }
        }

        return ToResult(statusCode, json, url, usedModel);
    }

    private static async Task<(int statusCode, GeminiGenerateContentResponse? json)> PostAsync(
        HttpClient http,
        string url,
        string headerName,
        string apiKey,
        JsonObject requestBody,
        CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(
                requestBody,
                mediaType: new MediaTypeHeaderValue("application/json"),
                options: new JsonSerializerOptions(JsonSerializerDefaults.Web)
            )
        };
        req.Headers.TryAddWithoutValidation(headerName, apiKey);

        using var response = await http.SendAsync(req, cancellationToken);

        GeminiGenerateContentResponse? json = null;
        try
        {
            json = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            json = null;
        }

        return ((int)response.StatusCode, json);
    }

    private static GeminiClientResult ToResult(int statusCode, GeminiGenerateContentResponse? json, string url, string model)
    {
        var upstreamError = ExtractUpstreamError(json);
        var isSuccess = statusCode is >= 200 and <= 299;

        if (!isSuccess)
        {
            var msg = !string.IsNullOrWhiteSpace(upstreamError)
                ? upstreamError
                : statusCode == (int)HttpStatusCode.NotFound
                    ? $"Not found (404) calling {SanitizeUrl(url)}. Model '{model}' may be unavailable for this API version/key/region. Try updating Gemini:Model (e.g. gemini-2.5-flash-lite) and/or switching Gemini:GenerateContentBaseUrl between /v1/models and /v1beta/models."
                    : $"HTTP {statusCode}";

            return new GeminiClientResult(
                Ok: false,
                UpstreamStatusCode: statusCode,
                Error: msg,
                CandidateText: null,
                Raw: json
            );
        }

        if (!string.IsNullOrWhiteSpace(upstreamError))
        {
            return new GeminiClientResult(
                Ok: false,
                UpstreamStatusCode: statusCode,
                Error: upstreamError,
                CandidateText: null,
                Raw: json
            );
        }

        var text = ExtractCandidateText(json);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new GeminiClientResult(
                Ok: false,
                UpstreamStatusCode: statusCode,
                Error: "Empty response",
                CandidateText: null,
                Raw: json
            );
        }

        return new GeminiClientResult(
            Ok: true,
            UpstreamStatusCode: statusCode,
            Error: null,
            CandidateText: text,
            Raw: json
        );
    }

    private static string BuildGenerateContentUrl(string modelsBaseUrl, string model, string qp, string apiKey)
    {
        var url = $"{modelsBaseUrl}/{Uri.EscapeDataString(model)}:generateContent";
        if (!string.IsNullOrWhiteSpace(qp))
        {
            url += $"?{Uri.EscapeDataString(qp)}={Uri.EscapeDataString(apiKey)}";
        }
        return url;
    }

    private static string NormalizeModelsBaseUrl(string? configured)
    {
        var baseUrl = (configured ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "https://generativelanguage.googleapis.com/v1";

        if (!baseUrl.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
            baseUrl = $"{baseUrl}/models";

        return baseUrl;
    }

    private static string TrySwapApiVersion(string modelsBaseUrl)
    {
        if (modelsBaseUrl.IndexOf("/v1beta/", StringComparison.OrdinalIgnoreCase) >= 0)
            return ReplaceFirst(modelsBaseUrl, "/v1beta/", "/v1/");

        if (modelsBaseUrl.IndexOf("/v1/", StringComparison.OrdinalIgnoreCase) >= 0)
            return ReplaceFirst(modelsBaseUrl, "/v1/", "/v1beta/");

        return modelsBaseUrl;
    }

    private static string ReplaceFirst(string input, string oldValue, string newValue)
    {
        var idx = input.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return input;
        return string.Concat(input.AsSpan(0, idx), newValue, input.AsSpan(idx + oldValue.Length));
    }

    private static string SanitizeUrl(string url)
    {
        try
        {
            return new Uri(url).GetLeftPart(UriPartial.Path);
        }
        catch
        {
            var q = url.IndexOf('?');
            return q >= 0 ? url[..q] : url;
        }
    }

    private static string ExtractUpstreamError(GeminiGenerateContentResponse? json)
    {
        var msg = (json?.Error?.Message ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(msg)) return msg;

        msg = (json?.Message ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(msg)) return msg;

        return string.Empty;
    }

    private static string? ExtractCandidateText(GeminiGenerateContentResponse? json)
    {
        return json?.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text?
            .Trim();
    }
}

