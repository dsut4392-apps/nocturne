using Nocturne.Core.Models.Injectables;

namespace Nocturne.Core.Contracts;

/// <summary>
/// Domain service for pen/vial inventory tracking operations.
/// </summary>
public interface IPenVialService
{
    /// <summary>
    /// Get all pens/vials with optional filtering by medication and archived status.
    /// </summary>
    /// <param name="medicationId">Optional medication ID filter.</param>
    /// <param name="includeArchived">Whether to include archived pens/vials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of pens/vials matching the filters.</returns>
    Task<IEnumerable<PenVial>> GetAllAsync(
        Guid? medicationId = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get a specific pen/vial by ID.
    /// </summary>
    /// <param name="id">Pen/vial ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pen/vial if found, null otherwise.</returns>
    Task<PenVial?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Create a new pen/vial record.
    /// </summary>
    /// <param name="penVial">Pen/vial to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created pen/vial with assigned ID.</returns>
    Task<PenVial> CreateAsync(
        PenVial penVial,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Update an existing pen/vial record.
    /// </summary>
    /// <param name="penVial">Updated pen/vial data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated pen/vial.</returns>
    Task<PenVial> UpdateAsync(
        PenVial penVial,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Archive a pen/vial (soft delete).
    /// </summary>
    /// <param name="id">Pen/vial ID to archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if archived successfully, false otherwise.</returns>
    Task<bool> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Decrement remaining units when a dose is logged from this pen/vial.
    /// </summary>
    /// <param name="penVialId">Pen/vial ID to decrement.</param>
    /// <param name="units">Number of units to decrement.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated pen/vial if successful, null if not found or insufficient units.</returns>
    Task<PenVial?> DecrementUnitsAsync(
        Guid penVialId,
        double units,
        CancellationToken cancellationToken = default
    );
}
