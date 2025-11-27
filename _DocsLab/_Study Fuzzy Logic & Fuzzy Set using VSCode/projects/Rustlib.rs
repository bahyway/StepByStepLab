//! # Fuzzy Logic Study Library
//! 
//! A comprehensive Rust library for studying fuzzy logic and fuzzy sets.
//! 
//! ## Modules
//! - `membership`: Membership function definitions and implementations
//! - `operations`: Fuzzy set operations (union, intersection, complement)
//! - `inference`: Fuzzy inference systems (Mamdani, Sugeno)
//! - `defuzzification`: Methods to convert fuzzy outputs to crisp values

pub mod membership;
pub mod operations;
pub mod inference;
pub mod defuzzification;

use std::fmt;

/// Represents a fuzzy set with a name and membership function
#[derive(Clone)]
pub struct FuzzySet {
    pub name: String,
    pub membership_fn: Box<dyn Fn(f64) -> f64 + Send + Sync>,
}

impl FuzzySet {
    /// Create a new fuzzy set
    pub fn new(name: &str, membership_fn: Box<dyn Fn(f64) -> f64 + Send + Sync>) -> Self {
        FuzzySet {
            name: name.to_string(),
            membership_fn,
        }
    }

    /// Evaluate the membership degree for a given value
    pub fn membership(&self, x: f64) -> f64 {
        (self.membership_fn)(x).clamp(0.0, 1.0)
    }

    /// Get multiple membership values for a range
    pub fn membership_vec(&self, x_values: &[f64]) -> Vec<f64> {
        x_values.iter().map(|&x| self.membership(x)).collect()
    }
}

impl fmt::Debug for FuzzySet {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "FuzzySet {{ name: {} }}", self.name)
    }
}

/// Linguistic variable with multiple fuzzy sets
pub struct LinguisticVariable {
    pub name: String,
    pub universe: (f64, f64),
    pub fuzzy_sets: Vec<FuzzySet>,
}

impl LinguisticVariable {
    pub fn new(name: &str, universe: (f64, f64)) -> Self {
        LinguisticVariable {
            name: name.to_string(),
            universe,
            fuzzy_sets: Vec::new(),
        }
    }

    pub fn add_set(&mut self, fuzzy_set: FuzzySet) {
        self.fuzzy_sets.push(fuzzy_set);
    }

    /// Fuzzify a crisp value - return membership in all sets
    pub fn fuzzify(&self, value: f64) -> Vec<(String, f64)> {
        self.fuzzy_sets
            .iter()
            .map(|set| (set.name.clone(), set.membership(value)))
            .collect()
    }

    /// Get the set with maximum membership for a value
    pub fn classify(&self, value: f64) -> Option<String> {
        self.fuzzy_sets
            .iter()
            .map(|set| (set.name.clone(), set.membership(value)))
            .max_by(|a, b| a.1.partial_cmp(&b.1).unwrap())
            .map(|(name, _)| name)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::membership::MembershipFunction;

    #[test]
    fn test_fuzzy_set_creation() {
        let triangular = MembershipFunction::Triangular { a: 0.0, b: 5.0, c: 10.0 };
        let set = FuzzySet::new("test", Box::new(move |x| triangular.evaluate(x)));
        
        assert_eq!(set.membership(5.0), 1.0);
        assert!(set.membership(2.5) > 0.0 && set.membership(2.5) < 1.0);
    }

    #[test]
    fn test_linguistic_variable() {
        let mut temp = LinguisticVariable::new("temperature", (0.0, 50.0));
        
        let cold = MembershipFunction::Triangular { a: 0.0, b: 0.0, c: 20.0 };
        let hot = MembershipFunction::Triangular { a: 30.0, b: 50.0, c: 50.0 };
        
        temp.add_set(FuzzySet::new("cold", Box::new(move |x| cold.evaluate(x))));
        temp.add_set(FuzzySet::new("hot", Box::new(move |x| hot.evaluate(x))));
        
        let classification = temp.classify(10.0);
        assert_eq!(classification, Some("cold".to_string()));
    }
}