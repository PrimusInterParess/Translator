using translator_proxy.Models;

namespace translator_proxy.Services;

public interface ITranslateService
{
    Task<TranslateServiceResult> TranslateAsync(TranslateRequest? req, CancellationToken cancellationToken);
}

public sealed record TranslateServiceResult(int StatusCode, object Body);

