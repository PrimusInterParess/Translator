using Microsoft.AspNetCore.Mvc;
using translator_proxy.Models;
using translator_proxy.Services;

namespace translator_proxy.Controllers;

[ApiController]
public class TranslateController : ControllerBase
{
    private readonly ITranslateService _translate;
    private readonly GeminiTranslateService _gemini;

    public TranslateController(ITranslateService translate, GeminiTranslateService gemini)
    {
        _translate = translate;
        _gemini = gemini;
    }

    [HttpPost("~/translate/mymemory")]
    public async Task<IActionResult> MyMemoryTranslate([FromBody] TranslateRequest? req, CancellationToken cancellationToken)
    {
        var result = await _translate.TranslateAsync(req, cancellationToken);
        return StatusCode(result.StatusCode, result.Body);
    }

    [HttpPost("~/translate/gemini")]
    public async Task<IActionResult> GeminiTranslate([FromBody] TranslateRequest? req, CancellationToken cancellationToken)
    {
        var result = await _gemini.TranslateAsync(req, cancellationToken);
        return StatusCode(result.StatusCode, result.Body);
    }
}

