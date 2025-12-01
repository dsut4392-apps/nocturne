//! Insulin curve type definitions

#[cfg(feature = "serde")]
use serde::{Deserialize, Serialize};

/// Insulin action curve type
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
#[cfg_attr(feature = "serde", derive(Serialize, Deserialize))]
#[cfg_attr(feature = "serde", serde(rename_all = "kebab-case"))]
pub enum InsulinCurve {
    /// Bilinear curve (legacy, simple triangular model)
    /// - Fixed peak at 75 minutes
    /// - Fixed end at 180 minutes
    /// - Scales based on DIA
    Bilinear,

    /// Rapid-acting insulin curve (Novolog, Novorapid, Humalog, Apidra)
    /// - Default peak at 75 minutes
    /// - Exponential decay model
    /// - Minimum DIA of 5 hours
    #[default]
    #[cfg_attr(feature = "serde", serde(alias = "rapid-acting"))]
    RapidActing,

    /// Ultra-rapid insulin curve (Fiasp, Lyumjev)
    /// - Default peak at 55 minutes
    /// - Exponential decay model
    /// - Minimum DIA of 5 hours
    #[cfg_attr(feature = "serde", serde(alias = "ultra-rapid"))]
    UltraRapid,
}

impl InsulinCurve {
    /// Get the default peak time for this curve (minutes)
    pub fn default_peak(&self) -> u32 {
        match self {
            InsulinCurve::Bilinear => 75,
            InsulinCurve::RapidActing => 75,
            InsulinCurve::UltraRapid => 55,
        }
    }

    /// Get the minimum DIA for this curve (hours)
    pub fn min_dia(&self) -> f64 {
        match self {
            InsulinCurve::Bilinear => 3.0,
            InsulinCurve::RapidActing => 5.0,
            InsulinCurve::UltraRapid => 5.0,
        }
    }

    /// Get the minimum peak time for custom peak (minutes)
    pub fn min_peak(&self) -> u32 {
        match self {
            InsulinCurve::Bilinear => 75, // Fixed
            InsulinCurve::RapidActing => 50,
            InsulinCurve::UltraRapid => 35,
        }
    }

    /// Get the maximum peak time for custom peak (minutes)
    pub fn max_peak(&self) -> u32 {
        match self {
            InsulinCurve::Bilinear => 75, // Fixed
            InsulinCurve::RapidActing => 120,
            InsulinCurve::UltraRapid => 100,
        }
    }

    /// Check if this curve requires a longer DIA (5+ hours)
    pub fn requires_long_dia(&self) -> bool {
        match self {
            InsulinCurve::Bilinear => false,
            InsulinCurve::RapidActing => true,
            InsulinCurve::UltraRapid => true,
        }
    }

    /// Get effective DIA, enforcing minimums
    pub fn effective_dia(&self, dia: f64) -> f64 {
        dia.max(self.min_dia())
    }

    /// Get effective peak time, respecting bounds
    pub fn effective_peak(&self, peak: u32, use_custom: bool) -> u32 {
        if use_custom {
            peak.clamp(self.min_peak(), self.max_peak())
        } else {
            self.default_peak()
        }
    }
}

impl std::fmt::Display for InsulinCurve {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            InsulinCurve::Bilinear => write!(f, "bilinear"),
            InsulinCurve::RapidActing => write!(f, "rapid-acting"),
            InsulinCurve::UltraRapid => write!(f, "ultra-rapid"),
        }
    }
}

impl std::str::FromStr for InsulinCurve {
    type Err = String;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        match s.to_lowercase().as_str() {
            "bilinear" => Ok(InsulinCurve::Bilinear),
            "rapid-acting" | "rapidacting" | "rapid_acting" => Ok(InsulinCurve::RapidActing),
            "ultra-rapid" | "ultrarapid" | "ultra_rapid" => Ok(InsulinCurve::UltraRapid),
            _ => Err(format!("Unknown insulin curve: {}", s)),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_default_peaks() {
        assert_eq!(InsulinCurve::Bilinear.default_peak(), 75);
        assert_eq!(InsulinCurve::RapidActing.default_peak(), 75);
        assert_eq!(InsulinCurve::UltraRapid.default_peak(), 55);
    }

    #[test]
    fn test_min_dia() {
        assert_eq!(InsulinCurve::Bilinear.min_dia(), 3.0);
        assert_eq!(InsulinCurve::RapidActing.min_dia(), 5.0);
        assert_eq!(InsulinCurve::UltraRapid.min_dia(), 5.0);
    }

    #[test]
    fn test_parse() {
        assert_eq!("bilinear".parse::<InsulinCurve>().unwrap(), InsulinCurve::Bilinear);
        assert_eq!("rapid-acting".parse::<InsulinCurve>().unwrap(), InsulinCurve::RapidActing);
        assert_eq!("ultra-rapid".parse::<InsulinCurve>().unwrap(), InsulinCurve::UltraRapid);
    }
}
