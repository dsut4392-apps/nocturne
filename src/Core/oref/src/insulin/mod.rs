//! Insulin curve models and calculations
//!
//! This module implements the insulin activity curves used to calculate
//! Insulin on Board (IOB) and insulin activity over time.

mod curves;
mod calculate;

pub use curves::InsulinCurve;
pub use calculate::{calculate_iob_contrib, BilinearCurve, ExponentialCurve};
