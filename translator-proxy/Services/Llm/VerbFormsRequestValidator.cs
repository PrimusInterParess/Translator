using Microsoft.AspNetCore.Http;
using translator_proxy.Models;

namespace translator_proxy.Services.Llm;

internal static class VerbFormsRequestValidator
{
    internal static VerbFormsServiceResult? Validate(VerbFormsRequest? req, out VerbFormsValidatedInput input)
    {
        var text = (req?.Text ?? string.Empty).Trim();
        var meaningIn = (req?.MeaningIn ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(meaningIn)) meaningIn = "en";

        input = new VerbFormsValidatedInput(
            CleanedVerb: DanishVerbNormalizer.Normalize(text),
            MeaningIn: meaningIn);

        if (string.IsNullOrWhiteSpace(text))
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = LlmConstants.ErrMissingText }
            );
        }

        if (text.Length > LlmConstants.MaxVerbFormsTextLength)
        {
            return new VerbFormsServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(LlmConstants.ErrTextTooLongFormat, LlmConstants.MaxVerbFormsTextLength)
                }
            );
        }

        return null;
    }
}
