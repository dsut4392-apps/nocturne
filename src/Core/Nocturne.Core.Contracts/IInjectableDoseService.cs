using Nocturne.Core.Models.Injectables;

namespace Nocturne.Core.Contracts;

/// <summary>
/// Domain service for injectable dose record operations.
/// </summary>
public interface IInjectableDoseService
{
    /// <summary>
    /// Get doses with optional filtering by time range and medication.
    /// </summary>
    /// <param name="fromMills">Optional start timestamp in Unix milliseconds.</param>
    /// <param name="toMills">Optional end timestamp in Unix milliseconds.</param>
    /// <param name="medicationId">Optional medication ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of injectable doses matching the filters.</returns>
    Task<IEnumerable<InjectableDose>> GetDosesAsync(
        long? fromMills = null,
        long? toMills = null,
        Guid? medicationId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get a specific dose by ID.
    /// </summary>
    /// <param name="id">Dose ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dose if found, null otherwise.</returns>
    Task<InjectableDose?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Create a new injectable dose record.
    /// </summary>
    /// <param name="dose">Dose to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created dose with assigned ID.</returns>
    Task<InjectableDose> CreateAsync(
        InjectableDose dose,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Update an existing injectable dose record.
    /// </summary>
    /// <param name="dose">Updated dose data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated dose.</returns>
    Task<InjectableDose> UpdateAsync(
        InjectableDose dose,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Delete an injectable dose record.
    /// </summary>
    /// <param name="id">Dose ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully, false otherwise.</returns>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get recent rapid-acting doses for IOB calculation.
    /// </summary>
    /// <param name="beforeMills">Timestamp before which to get doses in Unix milliseconds.</param>
    /// <param name="hoursBack">Number of hours back to search (default: 8 hours for typical DIA).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of recent rapid-acting doses.</returns>
    Task<IEnumerable<InjectableDose>> GetRecentRapidActingDosesAsync(
        long beforeMills,
        int hoursBack = 8,
        CancellationToken cancellationToken = default
    );
}
