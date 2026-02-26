using translator_proxy.Models;

namespace translator_proxy.Services;

public interface IExplainService
{
    Task<ExplainServiceResult> ExplainAsync(ExplainRequest? req, CancellationToken cancellationToken);
}

public sealed record ExplainServiceResult(int StatusCode, object Body);

