using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models.Injectables;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Mappers;

namespace Nocturne.API.Services;

/// <summary>
/// Domain service implementation for injectable medication catalog operations.
/// </summary>
public class InjectableMedicationService : IInjectableMedicationService
{
    private readonly NocturneDbContext _dbContext;
    private readonly ILogger<InjectableMedicationService> _logger;

    public InjectableMedicationService(
        NocturneDbContext dbContext,
        ILogger<InjectableMedicationService> logger
    )
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InjectableMedication>> GetAllAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug(
                "Getting all injectable medications, includeArchived: {IncludeArchived}",
                includeArchived
            );

            var query = _dbContext.InjectableMedications.AsNoTracking();

            if (!includeArchived)
            {
                query = query.Where(m => !m.IsArchived);
            }

            var entities = await query
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.Name)
                .ToListAsync(cancellationToken);

            var medications = entities.Select(InjectableMedicationMapper.ToDomainModel).ToList();

            _logger.LogDebug("Retrieved {Count} injectable medications", medications.Count);
            return medications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting injectable medications");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<InjectableMedication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Getting injectable medication by ID: {Id}", id);

            var entity = await _dbContext.InjectableMedications
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogDebug("Injectable medication not found: {Id}", id);
                return null;
            }

            return InjectableMedicationMapper.ToDomainModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting injectable medication by ID: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<InjectableMedication> CreateAsync(
        InjectableMedication medication,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Creating injectable medication: {Name}", medication.Name);

            var entity = InjectableMedicationMapper.ToEntity(medication);

            _dbContext.InjectableMedications.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var createdMedication = InjectableMedicationMapper.ToDomainModel(entity);
            _logger.LogInformation(
                "Created injectable medication: {Id} - {Name}",
                createdMedication.Id,
                createdMedication.Name
            );

            return createdMedication;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating injectable medication: {Name}", medication.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<InjectableMedication> UpdateAsync(
        InjectableMedication medication,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Updating injectable medication: {Id}", medication.Id);

            var entity = await _dbContext.InjectableMedications
                .FirstOrDefaultAsync(m => m.Id == medication.Id, cancellationToken);

            if (entity == null)
            {
                throw new InvalidOperationException(
                    $"Injectable medication not found: {medication.Id}"
                );
            }

            InjectableMedicationMapper.UpdateEntity(entity, medication);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedMedication = InjectableMedicationMapper.ToDomainModel(entity);
            _logger.LogInformation(
                "Updated injectable medication: {Id} - {Name}",
                updatedMedication.Id,
                updatedMedication.Name
            );

            return updatedMedication;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating injectable medication: {Id}", medication.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Archiving injectable medication: {Id}", id);

            var entity = await _dbContext.InjectableMedications
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Injectable medication not found for archiving: {Id}", id);
                return false;
            }

            entity.IsArchived = true;
            entity.SysUpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Archived injectable medication: {Id} - {Name}",
                id,
                entity.Name
            );
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving injectable medication: {Id}", id);
            throw;
        }
    }
}
