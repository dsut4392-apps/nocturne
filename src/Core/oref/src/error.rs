//! Error types for the oref library

use thiserror::Error;

/// Main error type for oref operations
#[derive(Error, Debug)]
pub enum OrefError {
    /// Invalid profile configuration
    #[error("Invalid profile: {0}")]
    InvalidProfile(String),

    /// Invalid treatment data
    #[error("Invalid treatment: {0}")]
    InvalidTreatment(String),

    /// Invalid glucose data
    #[error("Invalid glucose data: {0}")]
    InvalidGlucose(String),

    /// Calculation error
    #[error("Calculation error: {0}")]
    CalculationError(String),

    /// Missing required data
    #[error("Missing required data: {0}")]
    MissingData(String),

    /// Invalid timestamp
    #[error("Invalid timestamp: {0}")]
    InvalidTimestamp(String),

    /// Out of range value
    #[error("Value out of range: {field} = {value}, expected {min}..{max}")]
    OutOfRange {
        field: &'static str,
        value: f64,
        min: f64,
        max: f64,
    },

    #[cfg(feature = "serde")]
    /// JSON serialization error
    #[error("JSON error: {0}")]
    Json(#[from] serde_json::Error),
}

impl OrefError {
    /// Create an out of range error
    pub fn out_of_range(field: &'static str, value: f64, min: f64, max: f64) -> Self {
        Self::OutOfRange { field, value, min, max }
    }
}
