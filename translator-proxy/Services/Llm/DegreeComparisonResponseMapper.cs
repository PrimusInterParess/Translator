using Microsoft.AspNetCore.Http;

namespace translator_proxy.Services.Llm;

internal static class DegreeComparisonResponseMapper
{
    internal static DegreeComparisonServiceResult FromLlmText(
        string? raw,
        string emptyResponseError,
        string badJsonError)
    {
        var jsonText = LlmJsonHelper.NormalizeJsonText(raw);
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            return new DegreeComparisonServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = emptyResponseError }
            );
        }

        if (!DegreeComparisonJsonParser.TryParse(jsonText, badJsonError, out var parsed, out var parseError))
        {
            return new DegreeComparisonServiceResult(
                StatusCode: StatusCodes.Status502BadGateway,
                Body: new { ok = false, error = parseError }
            );
        }

        return new DegreeComparisonServiceResult(
            StatusCode: StatusCodes.Status200OK,
            Body: parsed!.ToApiBody()
        );
    }
}
