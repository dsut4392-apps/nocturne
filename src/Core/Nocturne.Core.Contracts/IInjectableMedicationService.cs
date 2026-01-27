using Nocturne.Core.Models.Injectables;

namespace Nocturne.Core.Contracts;

/// <summary>
/// Domain service for injectable medication catalog operations.
/// </summary>
public interface IInjectableMedicationService
{
    /// <summary>
    /// Get all injectable medications with optional archived filtering.
    /// </summary>
    /// <param name="includeArchived">Whether to include archived medications.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of injectable medications.</returns>
    Task<IEnumerable<InjectableMedication>> GetAllAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get a specific injectable medication by ID.
    /// </summary>
    /// <param name="id">Medication ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Medication if found, null otherwise.</returns>
    Task<InjectableMedication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Create a new injectable medication.
    /// </summary>
    /// <param name="medication">Medication to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created medication with assigned ID.</returns>
    Task<InjectableMedication> CreateAsync(
        InjectableMedication medication,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Update an existing injectable medication.
    /// </summary>
    /// <param name="medication">Updated medication data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated medication.</returns>
    Task<InjectableMedication> UpdateAsync(
        InjectableMedication medication,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Archive an injectable medication (soft delete).
    /// </summary>
    /// <param name="id">Medication ID to archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if archived successfully, false otherwise.</returns>
    Task<bool> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
