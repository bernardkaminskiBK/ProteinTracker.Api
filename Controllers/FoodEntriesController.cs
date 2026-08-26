using Microsoft.AspNetCore.Mvc;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Services;

namespace ProteinTracker.Api.Controllers;

[ApiController]
[Route("api/food-entries")]
public class FoodEntriesController(FoodEntryService foodEntryService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FoodEntryResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await foodEntryService.GetByIdAsync(id, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<List<FoodEntryResponse>>> GetByDateRange(
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        return Ok(await foodEntryService.GetByDateRangeAsync(
            start,
            end,
            cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<FoodEntryResponse>> Create(
        CreateFoodEntryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await foodEntryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FoodEntryResponse>> Update(
        int id,
        UpdateFoodEntryRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await foodEntryService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await foodEntryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
