using Microsoft.AspNetCore.Mvc;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Services;

namespace ProteinTracker.Api.Controllers;

[ApiController]
[Route("api/daily-target")]
public class DailyTargetController(DailyTargetService dailyTargetService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DailyTargetResponse>> GetCurrent(
        CancellationToken cancellationToken)
    {
        return Ok(await dailyTargetService.GetCurrentAsync(cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult<DailyTargetResponse>> Update(
        UpdateDailyTargetRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await dailyTargetService.UpdateAsync(request, cancellationToken));
    }
}
