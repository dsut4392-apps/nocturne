# MyFitnessPal Meal Matching Design

## Overview

Implement bidirectional matching between MyFitnessPal food entries and Nocturne treatments, with in-app notifications for suggested matches.

## Matching Algorithm

### Trigger Points
- **On MFP sync**: When new food entries are imported
- **On treatment creation**: When a treatment with carbs is created/updated

### Match Criteria (from existing settings)
- **Time window**: `MatchTimeWindowMinutes` (default 30 min) - MFP `consumedAt` vs treatment `Mills`
- **Carb tolerance**: Entry passes if within `MatchCarbTolerancePercent` (20%) OR `MatchCarbToleranceGrams` (10g)

### Scoring Formula
```
score = (1 - timeDiffMinutes/maxMinutes) * 0.6 + (1 - carbDiffRatio) * 0.4
```

The entry with the highest score wins. Ties broken by most recent timestamp.

### Match Candidates
- MFP entries with status `Pending`
- Treatments with `EventType` containing "Meal" or having `Carbs > 0`

## Notification Structure

**Type:** `SuggestedMealMatch`

**Title:** `"{FoodName}" may match your meal bolus`

**Subtitle:** `"{MealName}" · {Carbs}g carbs · {TimeAgo}`

### Actions

| ActionId | Label | Variant | Behavior |
|----------|-------|---------|----------|
| `accept` | Accept | default | Link MFP entry to treatment, archive notification |
| `edit` | Edit | outline | Open dialog to adjust carbs before linking |
| `dismiss` | Dismiss | outline | Mark MFP entry as `Standalone`, archive notification |

### Resolution Conditions
- `ExpiresAt`: Current time + `UnmatchedTimeoutHours` (default 12h)
- When expired, apply `UnmatchedBehavior` setting (Prompt/AutoStandalone/AutoDelete)

### Source Tracking
- `SourceId`: The `ConnectorFoodEntry.Id` (GUID)
- Enables finding/archiving the notification when the MFP entry is deleted or manually resolved

## Service Architecture

### New Service: `MealMatchingService`

**Location:** `src/API/Nocturne.API/Services/MealMatchingService.cs`

**Interface:**
```csharp
public interface IMealMatchingService
{
    // Called after MFP sync imports new entries
    Task ProcessNewFoodEntriesAsync(IEnumerable<Guid> foodEntryIds, CancellationToken ct);

    // Called after a treatment with carbs is created/updated
    Task ProcessNewTreatmentAsync(Guid treatmentId, CancellationToken ct);

    // Execute match actions from notification
    Task AcceptMatchAsync(Guid foodEntryId, Guid treatmentId, decimal? adjustedCarbs, CancellationToken ct);
    Task DismissMatchAsync(Guid foodEntryId, CancellationToken ct);
}
```

### Integration Points

1. **ConnectorFoodEntryService.ImportAsync** → After saving, call `ProcessNewFoodEntriesAsync` for new entries
2. **TreatmentService** → After creating/updating treatments with carbs, call `ProcessNewTreatmentAsync`
3. **NotificationsController** → Route `accept`/`edit`/`dismiss` actions to `MealMatchingService`

### Dependencies
- `IInAppNotificationService` - Create/archive notifications
- `ITreatmentRepository` - Query treatments for matching
- `IConnectorFoodEntryRepository` (new) - Query/update food entries
- `ISettingsService` - Get `MyFitnessPalMatchingSettings`

## Repository & Data Access

### New Repository: `IConnectorFoodEntryRepository`

**Locations:**
- `src/Core/Nocturne.Core.Contracts/IConnectorFoodEntryRepository.cs`
- `src/Infrastructure/Nocturne.Infrastructure.Data/Repositories/ConnectorFoodEntryRepository.cs`

**Methods:**
```csharp
public interface IConnectorFoodEntryRepository
{
    Task<ConnectorFoodEntry?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ConnectorFoodEntry>> GetPendingInTimeRangeAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task UpdateStatusAsync(Guid id, ConnectorFoodEntryStatus status,
        Guid? matchedTreatmentId, CancellationToken ct);
}
```

### Treatment Repository Addition

Add to existing `ITreatmentRepository`:
```csharp
Task<IReadOnlyList<Treatment>> GetMealTreatmentsInTimeRangeAsync(
    DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
```

Queries treatments where:
- `Mills` falls within the time range
- `Carbs > 0` OR `EventType` contains "Meal"

## Action Handling & Frontend

### Backend Action Routing

In `InAppNotificationService.ExecuteActionAsync`, route `SuggestedMealMatch` actions:

```csharp
case InAppNotificationType.SuggestedMealMatch:
    var foodEntryId = notification.SourceId;
    switch (actionId)
    {
        case "accept":
            await _mealMatchingService.AcceptMatchAsync(foodEntryId, treatmentId, null, ct);
            break;
        case "dismiss":
            await _mealMatchingService.DismissMatchAsync(foodEntryId, ct);
            break;
        case "edit":
            return new ActionResult { OpenDialog = "meal-match-edit", Data = { foodEntryId } };
    }
```

### Frontend Edit Dialog

**Location:** `src/Web/packages/app/src/lib/components/notifications/MealMatchEditDialog.svelte`

Features:
- Shows MFP entry details (food name, original carbs, meal name)
- Shows matched treatment details (time, current carbs)
- Input field to adjust carbs
- Confirm button calls `acceptMatch` with adjusted carbs

### New Remote Functions

**Location:** `src/Web/packages/app/src/lib/data/meal-matching.remote.ts`

```typescript
export const acceptMealMatch = command(({ foodEntryId, treatmentId, adjustedCarbs }) => ...);
export const getPendingFoodEntry = query((id) => ...);
```

## Implementation Plan

### Files to Create

| File | Purpose |
|------|---------|
| `src/Core/Nocturne.Core.Contracts/IMealMatchingService.cs` | Service interface |
| `src/Core/Nocturne.Core.Contracts/IConnectorFoodEntryRepository.cs` | Repository interface |
| `src/Infrastructure/Nocturne.Infrastructure.Data/Repositories/ConnectorFoodEntryRepository.cs` | Repository implementation |
| `src/API/Nocturne.API/Services/MealMatchingService.cs` | Matching logic & notification creation |
| `src/Web/packages/app/src/lib/components/notifications/MealMatchEditDialog.svelte` | Edit carbs dialog |
| `src/Web/packages/app/src/lib/data/meal-matching.remote.ts` | Frontend remote functions |

### Files to Modify

| File | Change |
|------|--------|
| `ConnectorFoodEntryService.cs` | Call matching service after import |
| `TreatmentService.cs` | Call matching service after carb treatments |
| `InAppNotificationService.cs` | Route SuggestedMealMatch actions |
| `NotificationResolutionService.cs` | Handle expired match notifications per UnmatchedBehavior |
| `Program.cs` | Register new services |
| `ITreatmentRepository.cs` | Add `GetMealTreatmentsInTimeRangeAsync` |
| `TreatmentRepository.cs` | Implement the new method |
| `NotificationItem.svelte` | Handle edit action opening dialog |

## Out of Scope (YAGNI)

- Batch matching UI for reviewing multiple suggestions at once
- Historical re-matching of already-resolved entries
- Custom match scoring weights in settings
