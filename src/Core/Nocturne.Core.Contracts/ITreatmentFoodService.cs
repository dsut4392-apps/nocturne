using Nocturne.Core.Models;

namespace Nocturne.Core.Contracts;

/// <summary>
/// Domain service for treatment food breakdown operations.
/// </summary>
public interface ITreatmentFoodService
{
    /// <summary>
    /// Get food breakdown for a treatment.
    /// </summary>
    Task<TreatmentFoodBreakdown?> GetByTreatmentIdAsync(
        Guid treatmentId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get food breakdown entries for multiple treatments.
    /// </summary>
    Task<IEnumerable<TreatmentFood>> GetByTreatmentIdsAsync(
        IEnumerable<Guid> treatmentIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Add a new food breakdown entry.
    /// </summary>
    Task<TreatmentFood> AddAsync(
        TreatmentFood entry,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Update an existing food breakdown entry.
    /// </summary>
    Task<TreatmentFood?> UpdateAsync(
        TreatmentFood entry,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Delete a food breakdown entry.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
