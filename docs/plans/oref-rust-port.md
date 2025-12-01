# oref0 → Rust Port Plan

## Executive Summary

This document outlines the plan to port the OpenAPS Reference Implementation (oref0) from JavaScript/Node.js to Rust. The primary goal is to create a unified, high-performance library that can be shared between:

1. **Trio** (iOS Loop fork) - compiled as a static library / FFI
2. **Nocturne** - compiled as WebAssembly for server-side calculations and optionally as a native library
3. **Potential future clients** - Android apps, embedded systems, etc.

## Current Status: ✅ Phase 1-7+ Complete

**Location:** `src/Core/oref/`

**Build Status:** ✅ Compiles successfully
**Test Status:** ✅ 83/83 tests passing

### Completed Components

| Component         | Status         | Files                                                      |
| ----------------- | -------------- | ---------------------------------------------------------- |
| Core Types        | ✅ Complete    | `src/types/*.rs`                                           |
| Insulin Curves    | ✅ Complete    | `src/insulin/curves.rs`, `src/insulin/calculate.rs`        |
| IOB Calculation   | ✅ Complete    | `src/iob/mod.rs`, `src/iob/history.rs`, `src/iob/total.rs` |
| COB Calculation   | ✅ Complete    | `src/cob/mod.rs`                                           |
| Autosens          | ✅ Complete    | `src/autosens/mod.rs`                                      |
| Profile Lookups   | ✅ Complete    | `src/profile/*.rs`                                         |
| Determine Basal   | ✅ Complete    | `src/determine_basal/*.rs`                                 |
| Meal Detection    | ✅ Complete    | `src/meal/mod.rs`                                          |
| Utilities         | ✅ Complete    | `src/utils/*.rs`                                           |
| **WASM Bindings** | ✅ Complete    | `src/wasm/mod.rs`                                          |
| **FFI Bindings**  | ✅ Complete    | `src/ffi/mod.rs`, `include/oref.h`                         |
| Autotune          | 🔲 Not Started | `src/autotune/`                                            |

### Integration Points

| Target          | Method                            | Status                 |
| --------------- | --------------------------------- | ---------------------- |
| Nocturne (.NET) | WASM via Wasmtime or JS interop   | ✅ Bindings ready      |
| Trio (iOS)      | FFI via static library + C header | ✅ Bindings ready      |
| Web clients     | WASM via wasm-pack                | ✅ Builds successfully |

## Current oref0 Architecture Analysis

### Core Modules (to be ported)

| Module                     | File(s)                                  | Description                                            | Complexity |
| -------------------------- | ---------------------------------------- | ------------------------------------------------------ | ---------- |
| **IOB (Insulin on Board)** | `lib/iob/`                               | Calculates active insulin from boluses and temp basals | High       |
| **COB (Carbs on Board)**   | `lib/determine-basal/cob.js`             | Calculates carb absorption using deviation analysis    | High       |
| **Meal Detection**         | `lib/meal/`                              | Detects meal absorption patterns                       | Medium     |
| **Autosens**               | `lib/determine-basal/autosens.js`        | Detects insulin sensitivity changes                    | High       |
| **Determine Basal**        | `lib/determine-basal/determine-basal.js` | Main dosing algorithm (~1600 lines)                    | Very High  |
| **Profile**                | `lib/profile/`                           | Handles basal, ISF, carb ratio schedules               | Medium     |
| **Autotune**               | `lib/autotune/`                          | Tunes basals, ISF, and carb ratios                     | High       |

### Key Algorithms to Port

1. **Insulin Curves**

   - Bilinear model (legacy)
   - Exponential model (rapid-acting: peak 75min, ultra-rapid: peak 55min)
   - Custom peak time support

2. **IOB Calculation**

   - Treatment processing from pump history
   - Basal IOB vs bolus IOB separation
   - Future IOB prediction with zero-temp scenarios

3. **COB Calculation**

   - Glucose deviation analysis
   - Carb absorption rate estimation
   - min_5m_carbimpact for minimum absorption

4. **Autosens Detection**

   - 24-hour deviation analysis
   - Meal and UAM (Unannounced Meal) exclusion
   - Sensitivity ratio calculation (bounded by autosens_min/max)

5. **Dynamic ISF/ICR**

   - Logarithmic formula
   - Sigmoid formula (newer)
   - TDD-based adjustments

6. **Determine Basal**
   - SMB (Super Micro Bolus) calculations
   - Temp basal recommendations
   - Safety limits and constraints

