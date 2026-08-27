using Microsoft.AspNetCore.Mvc;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Services;

namespace ProteinTracker.Api.Controllers;

[ApiController]
[Route("api/daily-summary")]
public class DailySummaryController(DailySummaryService dailySummaryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DailySummaryResponse>> Get(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        return Ok(await dailySummaryService.GetAsync(date, cancellationToken));
    }
}
