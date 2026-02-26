using translator_proxy.Models;

namespace translator_proxy.Services;

public interface IVerbFormsService
{
    Task<VerbFormsServiceResult> GetVerbFormsAsync(VerbFormsRequest? req, CancellationToken cancellationToken);
}

public sealed record VerbFormsServiceResult(int StatusCode, object Body);

