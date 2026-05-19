using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using translator_proxy.Services;

namespace translator_proxy.Services.Ollama;

public sealed class OllamaClient : IOllamaClient
{
    public const string HttpClientName = "ollama";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<OllamaOptions> _options;

    public OllamaClient(IHttpClientFactory httpClientFactory, IOptions<OllamaOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<OllamaChatResult> ChatAsync(
        string systemMessage,
        string userMessage,
        bool jsonMode,
        CancellationToken cancellationToken)
    {
        var opts = _options.Value;

        var baseUrl = (opts.BaseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new OllamaChatResult(
                Ok: false,
                UpstreamStatusCode: null,
                Error: LlmConstants.ErrMissingOllamaBaseUrl,
                Content: null
            );
        }

        var model = (opts.Model ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            return new OllamaChatResult(
                Ok: false,
                UpstreamStatusCode: null,
                Error: LlmConstants.ErrMissingOllamaModel,
                Content: null
            );
        }

        var url = BuildChatCompletionsUrl(baseUrl);
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemMessage))
            messages.Add(new { role = "system", content = systemMessage });
        messages.Add(new { role = "user", content = userMessage });

        var body = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = messages,
            ["temperature"] = opts.Temperature ?? 0.2,
            ["stream"] = false
        };

        if (jsonMode)
            body["response_format"] = new { type = "json_object" };

        var http = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };

        var apiKey = (opts.ApiKey ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return new OllamaChatResult(
                Ok: false,
                UpstreamStatusCode: null,
                Error: $"Failed to reach Ollama at {SanitizeUrl(url)}: {ex.Message}",
                Content: null
            );
        }

        var statusCode = (int)response.StatusCode;
        OllamaChatCompletionResponse? json = null;
        try
        {
            json = await response.Content.ReadFromJsonAsync<OllamaChatCompletionResponse>(
                cancellationToken: cancellationToken
            );
        }
        catch
        {
            // fall through with null json
        }

        if (!response.IsSuccessStatusCode)
        {
            var upstream = ExtractError(json);
            var msg = !string.IsNullOrWhiteSpace(upstream)
                ? upstream
                : statusCode == (int)HttpStatusCode.NotFound
                    ? $"Not found (404) calling {SanitizeUrl(url)}. Is Ollama running? Try: ollama serve"
                    : $"{LlmConstants.HttpStatusFallbackPrefix}{statusCode}";

            return new OllamaChatResult(
                Ok: false,
                UpstreamStatusCode: statusCode,
                Error: msg,
                Content: null
            );
        }

        var content = json?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            var upstream = ExtractError(json);
            return new OllamaChatResult(
                Ok: false,
                UpstreamStatusCode: statusCode,
                Error: !string.IsNullOrWhiteSpace(upstream) ? upstream : LlmConstants.ErrLlmEmptyResponse,
                Content: null
            );
        }

        return new OllamaChatResult(
            Ok: true,
            UpstreamStatusCode: statusCode,
            Error: null,
            Content: content
        );
    }

    internal static string BuildChatCompletionsUrl(string baseUrl)
    {
        var raw = baseUrl.Trim().TrimEnd('/');

        if (raw.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            return raw;

        if (raw.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            return $"{raw}/chat/completions";

        return $"{raw}/v1/chat/completions";
    }

    private static string ExtractError(OllamaChatCompletionResponse? json)
    {
        var msg = (json?.Error?.Message ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(msg) ? string.Empty : msg;
    }

    private static string SanitizeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
        }
        catch
        {
            return url;
        }
    }
}
