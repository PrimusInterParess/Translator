using Microsoft.AspNetCore.Http;

namespace translator_proxy.Services.Llm;

internal static class ExplainResponseMapper
{
    internal static ExplainServiceResult FromLlmText(
        string? raw,
        ExplainValidatedInput input,
        string emptyResponseError)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new ExplainServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = emptyResponseError }
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
            Body: parsed!.ToApiBody(input.Text, input.Context, input.ExplainIn)
        );
    }
}
