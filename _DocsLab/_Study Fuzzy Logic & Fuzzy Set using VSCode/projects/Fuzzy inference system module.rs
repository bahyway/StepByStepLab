//! Fuzzy inference system module
//! 
//! Implements Mamdani-style fuzzy inference systems

use crate::{FuzzySet, operations};
use std::collections::HashMap;

/// Represents a fuzzy rule: IF antecedent THEN consequent
#[derive(Clone)]
pub struct FuzzyRule {
    pub name: String,
    pub antecedent: Box<dyn Fn(&HashMap<String, f64>) -> f64 + Send + Sync>,
    pub consequent: String,
}

impl FuzzyRule {
    pub fn new(
        name: &str,
        antecedent: Box<dyn Fn(&HashMap<String, f64>) -> f64 + Send + Sync>,
        consequent: &str,
    ) -> Self {
        FuzzyRule {
            name: name.to_string(),
            antecedent,
            consequent: consequent.to_string(),
        }
    }

    /// Evaluate the rule given input memberships
    pub fn evaluate(&self, inputs: &HashMap<String, f64>) -> f64 {
        (self.antecedent)(inputs)
    }
}

/// Fuzzy Inference System
pub struct FuzzyInferenceSystem {
    pub name: String,
    pub rules: Vec<FuzzyRule>,
    pub output_sets: HashMap<String, FuzzySet>,
}

impl FuzzyInferenceSystem {
    pub fn new(name: &str) -> Self {
        FuzzyInferenceSystem {
            name: name.to_string(),
            rules: Vec::new(),
            output_sets: HashMap::new(),
        }
    }

    /// Add a fuzzy rule to the system
    pub fn add_rule(&mut self, rule: FuzzyRule) {
        self.rules.push(rule);
    }

    /// Add an output fuzzy set
    pub fn add_output_set(&mut self, name: String, fuzzy_set: FuzzySet) {
        self.output_sets.insert(name, fuzzy_set);
    }

    /// Infer output given crisp inputs (Mamdani method)
    pub fn infer(&self, inputs: &HashMap<String, f64>) -> HashMap<String, f64> {
        let mut output_activations: HashMap<String, Vec<f64>> = HashMap::new();

        // Evaluate all rules
        for rule in &self.rules {
            let activation = rule.evaluate(inputs);
            
            output_activations
                .entry(rule.consequent.clone())
                .or_insert_with(Vec::new)
                .push(activation);
        }

        // Aggregate activations for each output (using max)
        output_activations
            .iter()
            .map(|(key, values)| {
                let max_activation = values.iter().copied().fold(f64::NEG_INFINITY, f64::max);
                (key.clone(), max_activation)
            })
            .collect()
    }
}

/// Helper functions for creating rule antecedents

/// AND operation for rule antecedents (minimum)
pub fn and(conditions: Vec<f64>) -> f64 {
    conditions.iter().copied().fold(f64::INFINITY, f64::min)
}

/// OR operation for rule antecedents (maximum)
pub fn or(conditions: Vec<f64>) -> f64 {
    conditions.iter().copied().fold(f64::NEG_INFINITY, f64::max)
}

/// NOT operation for rule antecedents
pub fn not(value: f64) -> f64 {
    1.0 - value
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::membership::MembershipFunction;

    #[test]
    fn test_fuzzy_rule_evaluation() {
        let mut inputs = HashMap::new();
        inputs.insert("temp_cold".to_string(), 0.8);
        inputs.insert("temp_hot".to_string(), 0.2);

        let rule = FuzzyRule::new(
            "Rule1",
            Box::new(|inputs| {
                inputs.get("temp_cold").copied().unwrap_or(0.0)
            }),
            "heater_high",
        );

        let activation = rule.evaluate(&inputs);
        assert_eq!(activation, 0.8);
    }

    #[test]
    fn test_and_or_operations() {
        assert_eq!(and(vec![0.7, 0.3, 0.9]), 0.3);
        assert_eq!(or(vec![0.7, 0.3, 0.9]), 0.9);
        assert_eq!(not(0.7), 0.3);
    }

    #[test]
    fn test_inference_system() {
        let mut fis = FuzzyInferenceSystem::new("Test FIS");

        // Add a simple rule
        let rule = FuzzyRule::new(
            "Rule1",
            Box::new(|inputs| {
                inputs.get("input1").copied().unwrap_or(0.0)
            }),
            "output_high",
        );

        fis.add_rule(rule);

        let mut inputs = HashMap::new();
        inputs.insert("input1".to_string(), 0.8);

        let outputs = fis.infer(&inputs);
        assert_eq!(outputs.get("output_high"), Some(&0.8));
    }
}