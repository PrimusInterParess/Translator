using Microsoft.AspNetCore.Http;
using translator_proxy.Models;

namespace translator_proxy.Services.Llm;

internal static class DegreeComparisonRequestValidator
{
    internal static DegreeComparisonServiceResult? Validate(
        DegreeComparisonRequest? req,
        out DegreeComparisonValidatedInput input)
    {
        var text = (req?.Text ?? string.Empty).Trim();
        var targetLanguage = (req?.TargetLanguage ?? string.Empty).Trim();
        var translationIn = (req?.TranslationIn ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(targetLanguage)) targetLanguage = "Danish";
        if (string.IsNullOrWhiteSpace(translationIn)) translationIn = "en";

        input = new DegreeComparisonValidatedInput(
            CleanedWord: text,
            TargetLanguage: targetLanguage,
            TranslationIn: translationIn);

        if (string.IsNullOrWhiteSpace(text))
        {
            return new DegreeComparisonServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = LlmConstants.ErrMissingText }
            );
        }

        if (text.Length > LlmConstants.MaxDegreeComparisonTextLength)
        {
            return new DegreeComparisonServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(
                        LlmConstants.ErrTextTooLongFormat,
                        LlmConstants.MaxDegreeComparisonTextLength)
                }
            );
        }

        return null;
    }
}
