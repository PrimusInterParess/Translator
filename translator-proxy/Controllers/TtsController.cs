using Microsoft.AspNetCore.Mvc;
using translator_proxy.Models;
using translator_proxy.Services;

namespace translator_proxy.Controllers;

[ApiController]
public class TtsController : ControllerBase
{
    private readonly ITtsService _tts;

    public TtsController(ITtsService tts)
    {
        _tts = tts;
    }

    [HttpPost("~/tts")]
    public async Task<IActionResult> Tts([FromBody] TtsRequest? req, CancellationToken cancellationToken)
    {
        var result = await _tts.SynthesizeAsync(req, cancellationToken);
        return StatusCode(result.StatusCode, result.Body);
    }
}