## Rust Library Architecture

```
oref-rs/
├── Cargo.toml
├── src/
│   ├── lib.rs                    # Library root, feature flags
│   ├── types/
│   │   ├── mod.rs
│   │   ├── profile.rs            # Profile, BasalSchedule, ISFSchedule
│   │   ├── treatment.rs          # Treatment, TempBasal, Bolus
│   │   ├── glucose.rs            # GlucoseReading, GlucoseStatus
│   │   ├── iob.rs               # IOBData, IOBContrib
│   │   ├── cob.rs               # COBData, MealData
│   │   └── output.rs            # DetermineBasalResult, TempBasalRecommendation
│   │
│   ├── insulin/
│   │   ├── mod.rs
│   │   ├── curves.rs            # InsulinCurve trait, Bilinear, Exponential
│   │   └── activity.rs          # Activity and IOB contribution calculations
│   │
│   ├── iob/
│   │   ├── mod.rs
│   │   ├── calculate.rs         # Single treatment IOB calculation
│   │   ├── history.rs           # Treatment history processing
│   │   └── total.rs             # Total IOB summation
│   │
│   ├── cob/
│   │   ├── mod.rs
│   │   ├── absorption.rs        # Carb absorption calculation
│   │   └── deviation.rs         # Glucose deviation analysis
│   │
│   ├── meal/
│   │   ├── mod.rs
│   │   ├── history.rs           # Meal history processing
│   │   └── total.rs             # Meal COB calculation
│   │
│   ├── autosens/
│   │   ├── mod.rs
│   │   └── detect.rs            # Sensitivity detection algorithm
│   │
│   ├── determine_basal/
│   │   ├── mod.rs
│   │   ├── algorithm.rs         # Main determine basal logic
│   │   ├── dynamic_isf.rs       # Dynamic ISF calculations
│   │   ├── smb.rs               # SMB enable/calculation logic
│   │   └── predictions.rs       # BG prediction calculations
│   │
│   ├── profile/
│   │   ├── mod.rs
│   │   ├── basal.rs             # Basal rate lookups
│   │   ├── isf.rs               # ISF schedule lookups
│   │   ├── carbs.rs             # Carb ratio lookups
│   │   └── targets.rs           # BG target lookups
│   │
│   ├── autotune/
│   │   ├── mod.rs
│   │   └── tune.rs              # Autotune algorithm
│   │
│   ├── utils/
│   │   ├── mod.rs
│   │   ├── time.rs              # Timezone-aware time utilities
│   │   ├── round.rs             # Rounding utilities (pump-specific)
│   │   └── percentile.rs        # Statistical percentile calculations
│   │
│   └── ffi/
│       ├── mod.rs               # C FFI bindings
│       ├── swift.rs             # Swift-specific bindings for Trio
│       └── wasm.rs              # WebAssembly bindings for Nocturne
│
├── examples/
│   ├── basic_iob.rs
│   ├── determine_basal.rs
│   └── autotune.rs
│
└── tests/
    ├── iob_tests.rs             # Ported from tests/iob.test.js
    ├── cob_tests.rs
    ├── determine_basal_tests.rs # Ported from tests/determine-basal.test.js
    └── fixtures/                # Test data files
        ├── profiles/
        ├── glucose_data/
        └── pump_history/
```

## Type Definitions

### Core Types (Rust)

