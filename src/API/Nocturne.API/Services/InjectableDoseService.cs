using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nocturne.Core.Contracts;
using Nocturne.Core.Models.Injectables;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Mappers;

namespace Nocturne.API.Services;

/// <summary>
/// Domain service implementation for injectable dose record operations.
/// </summary>
public class InjectableDoseService : IInjectableDoseService
{
    private readonly NocturneDbContext _dbContext;
    private readonly ILogger<InjectableDoseService> _logger;

    public InjectableDoseService(
        NocturneDbContext dbContext,
        ILogger<InjectableDoseService> logger
    )
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InjectableDose>> GetDosesAsync(
        long? fromMills = null,
        long? toMills = null,
        Guid? medicationId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug(
                "Getting injectable doses with filters - fromMills: {FromMills}, toMills: {ToMills}, medicationId: {MedicationId}",
                fromMills,
                toMills,
                medicationId
            );

            var query = _dbContext.InjectableDoses.AsNoTracking();

            if (fromMills.HasValue)
            {
                query = query.Where(d => d.Timestamp >= fromMills.Value);
            }

            if (toMills.HasValue)
            {
                query = query.Where(d => d.Timestamp <= toMills.Value);
            }

            if (medicationId.HasValue)
            {
                query = query.Where(d => d.InjectableMedicationId == medicationId.Value);
            }

            var entities = await query
                .OrderByDescending(d => d.Timestamp)
                .ToListAsync(cancellationToken);

            var doses = entities.Select(InjectableDoseMapper.ToDomainModel).ToList();

            _logger.LogDebug("Retrieved {Count} injectable doses", doses.Count);
            return doses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting injectable doses");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<InjectableDose?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Getting injectable dose by ID: {Id}", id);

            var entity = await _dbContext.InjectableDoses
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogDebug("Injectable dose not found: {Id}", id);
                return null;
            }

            return InjectableDoseMapper.ToDomainModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting injectable dose by ID: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<InjectableDose> CreateAsync(
        InjectableDose dose,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug(
                "Creating injectable dose: {Units} units of medication {MedicationId}",
                dose.Units,
                dose.InjectableMedicationId
            );

            var entity = InjectableDoseMapper.ToEntity(dose);

            _dbContext.InjectableDoses.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var createdDose = InjectableDoseMapper.ToDomainModel(entity);
            _logger.LogInformation(
                "Created injectable dose: {Id} - {Units} units",
                createdDose.Id,
                createdDose.Units
            );

            return createdDose;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating injectable dose for medication: {MedicationId}",
                dose.InjectableMedicationId
            );
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<InjectableDose> UpdateAsync(
        InjectableDose dose,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Updating injectable dose: {Id}", dose.Id);

            var entity = await _dbContext.InjectableDoses
                .FirstOrDefaultAsync(d => d.Id == dose.Id, cancellationToken);

            if (entity == null)
            {
                throw new InvalidOperationException($"Injectable dose not found: {dose.Id}");
            }

            InjectableDoseMapper.UpdateEntity(entity, dose);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var updatedDose = InjectableDoseMapper.ToDomainModel(entity);
            _logger.LogInformation(
                "Updated injectable dose: {Id} - {Units} units",
                updatedDose.Id,
                updatedDose.Units
            );

            return updatedDose;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating injectable dose: {Id}", dose.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting injectable dose: {Id}", id);

            var entity = await _dbContext.InjectableDoses
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Injectable dose not found for deletion: {Id}", id);
                return false;
            }

            _dbContext.InjectableDoses.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted injectable dose: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting injectable dose: {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InjectableDose>> GetRecentRapidActingDosesAsync(
        long beforeMills,
        int hoursBack = 8,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Calculate the start time (beforeMills - hoursBack * 3600000 milliseconds per hour)
            var startMills = beforeMills - (hoursBack * 3600000L);

            _logger.LogDebug(
                "Getting recent rapid-acting doses from {StartMills} to {BeforeMills} ({HoursBack} hours back)",
                startMills,
                beforeMills,
                hoursBack
            );

            // Categories that count as rapid-acting for IOB calculations
            var rapidActingCategories = new[]
            {
                InjectableCategory.RapidActing.ToString(),
                InjectableCategory.UltraRapid.ToString(),
                InjectableCategory.ShortActing.ToString()
            };

            // Query doses with a join to medications to filter by category
            var entities = await _dbContext.InjectableDoses
                .AsNoTracking()
                .Include(d => d.InjectableMedication)
                .Where(d => d.Timestamp >= startMills && d.Timestamp <= beforeMills)
                .Where(d => rapidActingCategories.Contains(d.InjectableMedication.Category))
                .OrderByDescending(d => d.Timestamp)
                .ToListAsync(cancellationToken);

            var doses = entities.Select(InjectableDoseMapper.ToDomainModel).ToList();

            _logger.LogDebug(
                "Retrieved {Count} recent rapid-acting doses for IOB calculation",
                doses.Count
            );
            return doses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent rapid-acting doses");
            throw;
        }
    }
}
