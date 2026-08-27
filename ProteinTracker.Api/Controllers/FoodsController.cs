using Microsoft.AspNetCore.Mvc;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Services;

namespace ProteinTracker.Api.Controllers;

[ApiController]
[Route("api/foods")]
public class FoodsController(FoodService foodService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<FoodResponse>>> GetAllActive(
        CancellationToken cancellationToken)
    {
        return Ok(await foodService.GetAllActiveAsync(cancellationToken));
    }

    [HttpGet("archived")]
    public async Task<ActionResult<List<FoodResponse>>> GetAllArchived(
        CancellationToken cancellationToken)
    {
        return Ok(await foodService.GetAllArchivedAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FoodResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await foodService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<FoodResponse>> Create(
        CreateFoodRequest request,
        CancellationToken cancellationToken)
    {
        var response = await foodService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FoodResponse>> Update(
        int id,
        UpdateFoodRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await foodService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:int}/archive")]
    public async Task<ActionResult<FoodResponse>> Archive(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await foodService.ArchiveAsync(id, cancellationToken));
    }

    [HttpPatch("{id:int}/restore")]
    public async Task<ActionResult<FoodResponse>> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await foodService.RestoreAsync(id, cancellationToken));
    }
}
