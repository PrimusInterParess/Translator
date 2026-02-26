using translator_proxy.Models;

namespace translator_proxy.Services;

public interface ITtsService
{
    Task<TtsServiceResult> SynthesizeAsync(TtsRequest? req, CancellationToken cancellationToken);
}

public sealed record TtsServiceResult(int StatusCode, object Body);

