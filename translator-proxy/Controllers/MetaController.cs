using Microsoft.AspNetCore.Mvc;

namespace translator_proxy.Controllers;

[ApiController]
public class MetaController : ControllerBase
{
    [HttpGet("~/")]
    public IActionResult Root()
    {
        return Ok(new { ok = true, name = "translator-proxy" });
    }

    [HttpGet("~/health")]
    public IActionResult Health()
    {
        return Ok(new { ok = true });
    }
}

