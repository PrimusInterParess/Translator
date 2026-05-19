using System.Text.Json;
using System.Text.Json.Serialization;
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
        var text = (req?.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = GeminiConstants.ErrMissingText }
            );
        }

        if (text.Length > GeminiConstants.MaxTextLength)
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(GeminiConstants.ErrTextTooLongFormat, GeminiConstants.MaxTextLength)
                }
            );
        }

        var cleaned = NormalizeDanishVerb(text);

        var meaningIn = (req?.MeaningIn ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(meaningIn)) meaningIn = "en";

        if (!TryBuildSystemInstruction(_options.Value.VerbForms.SystemInstruction, out var systemInstruction, out var systemError))
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status500InternalServerError,
                Body: new { ok = false, error = systemError }
            );
        }

        if (!TryBuildPrompt(_options.Value.VerbForms.PromptTemplate, cleaned, meaningIn, out var prompt, out var promptError))
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

        var raw = LlmJsonHelper.NormalizeJsonText(result.Content);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = LlmConstants.ErrLlmEmptyResponse }
            );
        }

        if (!TryParseVerbForms(raw, out var forms, out var parseError))
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = parseError }
            );
        }

        return new VerbFormsServiceResult(
            StatusCode: StatusCodes.Status200OK,
            Body: new
            {
                ok = true,
                infinitive = forms!.Infinitive,
                meaning = forms.Meaning,
                present = forms.Present,
                past = forms.Past,
                pastParticiple = forms.PastParticiple,
                imperative = forms.Imperative
            }
        );
    }

    private static string NormalizeDanishVerb(string input)
    {
        var s = input.Trim();
        if (s.StartsWith("at ", StringComparison.OrdinalIgnoreCase))
            s = s[3..].Trim();
        return s;
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

    private static bool TryBuildPrompt(string? template, string verb, string meaningIn, out string prompt, out string error)
    {
        var t = (template ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t))
        {
            prompt = string.Empty;
            error = LlmConstants.ErrMissingOllamaVerbFormsPromptTemplate;
            return false;
        }

        var lang = string.IsNullOrWhiteSpace(meaningIn) ? "en" : meaningIn;
        prompt = t
            .Replace("{verb}", verb, StringComparison.OrdinalIgnoreCase)
            .Replace("{meaningIn}", lang, StringComparison.OrdinalIgnoreCase);

        if (!t.Contains("{verb}", StringComparison.OrdinalIgnoreCase))
            prompt = $"{prompt}\nVerb: {verb}";

        error = string.Empty;
        return true;
    }

    private sealed record VerbFormsDto(
        [property: JsonPropertyName("infinitive")] string? Infinitive,
        [property: JsonPropertyName("meaning")] string? Meaning,
        [property: JsonPropertyName("present")] string? Present,
        [property: JsonPropertyName("past")] string? Past,
        [property: JsonPropertyName("pastParticiple")] string? PastParticiple,
        [property: JsonPropertyName("imperative")] string? Imperative
    );

    private sealed record VerbForms(
        string Infinitive,
        string Meaning,
        string Present,
        string Past,
        string PastParticiple,
        string Imperative
    );

    private static bool TryParseVerbForms(string jsonText, out VerbForms? forms, out string error)
    {
        forms = null;
        error = LlmConstants.ErrLlmBadJson;

        try
        {
            var dto = JsonSerializer.Deserialize<VerbFormsDto>(
                jsonText,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            );

            if (dto is null)
            {
                error = LlmConstants.ErrLlmBadJson;
                return false;
            }

            var infinitive = (dto.Infinitive ?? string.Empty).Trim();
            var meaning = (dto.Meaning ?? string.Empty).Trim();
            var present = (dto.Present ?? string.Empty).Trim();
            var past = (dto.Past ?? string.Empty).Trim();
            var pastParticiple = (dto.PastParticiple ?? string.Empty).Trim();
            var imperative = (dto.Imperative ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(infinitive) ||
                string.IsNullOrWhiteSpace(meaning) ||
                string.IsNullOrWhiteSpace(present) ||
                string.IsNullOrWhiteSpace(past) ||
                string.IsNullOrWhiteSpace(pastParticiple) ||
                string.IsNullOrWhiteSpace(imperative))
            {
                error = LlmConstants.ErrUnexpectedApiResponse;
                return false;
            }

            forms = new VerbForms(
                Infinitive: NormalizeDanishVerb(infinitive),
                Meaning: meaning,
                Present: present,
                Past: past,
                PastParticiple: pastParticiple,
                Imperative: imperative
            );
            error = string.Empty;
            return true;
        }
        catch
        {
            error = LlmConstants.ErrLlmBadJson;
            return false;
        }
    }
}
