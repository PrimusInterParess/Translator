using translator_proxy.Models;

namespace translator_proxy.Services;

public interface IDegreeComparisonService
{
    Task<DegreeComparisonServiceResult> GetDegreeComparisonAsync(
        DegreeComparisonRequest? req,
        CancellationToken cancellationToken);
}

public sealed record DegreeComparisonServiceResult(int StatusCode, object Body);
