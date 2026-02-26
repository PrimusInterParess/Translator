using Microsoft.AspNetCore.Mvc;
using translator_proxy.Models;
using translator_proxy.Services; 

namespace translator_proxy.Controllers;

[ApiController]
public class VerbFormsController : ControllerBase
{
    private readonly IVerbFormsService _verbs;

    public VerbFormsController(IVerbFormsService verbs)
    {
        _verbs = verbs;
    }

    [HttpPost("~/verbforms/gemini")]
    public async Task<IActionResult> GeminiVerbForms([FromBody] VerbFormsRequest? req, CancellationToken cancellationToken)
    {
        var result = await _verbs.GetVerbFormsAsync(req, cancellationToken);
        return StatusCode(result.StatusCode, result.Body);
    }
}