```rust
// src/types/profile.rs
use chrono::{DateTime, Utc, NaiveTime};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Profile {
    pub dia: f64,                           // Duration of insulin action (hours)
    pub current_basal: f64,                 // Current scheduled basal rate
    pub max_iob: f64,
    pub max_daily_basal: f64,
    pub max_basal: f64,
    pub min_bg: f64,
    pub max_bg: f64,
    pub sens: f64,                          // Insulin sensitivity factor (mg/dL per U)
    pub carb_ratio: f64,                    // Carb ratio (g per U)
    pub curve: InsulinCurve,
    pub use_custom_peak_time: bool,
    pub insulin_peak_time: u32,             // minutes
    pub autosens_min: f64,
    pub autosens_max: f64,
    pub min_5m_carbimpact: f64,
    pub max_cob: f64,
    pub max_meal_absorption_time: f64,      // hours

    // SMB settings
    pub enable_smb_always: bool,
    pub enable_smb_with_cob: bool,
    pub enable_smb_with_temptarget: bool,
    pub enable_smb_after_carbs: bool,
    pub enable_smb_high_bg: bool,
    pub enable_smb_high_bg_target: f64,
    pub allow_smb_with_high_temptarget: bool,
    pub max_smb_basal_minutes: u32,
    pub max_uam_smb_basal_minutes: u32,
    pub smb_interval: u32,                  // minutes
    pub bolus_increment: f64,

    // UAM
    pub enable_uam: bool,

    // Dynamic settings
    pub use_dynamic_isf: bool,
    pub use_sigmoid: bool,
    pub adjustment_factor: f64,
    pub adjustment_factor_sigmoid: f64,
    pub weight_percentage: f64,
    pub tdd_adj_basal: bool,

    // Schedules
    pub basal_profile: Vec<BasalScheduleEntry>,
    pub isf_profile: ISFProfile,
}

#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq)]
pub enum InsulinCurve {
    Bilinear,
    RapidActing,
    UltraRapid,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BasalScheduleEntry {
    pub i: u32,
    pub start: NaiveTime,
    pub rate: f64,
    pub minutes: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ISFProfile {
    pub sensitivities: Vec<ISFEntry>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ISFEntry {
    pub offset: u32,        // minutes from midnight
    pub sensitivity: f64,   // mg/dL per U
}

// src/types/treatment.rs
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Treatment {
    pub timestamp: DateTime<Utc>,
    pub date: i64,              // Unix millis
    pub insulin: Option<f64>,
    pub carbs: Option<f64>,
    pub rate: Option<f64>,      // For temp basals
    pub duration: Option<f64>,  // minutes
}

// src/types/glucose.rs
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GlucoseReading {
    pub glucose: f64,           // mg/dL
    pub date: i64,              // Unix millis
    pub date_string: Option<String>,
    pub noise: Option<f64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GlucoseStatus {
    pub glucose: f64,
    pub delta: f64,
    pub short_avgdelta: f64,
    pub long_avgdelta: f64,
    pub date: i64,
    pub noise: Option<f64>,
}

// src/types/iob.rs
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct IOBData {
    pub iob: f64,
    pub activity: f64,
    pub basal_iob: f64,
    pub bolus_iob: f64,
    pub net_basal_insulin: f64,
    pub bolus_insulin: f64,
    pub time: DateTime<Utc>,
    pub iob_with_zero_temp: Option<Box<IOBData>>,
    pub last_bolus_time: Option<i64>,
    pub last_temp: Option<TempBasalState>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TempBasalState {
    pub date: i64,
    pub duration: f64,
    pub rate: Option<f64>,
}

// src/types/cob.rs
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MealData {
    pub carbs: f64,
    pub ns_carbs: f64,
    pub bw_carbs: f64,
    pub journal_carbs: f64,
    pub meal_cob: f64,
    pub current_deviation: f64,
    pub max_deviation: f64,
    pub min_deviation: f64,
    pub slope_from_max_deviation: f64,
    pub slope_from_min_deviation: f64,
    pub all_deviations: Vec<f64>,
    pub last_carb_time: i64,
    pub bw_found: bool,
}

// src/types/output.rs
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DetermineBasalResult {
    pub rate: Option<f64>,
    pub duration: Option<u32>,
    pub reason: String,
    pub cob: f64,
    pub iob: f64,
    pub eventual_bg: f64,
    pub insulin_req: Option<f64>,
    pub units: Option<f64>,         // SMB amount
    pub tick: Option<String>,
    pub error: Option<String>,

    // Prediction arrays (optional, for visualization)
    pub predicted_bg: Option<Vec<f64>>,
    pub predicted_uam: Option<Vec<f64>>,
    pub predicted_iob: Option<Vec<f64>>,
    pub predicted_zt: Option<Vec<f64>>,
    pub predicted_cob: Option<Vec<f64>>,
}
```

## Implementation Phases

### Phase 1: Core Types & Insulin Curves ✅ COMPLETE

- [x] Set up Cargo workspace
- [x] Define all core types with serde serialization
- [x] Implement insulin curve calculations (bilinear, exponential)
- [x] Unit tests for insulin curves matching JS behavior

### Phase 2: IOB Calculation ✅ COMPLETE

- [x] Port `lib/iob/calculate.js` - single treatment IOB
- [x] Port `lib/iob/history.js` - treatment history processing
- [x] Port `lib/iob/total.js` - total IOB summation
- [x] Port `lib/iob/index.js` - main IOB generation
- [x] Comprehensive tests matching `tests/iob.test.js`

### Phase 3: Profile & Utilities ✅ COMPLETE

