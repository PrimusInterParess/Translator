using Microsoft.AspNetCore.Mvc;
using translator_proxy.Models;
using translator_proxy.Services;

namespace translator_proxy.Controllers;

[ApiController]
public class DegreeComparisonController : ControllerBase
{
    private readonly IDegreeComparisonService _degreeComparison;

    public DegreeComparisonController(IDegreeComparisonService degreeComparison)
    {
        _degreeComparison = degreeComparison;
    }

    [HttpPost("~/degreecomparison")]
    public async Task<IActionResult> DegreeComparison(
        [FromBody] DegreeComparisonRequest? req,
        CancellationToken cancellationToken)
    {
        var result = await _degreeComparison.GetDegreeComparisonAsync(req, cancellationToken);
        return StatusCode(result.StatusCode, result.Body);
    }
}
