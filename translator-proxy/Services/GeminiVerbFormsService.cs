using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using translator_proxy.Models;
using translator_proxy.Services.Gemini;

namespace translator_proxy.Services;

public sealed class GeminiVerbFormsService : IVerbFormsService
{
    private readonly IGeminiClient _gemini;
    private readonly IOptions<GeminiOptions> _geminiOptions;

    public GeminiVerbFormsService(IGeminiClient gemini, IOptions<GeminiOptions> geminiOptions)
    {
        _gemini = gemini;
        _geminiOptions = geminiOptions;
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

        var apiKey = (_geminiOptions.Value.ApiKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status500InternalServerError,
                Body: new { ok = false, error = GeminiConstants.ErrMissingApiKey }
            );
        }

        var model = (_geminiOptions.Value.Model ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(model)) model = "gemini-2.5-flash-lite";

        var systemInstruction = (_geminiOptions.Value.VerbForms.SystemInstruction ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(systemInstruction))
            systemInstruction = "Return ONLY valid JSON matching the provided schema.";

        var promptTemplate = _geminiOptions.Value.VerbForms.PromptTemplate ?? string.Empty;
        var prompt = BuildPrompt(promptTemplate, cleaned);

        var body = BuildGenerateContentRequest(systemInstruction, prompt);
        var result = await _gemini.GenerateContentAsync(model, body, cancellationToken);

        if (!result.Ok)
        {
            var msg = !string.IsNullOrWhiteSpace(result.Error)
                ? result.Error!
                : (result.UpstreamStatusCode is { } s ? $"{GeminiConstants.HttpStatusFallbackPrefix}{s}" : GeminiConstants.ErrUnexpectedApiResponse);

            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = msg }
            );
        }

        var candidateText = result.CandidateText;
        if (string.IsNullOrWhiteSpace(candidateText))
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = GeminiConstants.ErrGeminiEmptyResponse }
            );
        }

        if (!TryParseVerbForms(candidateText!, out var forms, out var parseError))
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

    private static JsonObject BuildGenerateContentRequest(string systemInstruction, string prompt)
    {
        // `generativelanguage.googleapis.com/v1` does not accept `systemInstruction` or
        // JSON schema/mime response fields. Fold the instruction into the user prompt.
        var fullPrompt = string.IsNullOrWhiteSpace(systemInstruction)
            ? prompt
            : $"{systemInstruction}\n\n{prompt}";

        return new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["text"] = fullPrompt }
                    }
                }
            }
        };
    }

    private static string BuildPrompt(string template, string verb)
    {
        var t = (template ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t))
        {
            return $"Verb: {verb}";
        }

        if (t.Contains("{verb}", StringComparison.OrdinalIgnoreCase))
        {
            return t.Replace("{verb}", verb);
        }

        return $"{t}\nVerb: {verb}";
    }

    private sealed record VerbFormsDto(
        [property: JsonPropertyName("infinitive")] string? Infinitive,
        [property: JsonPropertyName("present")] string? Present,
        [property: JsonPropertyName("past")] string? Past,
        [property: JsonPropertyName("pastParticiple")] string? PastParticiple,
        [property: JsonPropertyName("imperative")] string? Imperative
    );

    private sealed record VerbForms(
        string Infinitive,
        string Present,
        string Past,
        string PastParticiple,
        string Imperative
    );

    private static bool TryParseVerbForms(string jsonText, out VerbForms? forms, out string error)
    {
        forms = null;
        error = GeminiConstants.ErrGeminiBadJson;

        try
        {
            var dto = JsonSerializer.Deserialize<VerbFormsDto>(
                jsonText,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            );

            if (dto is null)
            {
                error = GeminiConstants.ErrGeminiBadJson;
                return false;
            }

            var infinitive = (dto.Infinitive ?? string.Empty).Trim();
            var present = (dto.Present ?? string.Empty).Trim();
            var past = (dto.Past ?? string.Empty).Trim();
            var pastParticiple = (dto.PastParticiple ?? string.Empty).Trim();
            var imperative = (dto.Imperative ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(infinitive) ||
                string.IsNullOrWhiteSpace(present) ||
                string.IsNullOrWhiteSpace(past) ||
                string.IsNullOrWhiteSpace(pastParticiple) ||
                string.IsNullOrWhiteSpace(imperative))
            {
                error = GeminiConstants.ErrUnexpectedApiResponse;
                return false;
            }

            forms = new VerbForms(
                Infinitive: NormalizeDanishVerb(infinitive),
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
            error = GeminiConstants.ErrGeminiBadJson;
            return false;
        }
    }
}

