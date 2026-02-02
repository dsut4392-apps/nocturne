# Overnight Compression Low Detection

## Overview

Compression lows occur when a CGM sensor is physically compressed (typically from sleeping on it), causing falsely low glucose readings. These aren't real hypoglycemic events - they're sensor artifacts.

This feature automatically detects overnight compression lows using pattern recognition, presents them to the user for review, and allows marking them as data exclusions so they don't skew statistics.

## User Flow

1. **Detection**: Background service runs after overnight window closes (default 7am)
2. **Notification**: In-app notification shows "X potential compression lows detected last night"
3. **Review**: User navigates to `/reports/compression-lows/review`
4. **Visualization**: Overnight glucose chart with highlighted regions for each suggestion
5. **Adjustment**: User can drag handles to adjust region boundaries
6. **Action**: Accept or dismiss each suggestion
7. **Result**: Accepted suggestions become `DataExclusion` State Spans

## Data Model

### State Span Extension

Add `DataExclusion` to the `StateSpanCategory` enum. When a compression low is accepted, create a State Span with:

```
Category: DataExclusion
State: "Active"
StartMills: <start of compression low region>
EndMills: <end of compression low region>
Metadata: {
  "Type": "CompressionLow",
  "Confidence": 0.85,
  "DetectedAt": <unix ms when algorithm detected it>,
  "AdjustedByUser": true/false
}
```

### CompressionLowSuggestion

A new model to track pending (unreviewed) compression low suggestions:

```
CompressionLowSuggestion:
  - Id (UUID v7)
  - StartMills, EndMills (detected region)
  - Confidence (0-1 score from algorithm)
  - Status: Pending | Accepted | Dismissed
  - NightOf (date, e.g., "2026-02-01" for the night of Feb 1-2)
  - CreatedAt, ReviewedAt
  - StateSpanId (nullable, set when accepted)
```

### CompressionLowSettings

User-configurable detection settings:

```
CompressionLowSettings:
  - OvernightStartHour: 23 (11pm default)
  - OvernightEndHour: 7 (7am default)
  - DetectionEnabled: true
```

## Detection Algorithm

### Service: CompressionLowDetectionService

A background service that runs once daily (15 minutes after configured window end).

### Detection Logic (Classic V-shape)

1. Query entries within the overnight window (default 11pm-7am)
2. Identify candidates where:
   - Drop rate exceeds threshold (e.g., >2 mg/dL/min for 10+ minutes)
   - Reaches a local minimum below a threshold (e.g., <70 mg/dL)
   - Recovers to within 20% of pre-drop value within 60 minutes
   - No bolus or carb treatment in the 2 hours prior that would explain the drop
3. Calculate confidence score based on:
   - How "clean" the V-shape is
   - Absence of IOB that could explain the drop
   - Time of night (2am-5am = higher confidence)
4. Create `CompressionLowSuggestion` records for each candidate above minimum confidence threshold (e.g., 0.5)

## API Design

All endpoints under `/api/v4/compression-lows`:

```
GET  /suggestions
     Query params: ?status=Pending&nightOf=2026-02-01
     Returns: List of CompressionLowSuggestion

GET  /suggestions/{id}
     Returns: Single suggestion with entry data for the time range

POST /suggestions/{id}/accept
     Body: { startMills, endMills } (allows adjusted bounds)
     Creates StateSpan, updates suggestion status to Accepted
     Returns: Created StateSpan

POST /suggestions/{id}/dismiss
     Updates suggestion status to Dismissed
     Returns: 204 No Content

GET  /settings
     Returns: User's CompressionLowSettings

PUT  /settings
     Body: { overnightStartHour, overnightEndHour, detectionEnabled }
     Returns: Updated settings
```

## Frontend

### Routes

```
/reports/compression-lows/         - List view of compression low history
/reports/compression-lows/review   - Review pending suggestions with graph
```

### Remote Functions

Located in `src/Web/packages/app/src/lib/data/compression-lows.remote.ts`:

```typescript
// Queries
getCompressionLowSuggestions({ status?, nightOf? })
getCompressionLowSuggestion(id)
getCompressionLowSettings()

// Commands
acceptCompressionLow(id, { startMills, endMills })
dismissCompressionLow(id)
updateCompressionLowSettings({ overnightStartHour, overnightEndHour, detectionEnabled })
```

### In-App Notification

On the dashboard, show a banner when pending suggestions exist:

```
"2 potential compression lows detected last night"
[Review] button → navigates to /reports/compression-lows/review
```

### Review Page (`/reports/compression-lows/review`)

Layout:
- Header with date (e.g., "Night of Feb 1-2, 2026")
- Full-width glucose chart showing the overnight window
- Suggested compression low regions highlighted with semi-transparent overlay (orange/amber)
- Below the chart: a card for each pending suggestion

Per-Suggestion Card:
- Time range (e.g., "2:34 AM - 3:12 AM")
- Confidence indicator (e.g., "High confidence")
- Mini stats: lowest reading, drop rate, recovery time
- Actions: Accept | Dismiss

Adjustable Bounds:
- Clicking a highlighted region shows drag handles at start/end
- Dragging updates the region bounds in real-time
- Accept button uses the adjusted bounds

### History Page (`/reports/compression-lows/`)

- Table/list of past compression lows (accepted State Spans)
- Columns: Date, Time Range, Duration
- Click to view on calendar or glucose chart for that day

## Statistics Integration

### Exclusion Logic

When calculating statistics (Time in Range, averages, AGP, etc.):

1. Query all `DataExclusion` State Spans for the time range
2. Filter out entries where `entry.Mills` falls within any exclusion span
3. Calculate stats on the remaining entries

### Toggle Behavior

Reports that use glucose statistics will have a toggle:

```
[ ] Include excluded data (compression lows, etc.)
```

- Off by default (exclusions applied)
- When toggled on, show all data including excluded readings
- Persist toggle preference in user settings

### Visual Indicator

On glucose charts throughout the app (dashboard, calendar day view), when a `DataExclusion` State Span exists:

- Show a subtle swim lane or background tint for the excluded region
- Tooltip on hover: "Compression low - excluded from statistics"

## File Structure

### Backend (new files)

```
src/Core/Nocturne.Core.Models/
  CompressionLowSuggestion.cs
  CompressionLowSettings.cs

src/Core/Nocturne.Core.Contracts/
  ICompressionLowService.cs

src/API/Nocturne.API/Controllers/V4/
  CompressionLowController.cs

src/API/Nocturne.API/Services/
  CompressionLowService.cs
  CompressionLowDetectionService.cs  (background service)

src/Infrastructure/Nocturne.Infrastructure.Data/
  Entities/CompressionLowSuggestionEntity.cs
  Repositories/CompressionLowRepository.cs
  Mappers/CompressionLowMapper.cs
```

### Backend (modifications)

```
src/Core/Nocturne.Core.Models/StateSpanEnums.cs
  → Add DataExclusion to StateSpanCategory enum

src/API/Nocturne.API/Program.cs
  → Register new services
```

### Frontend (new files)

```
src/Web/packages/app/src/lib/data/
  compression-lows.remote.ts

src/Web/packages/app/src/lib/components/compression-lows/
  CompressionLowReviewCard.svelte
  CompressionLowChart.svelte
  index.ts

src/Web/packages/app/src/routes/reports/compression-lows/
  +page.svelte              (history)
  +page.server.ts
  review/
    +page.svelte            (review UI)
    +page.server.ts
```
