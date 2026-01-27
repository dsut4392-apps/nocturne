using Microsoft.AspNetCore.Mvc;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models.Injectables;

namespace Nocturne.API.Controllers;

/// <summary>
/// Controller for managing pen/vial inventory tracking.
/// Provides CRUD operations for tracking insulin pens and vials.
/// </summary>
[ApiController]
[Route("api/v4/pen-vials")]
[Produces("application/json")]
public class PenVialsController : ControllerBase
{
    private readonly IPenVialService _penVialService;
    private readonly ILogger<PenVialsController> _logger;

    public PenVialsController(
        IPenVialService penVialService,
        ILogger<PenVialsController> logger
    )
    {
        _penVialService = penVialService;
        _logger = logger;
    }

    /// <summary>
    /// Get all pens/vials with optional filtering.
    /// </summary>
    /// <param name="medicationId">Optional medication ID to filter by.</param>
    /// <param name="includeArchived">Whether to include archived pens/vials. Defaults to false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of pens/vials matching the filters.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PenVial>>> GetAll(
        [FromQuery] Guid? medicationId = null,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var penVials = await _penVialService.GetAllAsync(
                medicationId,
                includeArchived,
                cancellationToken
            );
            return Ok(penVials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pens/vials");
            return StatusCode(500, new { error = "An error occurred while retrieving pens/vials" });
        }
    }

    /// <summary>
    /// Get a specific pen/vial by ID.
    /// </summary>
    /// <param name="id">The pen/vial ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested pen/vial if found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PenVial>> GetById(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var penVial = await _penVialService.GetByIdAsync(id, cancellationToken);

            if (penVial == null)
            {
                return NotFound(new { error = $"Pen/vial not found: {id}" });
            }

            return Ok(penVial);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pen/vial: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the pen/vial" });
        }
    }

    /// <summary>
    /// Create a new pen/vial record.
    /// </summary>
    /// <param name="penVial">The pen/vial to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created pen/vial with assigned ID.</returns>
    [HttpPost]
    public async Task<ActionResult<PenVial>> Create(
        [FromBody] PenVial penVial,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (penVial.InjectableMedicationId == Guid.Empty)
            {
                return BadRequest(new { error = "Injectable medication ID is required" });
            }

            var created = await _penVialService.CreateAsync(penVial, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating pen/vial for medication: {MedicationId}",
                penVial.InjectableMedicationId
            );
            return StatusCode(500, new { error = "An error occurred while creating the pen/vial" });
        }
    }

    /// <summary>
    /// Update an existing pen/vial record.
    /// </summary>
    /// <param name="id">The pen/vial ID to update.</param>
    /// <param name="penVial">The updated pen/vial data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated pen/vial.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PenVial>> Update(
        Guid id,
        [FromBody] PenVial penVial,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (id != penVial.Id && penVial.Id != Guid.Empty)
            {
                return BadRequest(new { error = "ID in URL does not match ID in request body" });
            }

            // Ensure the ID from URL is used
            penVial.Id = id;

            if (penVial.InjectableMedicationId == Guid.Empty)
            {
                return BadRequest(new { error = "Injectable medication ID is required" });
            }

            var updated = await _penVialService.UpdateAsync(penVial, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pen/vial: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating the pen/vial" });
        }
    }

    /// <summary>
    /// Archive a pen/vial (soft delete).
    /// Archived pens/vials are hidden from normal queries but can be restored.
    /// </summary>
    /// <param name="id">The pen/vial ID to archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var success = await _penVialService.ArchiveAsync(id, cancellationToken);

            if (!success)
            {
                return NotFound(new { error = $"Pen/vial not found: {id}" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving pen/vial: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while archiving the pen/vial" });
        }
    }
}
