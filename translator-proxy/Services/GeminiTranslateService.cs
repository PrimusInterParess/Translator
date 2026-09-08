using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using translator_proxy.Models;
using translator_proxy.Services.Gemini;
using translator_proxy.Services.Llm;

namespace translator_proxy.Services;

public sealed class GeminiTranslateService : ITranslateService
{
    private const int MaxTextLength = 500;

    private readonly IGeminiClient _gemini;
    private readonly IOptions<GeminiOptions> _geminiOptions;

    public GeminiTranslateService(IGeminiClient gemini, IOptions<GeminiOptions> geminiOptions)
    {
        _gemini = gemini;
        _geminiOptions = geminiOptions;
    }

    public async Task<TranslateServiceResult> TranslateAsync(TranslateRequest? req, CancellationToken cancellationToken)
    {
        var text = (req?.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Fail(StatusCodes.Status400BadRequest, GeminiConstants.ErrMissingText);
        }

        if (text.Length > MaxTextLength)
        {
            return Fail(
                StatusCodes.Status400BadRequest,
                string.Format(GeminiConstants.ErrTextTooLongFormat, MaxTextLength)
            );
        }

        var source = (req?.Source ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            return Fail(StatusCodes.Status400BadRequest, GeminiConstants.ErrMissingSourceLang);
        }

        var target = (req?.Target ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            return Fail(StatusCodes.Status400BadRequest, GeminiConstants.ErrMissingTargetLang);
        }

        var apiKey = (_geminiOptions.Value.ApiKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Fail(StatusCodes.Status500InternalServerError, GeminiConstants.ErrMissingApiKey);
        }

        var model = (_geminiOptions.Value.Model ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(model)) model = "gemini-2.5-flash-lite";

        var systemInstruction = (_geminiOptions.Value.Translate.SystemInstruction ?? string.Empty).Trim();
        var promptTemplate = (_geminiOptions.Value.Translate.PromptTemplate ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(promptTemplate))
            promptTemplate = new GeminiOptions.TranslateOptions().PromptTemplate;

        var prompt = promptTemplate
            .Replace("{sourceName}", LanguageName(source))
            .Replace("{targetName}", LanguageName(target))
            .Replace("{source}", source)
            .Replace("{target}", target)
            .Replace("{text}", text);

        var body = BuildGenerateContentRequest(systemInstruction, prompt);
        var result = await _gemini.GenerateContentAsync(model, body, cancellationToken);

        if (!result.Ok)
        {
            var msg = !string.IsNullOrWhiteSpace(result.Error)
                ? result.Error!
                : (result.UpstreamStatusCode is { } s
                    ? $"{GeminiConstants.HttpStatusFallbackPrefix}{s}"
                    : GeminiConstants.ErrUnexpectedApiResponse);

            return Fail(StatusCodes.Status502BadGateway, msg);
        }

        var translatedText = ExtractTranslatedText(result.CandidateText);
        if (string.IsNullOrWhiteSpace(translatedText))
        {
            return Fail(StatusCodes.Status502BadGateway, GeminiConstants.ErrGeminiEmptyResponse);
        }

        return new TranslateServiceResult(
            StatusCode: StatusCodes.Status200OK,
            Body: new { ok = true, translatedText },
            TranslatedText: translatedText
        );
    }

    private static TranslateServiceResult Fail(int statusCode, string error) =>
        new(statusCode, new { ok = false, error });

    private static JsonObject BuildGenerateContentRequest(string systemInstruction, string prompt)
    {
        var req = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["text"] = prompt }
                    }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            req["systemInstruction"] = new JsonObject
            {
                ["role"] = "system",
                ["parts"] = new JsonArray
                {
                    new JsonObject { ["text"] = systemInstruction }
                }
            };
        }

        req["generationConfig"] = new JsonObject
        {
            ["temperature"] = 0,
            ["responseMimeType"] = "application/json",
            ["responseSchema"] = TranslateGeminiSchema.BuildResponseSchema()
        };

        return req;
    }

    private static string LanguageName(string code) =>
        code.ToLowerInvariant() switch
        {
            "da" => "Danish",
            "bg" => "Bulgarian",
            "en" => "English",
            "auto" => "the detected language",
            _ => code
        };

    private static string? ExtractTranslatedText(string? raw)
    {
        var jsonText = LlmJsonHelper.ExtractJsonObject(LlmJsonHelper.NormalizeJsonText(raw));
        if (string.IsNullOrWhiteSpace(jsonText)) return null;

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            if (!doc.RootElement.TryGetProperty("translatedText", out var el) ||
                el.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = el.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch
        {
            return null;
        }
    }
}
