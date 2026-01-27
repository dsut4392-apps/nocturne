using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models.Injectables;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Mappers;

namespace Nocturne.API.Services;

/// <summary>
/// Domain service implementation for pen/vial inventory tracking operations.
/// </summary>
public class PenVialService : IPenVialService
{
    private readonly NocturneDbContext _dbContext;
    private readonly ILogger<PenVialService> _logger;

    public PenVialService(NocturneDbContext dbContext, ILogger<PenVialService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PenVial>> GetAllAsync(
        Guid? medicationId = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug(
                "Getting all pens/vials with filters - medicationId: {MedicationId}, includeArchived: {IncludeArchived}",
                medicationId,
                includeArchived
            );

            var query = _dbContext.PenVials.AsNoTracking();

            if (medicationId.HasValue)
            {
                query = query.Where(p => p.InjectableMedicationId == medicationId.Value);
            }

            if (!includeArchived)
            {
                query = query.Where(p => !p.IsArchived);
            }

            var entities = await query
                .OrderByDescending(p => p.OpenedAt)
                .ToListAsync(cancellationToken);

            var penVials = entities.Select(PenVialMapper.ToDomainModel).ToList();

            _logger.LogDebug("Retrieved {Count} pens/vials", penVials.Count);
            return penVials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pens/vials");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PenVial?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Getting pen/vial by ID: {Id}", id);

            var entity = await _dbContext.PenVials
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogDebug("Pen/vial not found: {Id}", id);
                return null;
            }

            return PenVialMapper.ToDomainModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pen/vial by ID: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PenVial> CreateAsync(
        PenVial penVial,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug(
                "Creating pen/vial for medication: {MedicationId}",
                penVial.InjectableMedicationId
            );

            var entity = PenVialMapper.ToEntity(penVial);

            _dbContext.PenVials.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var createdPenVial = PenVialMapper.ToDomainModel(entity);
            _logger.LogInformation(
                "Created pen/vial: {Id} for medication {MedicationId}",
                createdPenVial.Id,
                createdPenVial.InjectableMedicationId
            );

            return createdPenVial;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating pen/vial for medication: {MedicationId}",
                penVial.InjectableMedicationId
            );
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PenVial> UpdateAsync(
        PenVial penVial,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Updating pen/vial: {Id}", penVial.Id);

            var entity = await _dbContext.PenVials
                .FirstOrDefaultAsync(p => p.Id == penVial.Id, cancellationToken);

            if (entity == null)
            {
                throw new InvalidOperationException($"Pen/vial not found: {penVial.Id}");
            }

            PenVialMapper.UpdateEntity(entity, penVial);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedPenVial = PenVialMapper.ToDomainModel(entity);
            _logger.LogInformation(
                "Updated pen/vial: {Id} - Status: {Status}, Remaining: {Remaining}",
                updatedPenVial.Id,
                updatedPenVial.Status,
                updatedPenVial.RemainingUnits
            );

            return updatedPenVial;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pen/vial: {Id}", penVial.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Archiving pen/vial: {Id}", id);

            var entity = await _dbContext.PenVials
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Pen/vial not found for archiving: {Id}", id);
                return false;
            }

            entity.IsArchived = true;
            entity.SysUpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Archived pen/vial: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving pen/vial: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PenVial?> DecrementUnitsAsync(
        Guid penVialId,
        double units,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Decrementing {Units} units from pen/vial: {Id}", units, penVialId);

            var entity = await _dbContext.PenVials
                .FirstOrDefaultAsync(p => p.Id == penVialId, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Pen/vial not found for decrement: {Id}", penVialId);
                return null;
            }

            // Subtract units from remaining
            entity.RemainingUnits = (entity.RemainingUnits ?? 0) - units;
            entity.SysUpdatedAt = DateTimeOffset.UtcNow;

            // Update status to Empty if remaining units <= 0
            if (entity.RemainingUnits <= 0)
            {
                entity.Status = PenVialStatus.Empty.ToString();
                _logger.LogInformation(
                    "Pen/vial {Id} is now empty (remaining: {Remaining})",
                    penVialId,
                    entity.RemainingUnits
                );
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedPenVial = PenVialMapper.ToDomainModel(entity);
            _logger.LogDebug(
                "Decremented pen/vial: {Id} - Remaining: {Remaining}, Status: {Status}",
                updatedPenVial.Id,
                updatedPenVial.RemainingUnits,
                updatedPenVial.Status
            );

            return updatedPenVial;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing units from pen/vial: {Id}", penVialId);
            throw;
        }
    }
}
