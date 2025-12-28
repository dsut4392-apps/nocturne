using Microsoft.EntityFrameworkCore;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Repositories;

namespace Nocturne.API.Services;

/// <summary>
/// Domain service implementation for treatment food breakdown operations.
/// </summary>
public class TreatmentFoodService : ITreatmentFoodService
{
    private readonly NocturneDbContext _context;
    private readonly TreatmentFoodRepository _treatmentFoodRepository;
    private readonly ILogger<TreatmentFoodService> _logger;

    public TreatmentFoodService(
        NocturneDbContext context,
        TreatmentFoodRepository treatmentFoodRepository,
        ILogger<TreatmentFoodService> logger
    )
    {
        _context = context;
        _treatmentFoodRepository = treatmentFoodRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TreatmentFoodBreakdown?> GetByTreatmentIdAsync(
        Guid treatmentId,
        CancellationToken cancellationToken = default
    )
    {
        var treatmentEntity = await _context
            .Treatments.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == treatmentId, cancellationToken);

        if (treatmentEntity == null)
        {
            return null;
        }

        var entries = await _treatmentFoodRepository.GetByTreatmentIdAsync(
            treatmentId,
            cancellationToken
        );

        var attributedCarbs = entries.Sum(entry => entry.Carbs);
        var treatmentCarbs = treatmentEntity.Carbs.HasValue
            ? (decimal)treatmentEntity.Carbs.Value
            : 0m;

        return new TreatmentFoodBreakdown
        {
            TreatmentId = treatmentEntity.Id,
            Foods = entries.ToList(),
            IsAttributed = entries.Count > 0,
            AttributedCarbs = attributedCarbs,
            UnspecifiedCarbs = treatmentCarbs - attributedCarbs,
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TreatmentFood>> GetByTreatmentIdsAsync(
        IEnumerable<Guid> treatmentIds,
        CancellationToken cancellationToken = default
    )
    {
        return await _treatmentFoodRepository.GetByTreatmentIdsAsync(
            treatmentIds,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<TreatmentFood> AddAsync(
        TreatmentFood entry,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Creating treatment food entry for treatment {TreatmentId}", entry.TreatmentId);
        return await _treatmentFoodRepository.CreateAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TreatmentFood?> UpdateAsync(
        TreatmentFood entry,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Updating treatment food entry {EntryId}", entry.Id);
        return await _treatmentFoodRepository.UpdateAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting treatment food entry {EntryId}", id);
        return await _treatmentFoodRepository.DeleteAsync(id, cancellationToken);
    }
}
