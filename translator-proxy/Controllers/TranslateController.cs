using Microsoft.AspNetCore.Mvc;
using translator_proxy.Models;
using translator_proxy.Services;

namespace translator_proxy.Controllers;

[ApiController]
public class TranslateController : ControllerBase
{
    private readonly ITranslateService _translate;

    public TranslateController(ITranslateService translate)
    {
        _translate = translate;
    }

    [HttpPost("~/translate/mymemory")]
    public async Task<IActionResult> MyMemoryTranslate([FromBody] TranslateRequest? req, CancellationToken cancellationToken)
    {
        var result = await _translate.TranslateAsync(req, cancellationToken);
        return StatusCode(result.StatusCode, result.Body);
    }
}