- [x] Port `lib/profile/basal.js`
- [x] Port `lib/profile/isf.js`
- [x] Port `lib/profile/carbs.js`
- [x] Port `lib/profile/targets.js`
- [x] Port `lib/round-basal.js`
- [x] Time/timezone utilities (replace moment-timezone)

### Phase 4: COB & Meal Detection ✅ COMPLETE

- [x] Port `lib/determine-basal/cob.js`
- [x] Port `lib/meal/history.js`
- [x] Port `lib/meal/total.js`
- [x] Tests for COB calculations

### Phase 5: Autosens ✅ COMPLETE

- [x] Port `lib/determine-basal/autosens.js`
- [x] Sensitivity detection algorithm
- [x] Meal/UAM exclusion logic

### Phase 6: Determine Basal ✅ COMPLETE

- [x] Port core determine basal logic
- [x] Dynamic ISF (logarithmic and sigmoid)
- [x] SMB calculations
- [x] BG prediction calculations
- [x] All safety limits
- [x] Tests matching `tests/determine-basal.test.js`

### Phase 7: Autotune 🔲 NOT STARTED

- [ ] Port `lib/autotune/index.js`
- [ ] Basal tuning
- [ ] ISF tuning
- [ ] CSF (Carb Sensitivity Factor) tuning

### Phase 8: FFI & WASM Bindings 🔲 NOT STARTED

- [ ] C FFI bindings with cbindgen
- [ ] Swift wrapper generation for Trio
- [ ] WASM bindings with wasm-bindgen
- [ ] TypeScript type definitions for Nocturne

### Phase 9: Integration & Testing 🔲 NOT STARTED

- [ ] End-to-end tests with real data scenarios
- [ ] Performance benchmarks
- [ ] Documentation
- [ ] Integration examples for Trio and Nocturne

## Key Technical Decisions

### 1. Time Handling

- Use `chrono` crate for all datetime operations
- Store timestamps as Unix milliseconds (i64) for compatibility
- Handle timezone conversions explicitly

### 2. Floating Point Precision

- Use f64 for all calculations
- Round outputs to match pump precision (0.025, 0.05, 0.1 depending on pump model)
- Use epsilon comparisons for floating point equality

### 3. Error Handling

- Use `thiserror` for error types
- Return `Result<T, OrefError>` for fallible operations
- Provide detailed error messages for debugging

### 4. Serialization

- Use `serde` with JSON for data exchange
- Support both camelCase (JS) and snake_case (Rust) field names
- Preserve compatibility with existing Nightscout/OpenAPS data formats

### 5. Feature Flags

```toml
[features]
default = ["std"]
std = []
ffi = ["std"]
wasm = ["wasm-bindgen", "js-sys"]
serde = ["dep:serde"]
```

### 6. Build Targets

- `aarch64-apple-ios` - Trio on iOS
- `x86_64-apple-darwin` - macOS development
- `wasm32-unknown-unknown` - Nocturne WebAssembly
- `x86_64-unknown-linux-gnu` - Server-side

## Testing Strategy

1. **Unit Tests**: Direct ports of existing JS tests
2. **Property-Based Tests**: Using `proptest` for edge cases
3. **Golden Tests**: Compare output with JS implementation
4. **Fuzzing**: Use `cargo-fuzz` for security-critical paths

## Nocturne Integration

```rust
// Example WASM usage in Nocturne
use wasm_bindgen::prelude::*;
use oref_rs::{Profile, GlucoseStatus, IOBData, MealData};

#[wasm_bindgen]
pub fn calculate_iob(
    profile_json: &str,
    history_json: &str,
    clock: &str,
) -> Result<JsValue, JsValue> {
    let profile: Profile = serde_json::from_str(profile_json)?;
    let history: Vec<Treatment> = serde_json::from_str(history_json)?;
    let clock = DateTime::parse_from_rfc3339(clock)?;

    let iob = oref_rs::iob::generate(&profile, &history, clock)?;
    Ok(serde_wasm_bindgen::to_value(&iob)?)
}

#[wasm_bindgen]
pub fn determine_basal(
    glucose_status_json: &str,
    current_temp_json: &str,
    iob_data_json: &str,
    profile_json: &str,
    autosens_json: &str,
    meal_data_json: &str,
) -> Result<JsValue, JsValue> {
    // Parse inputs and run algorithm
    // Return JSON result
}
```

## Trio Integration (Swift)

