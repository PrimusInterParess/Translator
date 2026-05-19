using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using translator_proxy.Models;
using translator_proxy.Services.Llm;
using translator_proxy.Services.Ollama;

namespace translator_proxy.Services;

public sealed class OllamaVerbFormsService : IVerbFormsService
{
    private readonly IOllamaClient _ollama;
    private readonly IOptions<OllamaOptions> _options;

    public OllamaVerbFormsService(IOllamaClient ollama, IOptions<OllamaOptions> options)
    {
        _ollama = ollama;
        _options = options;
    }

    public async Task<VerbFormsServiceResult> GetVerbFormsAsync(VerbFormsRequest? req, CancellationToken cancellationToken)
    {
        if (VerbFormsRequestValidator.Validate(req, out var input) is { } validationError)
        {
            return validationError;
        }

        if (!TryBuildSystemInstruction(_options.Value.VerbForms.SystemInstruction, out var systemInstruction, out var systemError))
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status500InternalServerError,
                Body: new { ok = false, error = systemError }
            );
        }

        if (!VerbFormsPromptBuilder.TryBuildRequired(_options.Value.VerbForms.PromptTemplate, input, out var prompt, out var promptError))
        {
            return new VerbFormsServiceResult(
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

            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = msg }
            );
        }

        return VerbFormsResponseMapper.FromLlmText(
            result.Content,
            LlmConstants.ErrLlmEmptyResponse,
            LlmConstants.ErrLlmBadJson);
    }

    private static bool TryBuildSystemInstruction(string? baseInstruction, out string systemInstruction, out string error)
    {
        var instruction = (baseInstruction ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(instruction))
        {
            systemInstruction = string.Empty;
            error = LlmConstants.ErrMissingOllamaVerbFormsSystemInstruction;
            return false;
        }

        systemInstruction = $"{instruction}\n\n{LlmConstants.VerbFormsJsonSchema}";
        error = string.Empty;
        return true;
    }
}
