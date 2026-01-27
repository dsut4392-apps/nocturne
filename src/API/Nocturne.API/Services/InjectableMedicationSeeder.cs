using Nocturne.Core.Contracts;
using Nocturne.Core.Models.Injectables;

namespace Nocturne.API.Services;

/// <summary>
/// Background service that seeds the default injectable medication catalog on startup.
/// Only seeds if the catalog is empty (no medications exist).
/// </summary>
public class InjectableMedicationSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InjectableMedicationSeeder> _logger;

    public InjectableMedicationSeeder(
        IServiceProvider serviceProvider,
        ILogger<InjectableMedicationSeeder> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SeedDefaultMedicationsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default injectable medications");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Seeds the pre-populated medication catalog if the catalog is empty.
    /// </summary>
    public async Task SeedDefaultMedicationsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var medicationService = scope.ServiceProvider.GetRequiredService<IInjectableMedicationService>();

        // Check if any medications already exist (include archived to prevent re-seeding)
        var existing = await medicationService.GetAllAsync(
            includeArchived: true,
            cancellationToken: cancellationToken
        );

        if (existing.Any())
        {
            _logger.LogDebug(
                "Injectable medication catalog already has {Count} entries, skipping seed",
                existing.Count()
            );
            return;
        }

        _logger.LogInformation("Seeding default injectable medication catalog");

        var sortOrder = 0;
        var medications = GetDefaultMedications(ref sortOrder);

        foreach (var medication in medications)
        {
            await medicationService.CreateAsync(medication, cancellationToken);
        }

        _logger.LogInformation(
            "Seeded {Count} default injectable medications",
            medications.Count
        );
    }

    /// <summary>
    /// Returns the pre-populated catalog of injectable medications.
    /// </summary>
    private static List<InjectableMedication> GetDefaultMedications(ref int sortOrder)
    {
        var medications = new List<InjectableMedication>();

        // === Rapid-Acting ===
        medications.Add(CreateMedication(
            "Humalog (Lispro)", InjectableCategory.RapidActing, UnitType.Units,
            concentration: 100, dia: 4.0, onset: 15, peak: 75, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Humalog U200", InjectableCategory.RapidActing, UnitType.Units,
            concentration: 200, dia: 4.0, onset: 15, peak: 75, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Novolog (Aspart)", InjectableCategory.RapidActing, UnitType.Units,
            concentration: 100, dia: 4.0, onset: 15, peak: 75, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Novorapid (Aspart)", InjectableCategory.RapidActing, UnitType.Units,
            concentration: 100, dia: 4.0, onset: 15, peak: 75, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Apidra (Glulisine)", InjectableCategory.RapidActing, UnitType.Units,
            concentration: 100, dia: 4.0, onset: 15, peak: 75, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Fiasp", InjectableCategory.RapidActing, UnitType.Units,
            concentration: 100, dia: 3.5, onset: 5, peak: 60, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Lyumjev", InjectableCategory.UltraRapid, UnitType.Units,
            concentration: 100, dia: 3.5, onset: 5, peak: 60, sortOrder: sortOrder++
        ));

        // === Short-Acting ===
        medications.Add(CreateMedication(
            "Regular (R)", InjectableCategory.ShortActing, UnitType.Units,
            concentration: 100, dia: 6.0, onset: 30, peak: 150, sortOrder: sortOrder++
        ));

        // === Intermediate ===
        medications.Add(CreateMedication(
            "NPH", InjectableCategory.Intermediate, UnitType.Units,
            concentration: 100, dia: 14.0, onset: 90, peak: 480, sortOrder: sortOrder++
        ));

        // === Long-Acting ===
        medications.Add(CreateMedication(
            "Lantus (Glargine)", InjectableCategory.LongActing, UnitType.Units,
            concentration: 100, duration: 24, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Toujeo (Glargine)", InjectableCategory.LongActing, UnitType.Units,
            concentration: 300, duration: 24, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Basaglar (Glargine)", InjectableCategory.LongActing, UnitType.Units,
            concentration: 100, duration: 24, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Levemir (Detemir)", InjectableCategory.LongActing, UnitType.Units,
            concentration: 100, duration: 20, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Tresiba (Degludec)", InjectableCategory.UltraLong, UnitType.Units,
            concentration: 100, duration: 42, sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Tresiba U200", InjectableCategory.UltraLong, UnitType.Units,
            concentration: 200, duration: 42, sortOrder: sortOrder++
        ));

        // === GLP-1 ===
        medications.Add(CreateMedication(
            "Ozempic (Semaglutide)", InjectableCategory.GLP1Weekly, UnitType.Milligrams,
            sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Mounjaro (Tirzepatide)", InjectableCategory.GLP1Weekly, UnitType.Milligrams,
            sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Trulicity (Dulaglutide)", InjectableCategory.GLP1Weekly, UnitType.Milligrams,
            sortOrder: sortOrder++
        ));
        medications.Add(CreateMedication(
            "Victoza (Liraglutide)", InjectableCategory.GLP1Daily, UnitType.Milligrams,
            sortOrder: sortOrder++
        ));

        return medications;
    }

    /// <summary>
    /// Helper to create a medication with the given properties.
    /// </summary>
    private static InjectableMedication CreateMedication(
        string name,
        InjectableCategory category,
        UnitType unitType,
        int concentration = 100,
        double? dia = null,
        double? onset = null,
        double? peak = null,
        double? duration = null,
        int sortOrder = 0
    )
    {
        return new InjectableMedication
        {
            Name = name,
            Category = category,
            UnitType = unitType,
            Concentration = concentration,
            Dia = dia,
            Onset = onset,
            Peak = peak,
            Duration = duration,
            SortOrder = sortOrder,
            IsArchived = false,
        };
    }
}