```swift
// Example Swift FFI usage in Trio
import OrefRs

let profile = OrefProfile(/* ... */)
let glucoseStatus = OrefGlucoseStatus(/* ... */)

let result = try oref_determine_basal(
    glucoseStatus: glucoseStatus,
    currentTemp: currentTemp,
    iobData: iobData,
    profile: profile,
    autosensData: autosensData,
    mealData: mealData
)

if let smbUnits = result.units {
    // Enact SMB
}
```

## Questions for Clarification

1. **Trio Compatibility**: Should we maintain exact behavioral compatibility with the current Trio implementation, or is there room for improvements/fixes?

2. **Dynamic ISF Variants**: The codebase shows multiple dynamic ISF formulas (Chris Wilson's original, logarithmic, sigmoid). Which variants should be included?

3. **Autotune**: Is Autotune required for the initial release, or can it be deferred?

4. **Testing Data**: Do you have access to real-world test datasets for validation?

5. **Performance Requirements**: Are there specific latency requirements for the calculations?

6. **Configuration Format**: Should the Rust library accept the same JSON format as oref0, or design a new schema?

## Dependencies (Cargo.toml)

```toml
[package]
name = "oref-rs"
version = "0.1.0"
edition = "2021"

[lib]
crate-type = ["cdylib", "rlib", "staticlib"]

[dependencies]
chrono = { version = "0.4", features = ["serde"] }
serde = { version = "1.0", features = ["derive"], optional = true }
serde_json = { version = "1.0", optional = true }
thiserror = "1.0"

# FFI
cbindgen = { version = "0.26", optional = true }

# WASM
wasm-bindgen = { version = "0.2", optional = true }
js-sys = { version = "0.3", optional = true }
serde-wasm-bindgen = { version = "0.6", optional = true }

[dev-dependencies]
proptest = "1.0"
criterion = "0.5"
approx = "0.5"

[[bench]]
name = "iob_bench"
harness = false
```

## Next Steps

1. Review and approve this plan
2. Set up the Rust workspace in `src/Core/Nocturne.Core.Oref/` or separate repo
3. Begin Phase 1 implementation
4. Establish CI/CD for cross-compilation

## Phase 8: .NET Integration (Nocturne)

### Status: ✅ Complete

The Nocturne .NET backend integrates with the oref Rust library via WebAssembly using the Wasmtime runtime.

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Nocturne.API                               │
├─────────────────────────────────────────────────────────────────┤
│  IOrefService (Interface)                                       │
│  ├── CalculateIobAsync()                                        │
│  ├── CalculateCobAsync()                                        │
│  ├── CalculateAutosensAsync()                                   │
│  ├── DetermineBasalAsync()                                      │
│  └── CalculateGlucoseStatusAsync()                              │
├─────────────────────────────────────────────────────────────────┤
│  OrefWasmService (Implementation)                               │
│  └── Uses Wasmtime to execute oref.wasm                         │
├─────────────────────────────────────────────────────────────────┤
│  Wasmtime Runtime (NuGet: Wasmtime 34.0.2)                      │
│  └── Loads oref.wasm and manages WASM memory                    │
├─────────────────────────────────────────────────────────────────┤
│  oref.wasm (Compiled from src/Core/oref/)                       │
│  └── cargo build --target wasm32-unknown-unknown --features wasm│
└─────────────────────────────────────────────────────────────────┘
```

### Key Files

| File                                                     | Purpose                      |
| -------------------------------------------------------- | ---------------------------- |
| `src/Core/Nocturne.Core.Contracts/IOrefService.cs`       | Service interface            |
| `src/Core/Nocturne.Core.Contracts/OrefModels.cs`         | C# model types matching Rust |
| `src/API/Nocturne.API/Services/OrefWasmService.cs`       | WASM integration service     |
| `src/API/Nocturne.API/Services/OrefServiceExtensions.cs` | DI registration              |

### C# Model Types

The following C# types map to their Rust counterparts:

| C# Type                    | Rust Type              |
| -------------------------- | ---------------------- |
| `OrefProfile`              | `Profile`              |
| `OrefTreatment`            | `Treatment`            |
| `OrefGlucoseReading`       | `GlucoseReading`       |
| `OrefGlucoseStatus`        | `GlucoseStatus`        |
| `OrefCurrentTemp`          | `CurrentTemp`          |
| `OrefIobResult`            | `IOBData`              |
| `OrefCobResult`            | `COBResult`            |
| `OrefAutosensResult`       | `AutosensData`         |
| `OrefDetermineBasalInputs` | `DetermineBasalInputs` |
| `OrefDetermineBasalResult` | `DetermineBasalResult` |

### Usage in Nocturne

```csharp
// Inject the service
public class MyController(IOrefService orefService) : ControllerBase
{
    public async Task<IActionResult> GetIob()
    {
        var profile = new OrefProfile { Dia = 5.0, Sens = 45 };
        var treatments = new List<OrefTreatment>
        {
            OrefTreatment.Bolus(2.5, DateTimeOffset.UtcNow.AddHours(-1)),
            OrefTreatment.TempBasal(1.5, 30, DateTimeOffset.UtcNow.AddMinutes(-45))
        };

        var iob = await orefService.CalculateIobAsync(profile, treatments);
        return Ok(iob);
    }
}
```

### Service Registration

```csharp
// Program.cs
builder.Services.AddOrefService(options =>
{
    options.WasmPath = "oref.wasm";
    options.Enabled = true;
});
```

### Building the WASM File

```bash
cd src/Core/oref
cargo build --target wasm32-unknown-unknown --features wasm --release
# Output: target/wasm32-unknown-unknown/release/oref.wasm
```

### Legacy Compatibility

The existing `IIobService` and `ICobService` remain available for backward compatibility.
New code should prefer `IOrefService` for:

- Better performance (Rust)
- Full algorithm support (autosens, determine-basal, SMB)
- Unified interface

## Demo Data Generator Integration

### Status: ✅ Complete

The demo data generator has been refactored to use oref pharmacokinetic models for realistic glucose simulation.

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                 DemoDataGenerator                               │
├─────────────────────────────────────────────────────────────────┤
│  Uses OrefPhysiologySimulator for each simulated day            │
│  ├── Creates OrefProfile from scenario parameters               │
│  ├── Tracks insulin doses (bolus, temp basal)                   │
│  ├── Tracks carb entries with absorption times                  │
│  └── Calculates glucose effects using oref insulin curves       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│               OrefPhysiologySimulator                           │
├─────────────────────────────────────────────────────────────────┤
│  CalculateIob()         - Exponential insulin model from oref   │
│  CalculateInsulinActivity() - Glucose-lowering effect           │
│  CalculateCob()         - Linear carb absorption decay          │
│  CalculateCarbAbsorptionRate() - Gamma-curve carb effect        │
│  SimulateNextGlucose()  - Combined 5-minute glucose prediction  │
└─────────────────────────────────────────────────────────────────┘
```

### Key Files

| File                                                                      | Purpose                              |
| ------------------------------------------------------------------------- | ------------------------------------ |
| `src/Services/Nocturne.Services.Demo/Services/OrefPhysiologySimulator.cs` | Glucose simulation using oref curves |
| `src/Services/Nocturne.Services.Demo/Services/DemoDataGenerator.cs`       | Demo data generation (refactored)    |

### Insulin Curve Algorithm

The simulator uses the oref exponential insulin activity model:

```csharp
// Exponential IOB model from oref0
// τ = peak × (1 - peak/DIA) / (1 - 2×peak/DIA)
// a = 2τ / DIA
// S = 1 / (1 - a + (1 + a) × e^(-DIA/τ))
// IOB(t) = 1 - S × (1 - a) × ((t²/(τ×DIA×(1-a)) - t/τ - 1) × e^(-t/τ) + 1)
```

This produces more realistic insulin activity curves than simple linear decay:

- **Peak Time:** Configurable (default 75 min for rapid-acting)
- **Duration:** Configurable DIA (default 5 hours)
- **Activity Curve:** Bell-shaped with gradual onset and decay

### Scenario Effects

The simulator also handles scenario-specific physiological effects:

| Scenario        | Effect                                                              |
| --------------- | ------------------------------------------------------------------- |
| Normal          | Baseline simulation with standard variability                       |
| Dawn Phenomenon | Increased glucose 4-8 AM (sinusoidal curve)                         |
| Exercise        | Glucose drops during/after exercise, enhanced sensitivity overnight |
| Sick Day        | Increased insulin resistance, trending higher                       |
| Stress Day      | Random cortisol-driven glucose spikes                               |

### Usage

The demo generator automatically uses oref physics when generating data:

```csharp
var (entries, treatments) = demoGenerator.GenerateHistoricalData();
// Each day simulates:
// - Meal boluses with realistic carb absorption
// - Temp basals and basal coverage
// - Insulin stacking effects
// - Dawn phenomenon
// - Exercise effects
// - Random variability
```

---

_Last Updated: December 2025_
_Author: Nocturne Development Team_
