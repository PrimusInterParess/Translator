using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using translator_proxy.Models;
using translator_proxy.Services.Llm;
using translator_proxy.Services.Ollama;

namespace translator_proxy.Services;

public sealed class OllamaExplainService : IExplainService
{
    private readonly IOllamaClient _ollama;
    private readonly IOptions<OllamaOptions> _options;

    public OllamaExplainService(IOllamaClient ollama, IOptions<OllamaOptions> options)
    {
        _ollama = ollama;
        _options = options;
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

        var explainIn = (req?.ExplainIn ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(explainIn)) explainIn = "en";

        var sourceLang = (req?.SourceLang ?? string.Empty).Trim();

        if (!TryBuildSystemInstruction(_options.Value.Explain.SystemInstruction, out var systemInstruction, out var systemError))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status500InternalServerError,
                Body: new { ok = false, error = systemError }
            );
        }

        if (!TryBuildPrompt(_options.Value.Explain.PromptTemplate, text, context, sourceLang, explainIn, out var prompt, out var promptError))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status500InternalServerError,
                Body: new { ok = false, error = promptError }
            );
        }

        var result = await _ollama.ChatAsync(systemInstruction, prompt, jsonMode: true, cancellationToken);

        if (!result.Ok)
        {
            var msg = !string.IsNullOrWhiteSpace(result.Error)
                ? result.Error!
                : (result.UpstreamStatusCode is { } s
                    ? $"{LlmConstants.HttpStatusFallbackPrefix}{s}"
                    : LlmConstants.ErrUnexpectedApiResponse);

            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = msg }
            );
        }

        var raw = LlmJsonHelper.NormalizeJsonText(result.Content);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = LlmConstants.ErrLlmEmptyResponse }
            );
        }

        if (!ExplainJsonParser.TryParse(raw, out var parsed, out var parseError))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = parseError }
            );
        }

        return new ExplainServiceResult(
            StatusCode: StatusCodes.Status200OK,
            Body: parsed!.ToApiBody(text, context, explainIn)
        );
    }

    private static bool TryBuildSystemInstruction(string? baseInstruction, out string systemInstruction, out string error)
    {
        var instruction = (baseInstruction ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(instruction))
        {
            systemInstruction = string.Empty;
            error = LlmConstants.ErrMissingOllamaExplainSystemInstruction;
            return false;
        }

        systemInstruction = $"{instruction}\n\n{LlmConstants.ExplainJsonSchema}";
        error = string.Empty;
        return true;
    }

    private static bool TryBuildPrompt(
        string? template,
        string text,
        string context,
        string sourceLang,
        string explainIn,
        out string prompt,
        out string error)
    {
        var t = (template ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t))
        {
            prompt = string.Empty;
            error = LlmConstants.ErrMissingOllamaExplainPromptTemplate;
            return false;
        }

        var fragment = string.IsNullOrWhiteSpace(context) ? "(none)" : context;
        prompt = t
            .Replace("{sentence}", text)
            .Replace("{fragment}", fragment)
            .Replace("{text}", text)
            .Replace("{context}", fragment)
            .Replace("{sourceLang}", string.IsNullOrWhiteSpace(sourceLang) ? "(infer)" : sourceLang)
            .Replace("{explainIn}", string.IsNullOrWhiteSpace(explainIn) ? "en" : explainIn);
        error = string.Empty;
        return true;
    }

}
