using Microsoft.AspNetCore.Http;
using translator_proxy.Models;

namespace translator_proxy.Services.Llm;

internal static class ExplainRequestValidator
{
    internal static ExplainServiceResult? Validate(ExplainRequest? req, out ExplainValidatedInput input)
    {
        var text = (req?.Text ?? string.Empty).Trim();
        var context = (req?.Context ?? string.Empty).Trim();
        var sourceLang = (req?.SourceLang ?? string.Empty).Trim();
        var explainIn = (req?.ExplainIn ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(explainIn)) explainIn = "en";

        input = new ExplainValidatedInput(text, context, sourceLang, explainIn);

        if (string.IsNullOrWhiteSpace(text))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new { ok = false, error = LlmConstants.ErrMissingText }
            );
        }

        if (text.Length > LlmConstants.MaxExplainTextLength)
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(LlmConstants.ErrTextTooLongFormat, LlmConstants.MaxExplainTextLength)
                }
            );
        }

        if (context.Length > LlmConstants.MaxExplainContextLength)
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status400BadRequest,
                Body: new
                {
                    ok = false,
                    error = string.Format(LlmConstants.ErrContextTooLongFormat, LlmConstants.MaxExplainContextLength)
                }
            );
        }

        return null;
    }
}
