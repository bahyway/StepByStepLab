//! Membership function implementations
//! 
//! This module provides various types of membership functions commonly used
//! in fuzzy logic systems.

use std::f64::consts::PI;

/// Enumeration of different membership function types
#[derive(Debug, Clone, Copy)]
pub enum MembershipFunction {
    /// Triangular membership function: trimf(x; a, b, c)
    Triangular { a: f64, b: f64, c: f64 },
    
    /// Trapezoidal membership function: trapmf(x; a, b, c, d)
    Trapezoidal { a: f64, b: f64, c: f64, d: f64 },
    
    /// Gaussian membership function: gaussmf(x; mean, sigma)
    Gaussian { mean: f64, sigma: f64 },
    
    /// Bell-shaped membership function: gbellmf(x; a, b, c)
    GeneralizedBell { a: f64, b: f64, c: f64 },
    
    /// Sigmoid membership function: sigmf(x; a, c)
    Sigmoid { a: f64, c: f64 },
    
    /// S-shaped membership function: smf(x; a, b)
    SMF { a: f64, b: f64 },
    
    /// Z-shaped membership function: zmf(x; a, b)
    ZMF { a: f64, b: f64 },
    
    /// Pi-shaped membership function: pimf(x; a, b, c, d)
    PiShaped { a: f64, b: f64, c: f64, d: f64 },
}

impl MembershipFunction {
    /// Evaluate the membership function at a given point
    pub fn evaluate(&self, x: f64) -> f64 {
        match self {
            MembershipFunction::Triangular { a, b, c } => {
                Self::triangular(x, *a, *b, *c)
            }
            MembershipFunction::Trapezoidal { a, b, c, d } => {
                Self::trapezoidal(x, *a, *b, *c, *d)
            }
            MembershipFunction::Gaussian { mean, sigma } => {
                Self::gaussian(x, *mean, *sigma)
            }
            MembershipFunction::GeneralizedBell { a, b, c } => {
                Self::generalized_bell(x, *a, *b, *c)
            }
            MembershipFunction::Sigmoid { a, c } => {
                Self::sigmoid(x, *a, *c)
            }
            MembershipFunction::SMF { a, b } => {
                Self::smf(x, *a, *b)
            }
            MembershipFunction::ZMF { a, b } => {
                Self::zmf(x, *a, *b)
            }
            MembershipFunction::PiShaped { a, b, c, d } => {
                Self::pi_shaped(x, *a, *b, *c, *d)
            }
        }
    }

    /// Triangular membership function
    fn triangular(x: f64, a: f64, b: f64, c: f64) -> f64 {
        if x <= a || x >= c {
            0.0
        } else if x == b {
            1.0
        } else if x > a && x < b {
            (x - a) / (b - a)
        } else {
            (c - x) / (c - b)
        }
    }

    /// Trapezoidal membership function
    fn trapezoidal(x: f64, a: f64, b: f64, c: f64, d: f64) -> f64 {
        if x <= a || x >= d {
            0.0
        } else if x >= b && x <= c {
            1.0
        } else if x > a && x < b {
            (x - a) / (b - a)
        } else {
            (d - x) / (d - c)
        }
    }

    /// Gaussian membership function
    fn gaussian(x: f64, mean: f64, sigma: f64) -> f64 {
        (-(x - mean).powi(2) / (2.0 * sigma.powi(2))).exp()
    }

    /// Generalized bell membership function
    fn generalized_bell(x: f64, a: f64, b: f64, c: f64) -> f64 {
        1.0 / (1.0 + ((x - c) / a).abs().powf(2.0 * b))
    }

    /// Sigmoid membership function
    fn sigmoid(x: f64, a: f64, c: f64) -> f64 {
        1.0 / (1.0 + (-a * (x - c)).exp())
    }

    /// S-shaped membership function
    fn smf(x: f64, a: f64, b: f64) -> f64 {
        if x <= a {
            0.0
        } else if x >= b {
            1.0
        } else {
            let mid = (a + b) / 2.0;
            if x <= mid {
                2.0 * ((x - a) / (b - a)).powi(2)
            } else {
                1.0 - 2.0 * ((x - b) / (b - a)).powi(2)
            }
        }
    }

    /// Z-shaped membership function
    fn zmf(x: f64, a: f64, b: f64) -> f64 {
        1.0 - Self::smf(x, a, b)
    }

    /// Pi-shaped membership function
    fn pi_shaped(x: f64, a: f64, b: f64, c: f64, d: f64) -> f64 {
        Self::smf(x, a, b) * Self::zmf(x, c, d)
    }

    /// Evaluate membership for a vector of values
    pub fn evaluate_vec(&self, x_values: &[f64]) -> Vec<f64> {
        x_values.iter().map(|&x| self.evaluate(x)).collect()
    }
}

/// Helper function to create common membership function configurations
pub mod presets {
    use super::MembershipFunction;

    /// Create a "low" triangular membership function
    pub fn low(min: f64, max: f64) -> MembershipFunction {
        MembershipFunction::Triangular {
            a: min,
            b: min,
            c: (min + max) / 2.0,
        }
    }

    /// Create a "medium" triangular membership function
    pub fn medium(min: f64, max: f64) -> MembershipFunction {
        let mid = (min + max) / 2.0;
        MembershipFunction::Triangular {
            a: min,
            b: mid,
            c: max,
        }
    }

    /// Create a "high" triangular membership function
    pub fn high(min: f64, max: f64) -> MembershipFunction {
        MembershipFunction::Triangular {
            a: (min + max) / 2.0,
            b: max,
            c: max,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_triangular_membership() {
        let mf = MembershipFunction::Triangular { a: 0.0, b: 5.0, c: 10.0 };
        
        assert_eq!(mf.evaluate(0.0), 0.0);
        assert_eq!(mf.evaluate(5.0), 1.0);
        assert_eq!(mf.evaluate(10.0), 0.0);
        assert!((mf.evaluate(2.5) - 0.5).abs() < 1e-10);
    }

    #[test]
    fn test_gaussian_membership() {
        let mf = MembershipFunction::Gaussian { mean: 5.0, sigma: 1.0 };
        
        assert_eq!(mf.evaluate(5.0), 1.0);
        assert!(mf.evaluate(4.0) < 1.0);
        assert!(mf.evaluate(6.0) < 1.0);
    }

    #[test]
    fn test_trapezoidal_membership() {
        let mf = MembershipFunction::Trapezoidal {
            a: 0.0,
            b: 2.0,
            c: 8.0,
            d: 10.0,
        };
        
        assert_eq!(mf.evaluate(0.0), 0.0);
        assert_eq!(mf.evaluate(5.0), 1.0);
        assert_eq!(mf.evaluate(10.0), 0.0);
    }
}