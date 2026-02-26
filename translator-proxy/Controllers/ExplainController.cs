using Microsoft.AspNetCore.Mvc;
using translator_proxy.Models;
using translator_proxy.Services;

namespace translator_proxy.Controllers;

[ApiController]
public class ExplainController : ControllerBase
{
    private readonly IExplainService _explain;

    public ExplainController(IExplainService explain)
    {
        _explain = explain;
    }

    [HttpPost("~/explain/gemini")]
    public async Task<IActionResult> GeminiExplain([FromBody] ExplainRequest? req, CancellationToken cancellationToken)
    {
        var result = await _explain.ExplainAsync(req, cancellationToken);
        return StatusCode(result.StatusCode, result.Body);
    }
}

