using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using translator_proxy.Models;
using translator_proxy.Services.Gemini;

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
        var text = (req?.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = GeminiConstants.ErrMissingText }
            );
        }

        if (text.Length > GeminiConstants.MaxExplainTextLength)
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(GeminiConstants.ErrTextTooLongFormat, GeminiConstants.MaxExplainTextLength)
                }
            );
        }

        var context = (req?.Context ?? string.Empty).Trim();
        if (context.Length > GeminiConstants.MaxExplainContextLength)
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(GeminiConstants.ErrContextTooLongFormat, GeminiConstants.MaxExplainContextLength)
                }
            );
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

        var explainIn = (req?.ExplainIn ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(explainIn)) explainIn = "en";

        var sourceLang = (req?.SourceLang ?? string.Empty).Trim();

        var systemInstruction = (_geminiOptions.Value.Explain.SystemInstruction ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(systemInstruction))
        {
            systemInstruction =
                "You explain phrases or partial sentences. " +
                "Return ONLY valid JSON matching the provided schema. " +
                "Do not include markdown or extra keys.";
        }

        var promptTemplate = _geminiOptions.Value.Explain.PromptTemplate ?? string.Empty;
        var prompt = BuildPrompt(promptTemplate, text, context, sourceLang, explainIn);

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

        var candidateText = result.CandidateText;
        if (string.IsNullOrWhiteSpace(candidateText))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = GeminiConstants.ErrGeminiEmptyResponse }
            );
        }

        if (!TryParseExplain(candidateText!, out var parsed, out var parseError))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = parseError }
            );
        }

        return new ExplainServiceResult(
            StatusCode: StatusCodes.Status200OK,
            Body: new
            {
                ok = true,
                summary = parsed!.Summary,
                what = parsed.What,
                when = parsed.When,
                why = parsed.Why,
                notes = parsed.Notes,
                alternatives = parsed.Alternatives,
                examples = parsed.Examples
            }
        );
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
            ["responseSchema"] = new JsonObject
            {
                ["type"] = "OBJECT",
                ["properties"] = new JsonObject
                {
                    ["summary"] = new JsonObject { ["type"] = "STRING" },
                    ["what"] = new JsonObject { ["type"] = "STRING" },
                    ["when"] = new JsonObject { ["type"] = "STRING" },
                    ["why"] = new JsonObject { ["type"] = "STRING" },
                    ["notes"] = new JsonObject
                    {
                        ["type"] = "ARRAY",
                        ["items"] = new JsonObject { ["type"] = "STRING" }
                    },
                    ["alternatives"] = new JsonObject
                    {
                        ["type"] = "ARRAY",
                        ["items"] = new JsonObject { ["type"] = "STRING" }
                    },
                    ["examples"] = new JsonObject
                    {
                        ["type"] = "ARRAY",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "OBJECT",
                            ["properties"] = new JsonObject
                            {
                                ["source"] = new JsonObject { ["type"] = "STRING" },
                                ["meaning"] = new JsonObject { ["type"] = "STRING" }
                            },
                            ["required"] = new JsonArray { "source", "meaning" }
                        }
                    }
                },
                ["required"] = new JsonArray
                {
                    "summary",
                    "what",
                    "when",
                    "why",
                    "notes",
                    "alternatives",
                    "examples"
                }
            }
        };

        return req;
    }

    private static string BuildPrompt(string template, string text, string context, string sourceLang, string explainIn)
    {
        var t = (template ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t))
        {
            t =
                "Explain the phrase/fragment.\n" +
                "Explain in: {explainIn}\n" +
                "Source language (if known): {sourceLang}\n" +
                "Text: {text}\n" +
                "Context: {context}";
        }

        return t
            .Replace("{text}", text)
            .Replace("{context}", string.IsNullOrWhiteSpace(context) ? "(none)" : context)
            .Replace("{sourceLang}", string.IsNullOrWhiteSpace(sourceLang) ? "(infer)" : sourceLang)
            .Replace("{explainIn}", string.IsNullOrWhiteSpace(explainIn) ? "en" : explainIn);
    }

    private sealed record ExplainDto(
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("what")] string? What,
        [property: JsonPropertyName("when")] string? When,
        [property: JsonPropertyName("why")] string? Why,
        [property: JsonPropertyName("notes")] string[]? Notes,
        [property: JsonPropertyName("alternatives")] string[]? Alternatives,
        [property: JsonPropertyName("examples")] ExplainExampleDto[]? Examples
    );

    private sealed record ExplainExampleDto(
        [property: JsonPropertyName("source")] string? Source,
        [property: JsonPropertyName("meaning")] string? Meaning
    );

    private sealed record ExplainParsed(
        string Summary,
        string What,
        string When,
        string Why,
        string[] Notes,
        string[] Alternatives,
        object[] Examples
    );

    private static bool TryParseExplain(string jsonText, out ExplainParsed? parsed, out string error)
    {
        parsed = null;
        error = GeminiConstants.ErrGeminiBadJson;

        try
        {
            var dto = JsonSerializer.Deserialize<ExplainDto>(
                jsonText,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            );

            if (dto is null)
            {
                error = GeminiConstants.ErrGeminiBadJson;
                return false;
            }

            var summary = (dto.Summary ?? string.Empty).Trim();
            var what = (dto.What ?? string.Empty).Trim();
            var when = (dto.When ?? string.Empty).Trim();
            var why = (dto.Why ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(summary) ||
                string.IsNullOrWhiteSpace(what) ||
                string.IsNullOrWhiteSpace(when) ||
                string.IsNullOrWhiteSpace(why))
            {
                error = GeminiConstants.ErrUnexpectedApiResponse;
                return false;
            }

            var notes = dto.Notes?.Select(v => (v ?? string.Empty).Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray()
                        ?? Array.Empty<string>();
            var alternatives = dto.Alternatives?.Select(v => (v ?? string.Empty).Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray()
                              ?? Array.Empty<string>();

            var examples = (dto.Examples ?? Array.Empty<ExplainExampleDto>())
                .Select(e => new
                {
                    source = (e.Source ?? string.Empty).Trim(),
                    meaning = (e.Meaning ?? string.Empty).Trim()
                })
                .Where(e => !string.IsNullOrWhiteSpace(e.source) && !string.IsNullOrWhiteSpace(e.meaning))
                .Cast<object>()
                .ToArray();

            parsed = new ExplainParsed(
                Summary: summary,
                What: what,
                When: when,
                Why: why,
                Notes: notes,
                Alternatives: alternatives,
                Examples: examples
            );

            error = string.Empty;
            return true;
        }
        catch
        {
            error = GeminiConstants.ErrGeminiBadJson;
            return false;
        }
    }
}

