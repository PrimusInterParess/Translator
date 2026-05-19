using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using translator_proxy.Models;
using translator_proxy.Services.Gemini;
using translator_proxy.Services.Llm;

namespace translator_proxy.Services;

public sealed class GeminiExplainService : IExplainService
{
    private readonly IGeminiClient _gemini;
    private readonly IOptions<GeminiOptions> _geminiOptions;

    public GeminiExplainService(IGeminiClient gemini, IOptions<GeminiOptions> geminiOptions)
    {
        _gemini = gemini;
        _geminiOptions = geminiOptions;
    }

    public async Task<ExplainServiceResult> ExplainAsync(ExplainRequest? req, CancellationToken cancellationToken)
    {
        if (ExplainRequestValidator.Validate(req, out var input) is { } validationError)
        {
            return validationError;
        }

        var apiKey = (_geminiOptions.Value.ApiKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status500InternalServerError,
                Body: new { ok = false, error = GeminiConstants.ErrMissingApiKey }
            );
        }

        var model = (_geminiOptions.Value.Model ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(model)) model = "gemini-2.5-flash-lite";

        var systemInstruction = (_geminiOptions.Value.Explain.SystemInstruction ?? string.Empty).Trim();

        var promptTemplate = (_geminiOptions.Value.Explain.PromptTemplate ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(promptTemplate))
        {
            promptTemplate = new GeminiOptions.ExplainOptions().PromptTemplate;
        }

        var prompt = ExplainPromptBuilder.Build(promptTemplate, input);

        var body = BuildGenerateContentRequest(systemInstruction, prompt);
        var result = await _gemini.GenerateContentAsync(model, body, cancellationToken);

        if (!result.Ok)
        {
            var msg = !string.IsNullOrWhiteSpace(result.Error)
                ? result.Error!
                : (result.UpstreamStatusCode is { } s ? $"{GeminiConstants.HttpStatusFallbackPrefix}{s}" : GeminiConstants.ErrUnexpectedApiResponse);

            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = msg }
            );
        }

        return ExplainResponseMapper.FromLlmText(result.CandidateText, input, GeminiConstants.ErrGeminiEmptyResponse);
    }

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
            ["responseMimeType"] = "application/json",
            ["responseSchema"] = ExplainGeminiSchema.BuildResponseSchema()
        };

        return req;
    }

}

