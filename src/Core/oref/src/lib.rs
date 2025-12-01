//! # oref - OpenAPS Reference Implementation in Rust
//!
//! This crate provides a complete implementation of the OpenAPS reference algorithms
//! for calculating Insulin on Board (IOB), Carbs on Board (COB), and determining
//! optimal basal rates and Super Micro Boluses (SMB).
//!
//! ## Features
//!
//! - **IOB Calculation**: Calculate active insulin from boluses and temp basals
//! - **COB Calculation**: Calculate carb absorption using glucose deviation analysis
//! - **Autosens**: Detect insulin sensitivity changes over time
//! - **Determine Basal**: Main dosing algorithm with SMB support
//! - **Dynamic ISF**: Logarithmic and sigmoid dynamic insulin sensitivity
//!
//! ## Usage
//!
//! ```rust,ignore
//! use oref::prelude::*;
//!
//! // Create a profile
//! let profile = Profile::builder()
//!     .dia(5.0)
//!     .sens(50.0)
//!     .carb_ratio(10.0)
//!     .curve(InsulinCurve::RapidActing)
//!     .build();
//!
//! // Calculate IOB from treatment history
//! let iob = calculate_iob(&profile, &treatments, clock)?;
//! ```
//!
//! ## Feature Flags
//!
//! - `std` (default): Enable standard library support
//! - `serde`: Enable JSON serialization/deserialization
//! - `ffi`: Enable C FFI bindings
//! - `wasm`: Enable WebAssembly bindings

#![cfg_attr(not(feature = "std"), no_std)]

#[cfg(not(feature = "std"))]
extern crate alloc;

pub mod types;
pub mod insulin;
pub mod iob;
pub mod cob;
pub mod meal;
pub mod autosens;
pub mod determine_basal;
pub mod profile;
pub mod utils;
pub mod error;

#[cfg(feature = "ffi")]
pub mod ffi;

#[cfg(feature = "wasm")]
pub mod wasm;

/// Re-exports of commonly used types and functions
pub mod prelude {
    pub use crate::types::*;
    pub use crate::insulin::InsulinCurve;
    pub use crate::iob::calculate as calculate_iob;
    pub use crate::cob::calculate as calculate_cob;
    pub use crate::determine_basal::determine_basal;
    pub use crate::error::OrefError;
}

pub use error::OrefError;
pub type Result<T> = std::result::Result<T, OrefError>;
