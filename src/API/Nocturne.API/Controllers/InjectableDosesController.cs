using Microsoft.AspNetCore.Mvc;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models.Injectables;

namespace Nocturne.API.Controllers;

/// <summary>
/// Controller for managing injectable dose records.
/// Provides CRUD operations for tracking administered injection doses.
/// </summary>
[ApiController]
[Route("api/v4/injectable-doses")]
[Produces("application/json")]
public class InjectableDosesController : ControllerBase
{
    private readonly IInjectableDoseService _doseService;
    private readonly ILogger<InjectableDosesController> _logger;

    public InjectableDosesController(
        IInjectableDoseService doseService,
        ILogger<InjectableDosesController> logger
    )
    {
        _doseService = doseService;
        _logger = logger;
    }

    /// <summary>
    /// Get injectable doses with optional filtering.
    /// </summary>
    /// <param name="fromMills">Optional start timestamp in Unix milliseconds.</param>
    /// <param name="toMills">Optional end timestamp in Unix milliseconds.</param>
    /// <param name="medicationId">Optional medication ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of injectable doses matching the filters.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InjectableDose>>> GetAll(
        [FromQuery] long? fromMills = null,
        [FromQuery] long? toMills = null,
        [FromQuery] Guid? medicationId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var doses = await _doseService.GetDosesAsync(
                fromMills,
                toMills,
                medicationId,
                cancellationToken
            );
            return Ok(doses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting injectable doses");
            return StatusCode(500, new { error = "An error occurred while retrieving doses" });
        }
    }

    /// <summary>
    /// Get a specific injectable dose by ID.
    /// </summary>
    /// <param name="id">The dose ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested dose if found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InjectableDose>> GetById(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var dose = await _doseService.GetByIdAsync(id, cancellationToken);

            if (dose == null)
            {
                return NotFound(new { error = $"Injectable dose not found: {id}" });
            }

            return Ok(dose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting injectable dose: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the dose" });
        }
    }

    /// <summary>
    /// Create a new injectable dose record.
    /// </summary>
    /// <param name="dose">The dose to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created dose with assigned ID.</returns>
    [HttpPost]
    public async Task<ActionResult<InjectableDose>> Create(
        [FromBody] InjectableDose dose,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (dose.InjectableMedicationId == Guid.Empty)
            {
                return BadRequest(new { error = "Injectable medication ID is required" });
            }

            if (dose.Units <= 0)
            {
                return BadRequest(new { error = "Units must be greater than zero" });
            }

            if (dose.Timestamp <= 0)
            {
                return BadRequest(new { error = "Timestamp is required" });
            }

            var created = await _doseService.CreateAsync(dose, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating injectable dose for medication: {MedicationId}",
                dose.InjectableMedicationId
            );
            return StatusCode(500, new { error = "An error occurred while creating the dose" });
        }
    }

    /// <summary>
    /// Update an existing injectable dose record.
    /// </summary>
    /// <param name="id">The dose ID to update.</param>
    /// <param name="dose">The updated dose data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated dose.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InjectableDose>> Update(
        Guid id,
        [FromBody] InjectableDose dose,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (id != dose.Id && dose.Id != Guid.Empty)
            {
                return BadRequest(new { error = "ID in URL does not match ID in request body" });
            }

            // Ensure the ID from URL is used
            dose.Id = id;

            if (dose.InjectableMedicationId == Guid.Empty)
            {
                return BadRequest(new { error = "Injectable medication ID is required" });
            }

            if (dose.Units <= 0)
            {
                return BadRequest(new { error = "Units must be greater than zero" });
            }

            var updated = await _doseService.UpdateAsync(dose, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating injectable dose: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating the dose" });
        }
    }

    /// <summary>
    /// Delete an injectable dose record.
    /// </summary>
    /// <param name="id">The dose ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var success = await _doseService.DeleteAsync(id, cancellationToken);

            if (!success)
            {
                return NotFound(new { error = $"Injectable dose not found: {id}" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting injectable dose: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the dose" });
        }
    }
}
