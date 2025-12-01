//! Core type definitions for oref
//!
//! This module contains all the data structures used throughout the oref library,
//! designed to be compatible with the original JavaScript oref0 implementation.

mod profile;
mod treatment;
mod glucose;
mod iob;
mod cob;
mod output;

pub use profile::*;
pub use treatment::*;
pub use glucose::*;
pub use iob::*;
pub use cob::*;
pub use output::*;
