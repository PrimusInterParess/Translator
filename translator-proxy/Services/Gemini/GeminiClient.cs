using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
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
            baseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

        var qp = (opts.ApiKeyQueryParamName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(qp)) qp = "key";

        var usedModel = (model ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(usedModel))
            usedModel = opts.Model;
        if (string.IsNullOrWhiteSpace(usedModel))
            usedModel = "gemini-1.5-flash";

        var url =
            $"{baseUrl}/{Uri.EscapeDataString(usedModel)}:generateContent" +
            $"?{Uri.EscapeDataString(qp)}={Uri.EscapeDataString(apiKey)}";

        var http = _httpClientFactory.CreateClient();
        using var response = await http.PostAsJsonAsync(
            url,
            requestBody,
            options: new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken: cancellationToken
        );

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

