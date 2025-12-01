//! Output types for determine-basal algorithm

#[cfg(feature = "serde")]
use serde::{Deserialize, Serialize};

/// Result from the determine-basal algorithm
#[derive(Debug, Clone)]
#[cfg_attr(feature = "serde", derive(Serialize, Deserialize))]
#[cfg_attr(feature = "serde", serde(rename_all = "camelCase"))]
pub struct DetermineBasalResult {
    /// Recommended temp basal rate (U/hr)
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub rate: Option<f64>,

    /// Recommended temp basal duration (minutes)
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub duration: Option<u32>,

    /// Reason string explaining the decision
    pub reason: String,

    /// Current COB (grams)
    #[cfg_attr(feature = "serde", serde(default))]
    pub cob: f64,

    /// Current IOB (units)
    #[cfg_attr(feature = "serde", serde(default))]
    pub iob: f64,

    /// Eventual BG prediction (mg/dL)
    #[cfg_attr(feature = "serde", serde(default))]
    pub eventual_bg: f64,

    /// Insulin required to reach target
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub insulin_req: Option<f64>,

    /// SMB amount to deliver (units)
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub units: Option<f64>,

    /// Tick indicator for UI
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub tick: Option<String>,

    /// Error message if calculation failed
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub error: Option<String>,

    /// Delivery time for temp basal
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub deliver_at: Option<String>,

    /// Sensitivity ratio used
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub sensitivity_ratio: Option<f64>,

    /// Variable sensitivity (adjusted ISF)
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub variable_sens: Option<f64>,

    // ============ Prediction Arrays (for visualization) ============
    /// Predicted BG values
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub predicted_bg: Option<Vec<f64>>,

    /// Predicted BG with UAM
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub pred_bgs_uam: Option<Vec<f64>>,

    /// Predicted BG with IOB only
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub pred_bgs_iob: Option<Vec<f64>>,

    /// Predicted BG with zero temp
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub pred_bgs_zt: Option<Vec<f64>>,

    /// Predicted BG with COB
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub pred_bgs_cob: Option<Vec<f64>>,

    // ============ Additional Context ============
    /// Minutes ago of current BG
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub bg_mins_ago: Option<f64>,

    /// Target BG used
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub target_bg: Option<f64>,

    /// Whether SMB is enabled
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub smb_enabled: Option<bool>,

    /// Carbs required
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub carbs_req: Option<f64>,

    /// Threshold BG
    #[cfg_attr(feature = "serde", serde(skip_serializing_if = "Option::is_none"))]
    pub threshold: Option<f64>,
}

impl Default for DetermineBasalResult {
    fn default() -> Self {
        Self {
            rate: None,
            duration: None,
            reason: String::new(),
            cob: 0.0,
            iob: 0.0,
            eventual_bg: 0.0,
            insulin_req: None,
            units: None,
            tick: None,
            error: None,
            deliver_at: None,
            sensitivity_ratio: None,
            variable_sens: None,
            predicted_bg: None,
            pred_bgs_uam: None,
            pred_bgs_iob: None,
            pred_bgs_zt: None,
            pred_bgs_cob: None,
            bg_mins_ago: None,
            target_bg: None,
            smb_enabled: None,
            carbs_req: None,
            threshold: None,
        }
    }
}

impl DetermineBasalResult {
    /// Create an error result
    pub fn error(message: impl Into<String>) -> Self {
        Self {
            error: Some(message.into()),
            ..Default::default()
        }
    }

    /// Create a result with temp basal recommendation
    pub fn temp_basal(rate: f64, duration: u32, reason: impl Into<String>) -> Self {
        Self {
            rate: Some(rate),
            duration: Some(duration),
            reason: reason.into(),
            ..Default::default()
        }
    }

    /// Create a result with SMB recommendation
    pub fn smb(units: f64, rate: f64, duration: u32, reason: impl Into<String>) -> Self {
        Self {
            units: Some(units),
            rate: Some(rate),
            duration: Some(duration),
            reason: reason.into(),
            ..Default::default()
        }
    }

    /// Create a result with no action needed
    pub fn no_action(reason: impl Into<String>) -> Self {
        Self {
            reason: reason.into(),
            ..Default::default()
        }
    }

    /// Check if an SMB is recommended
    pub fn has_smb(&self) -> bool {
        self.units.map_or(false, |u| u > 0.0)
    }

    /// Check if a temp basal change is recommended
    pub fn has_temp(&self) -> bool {
        self.rate.is_some() && self.duration.is_some()
    }

    /// Check if there was an error
    pub fn has_error(&self) -> bool {
        self.error.is_some()
    }
}

/// Temp basal recommendation
#[derive(Debug, Clone)]
#[cfg_attr(feature = "serde", derive(Serialize, Deserialize))]
pub struct TempBasalRecommendation {
    /// Rate (U/hr)
    pub rate: f64,

    /// Duration (minutes)
    pub duration: u32,

    /// Reason for recommendation
    pub reason: String,
}

impl TempBasalRecommendation {
    /// Create a new temp basal recommendation
    pub fn new(rate: f64, duration: u32, reason: impl Into<String>) -> Self {
        Self {
            rate,
            duration,
            reason: reason.into(),
        }
    }

    /// Create a zero temp recommendation
    pub fn zero(duration: u32, reason: impl Into<String>) -> Self {
        Self::new(0.0, duration, reason)
    }
}
