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
        if (ExplainRequestValidator.Validate(req, out var input) is { } validationError)
        {
            return validationError;
        }

        if (!TryBuildSystemInstruction(_options.Value.Explain.SystemInstruction, out var systemInstruction, out var systemError))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status500InternalServerError,
                Body: new { ok = false, error = systemError }
            );
        }

        if (!TryBuildPrompt(_options.Value.Explain.PromptTemplate, input, out var prompt, out var promptError))
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

        return ExplainResponseMapper.FromLlmText(result.Content, input, LlmConstants.ErrLlmEmptyResponse);
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

        // Ollama has no responseSchema field, so keep the JSON contract in the system instruction.
        systemInstruction = $"{instruction}\n\n{LlmConstants.ExplainJsonSchema}";
        error = string.Empty;
        return true;
    }

    private static bool TryBuildPrompt(
        string? template,
        ExplainValidatedInput input,
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

        prompt = ExplainPromptBuilder.Build(t, input);
        error = string.Empty;
        return true;
    }

}
