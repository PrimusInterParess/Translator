using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

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

        var baseUrl = (opts.GenerateContentBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "https://generativelanguage.googleapis.com/v1beta";
        if (!baseUrl.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
            baseUrl = $"{baseUrl}/models";

        var headerName = (opts.ApiKeyHeaderName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(headerName)) headerName = "x-goog-api-key";

        // Optional legacy mode (discouraged): pass API key via query string.
        // Keep empty by default to avoid leaking secrets into logs (HttpClient logs include URLs).
        var qp = (opts.ApiKeyQueryParamName ?? string.Empty).Trim();

        var usedModel = (model ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(usedModel))
            usedModel = opts.Model;
        if (string.IsNullOrWhiteSpace(usedModel))
            usedModel = "gemini-1.5-flash";

        // Allow both "gemini-1.5-flash" and "models/gemini-1.5-flash" inputs.
        // `GenerateContentBaseUrl` already points at ".../models".
        var slash = usedModel.LastIndexOf('/');
        if (slash >= 0 && slash < usedModel.Length - 1)
            usedModel = usedModel[(slash + 1)..];

        var url = $"{baseUrl}/{Uri.EscapeDataString(usedModel)}:generateContent";
        if (!string.IsNullOrWhiteSpace(qp))
        {
            url += $"?{Uri.EscapeDataString(qp)}={Uri.EscapeDataString(apiKey)}";
        }

        var http = _httpClientFactory.CreateClient();

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

        var upstreamError = ExtractUpstreamError(json);

        if (!response.IsSuccessStatusCode)
        {
            return new GeminiClientResult(
                Ok: false,
                UpstreamStatusCode: (int)response.StatusCode,
                Error: !string.IsNullOrWhiteSpace(upstreamError) ? upstreamError : $"HTTP {(int)response.StatusCode}",
                CandidateText: null,
                Raw: json
            );
        }

        if (!string.IsNullOrWhiteSpace(upstreamError))
        {
            return new GeminiClientResult(
                Ok: false,
                UpstreamStatusCode: (int)response.StatusCode,
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
                UpstreamStatusCode: (int)response.StatusCode,
                Error: "Empty response",
                CandidateText: null,
                Raw: json
            );
        }

        return new GeminiClientResult(
            Ok: true,
            UpstreamStatusCode: (int)response.StatusCode,
            Error: null,
            CandidateText: text,
            Raw: json
        );
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

