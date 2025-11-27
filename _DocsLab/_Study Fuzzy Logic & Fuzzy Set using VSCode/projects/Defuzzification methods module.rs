//! Defuzzification methods module
//! 
//! Convert fuzzy outputs back to crisp values using various methods:
//! - Centroid (Center of Gravity/Area)
//! - Bisector (Area Division)
//! - Mean of Maximum (MOM)
//! - Smallest of Maximum (SOM)
//! - Largest of Maximum (LOM)

use crate::FuzzySet;

/// Defuzzification method enum
#[derive(Debug, Clone, Copy)]
pub enum DefuzzMethod {
    Centroid,
    Bisector,
    MeanOfMaximum,
    SmallestOfMaximum,
    LargestOfMaximum,
}

/// Defuzzify a fuzzy set using the specified method
pub fn defuzzify(
    fuzzy_set: &FuzzySet,
    universe: &[f64],
    method: DefuzzMethod,
) -> f64 {
    match method {
        DefuzzMethod::Centroid => centroid(fuzzy_set, universe),
        DefuzzMethod::Bisector => bisector(fuzzy_set, universe),
        DefuzzMethod::MeanOfMaximum => mean_of_maximum(fuzzy_set, universe),
        DefuzzMethod::SmallestOfMaximum => smallest_of_maximum(fuzzy_set, universe),
        DefuzzMethod::LargestOfMaximum => largest_of_maximum(fuzzy_set, universe),
    }
}

/// Centroid defuzzification (Center of Gravity/Area)
/// Returns the center of the area under the membership function
pub fn centroid(fuzzy_set: &FuzzySet, universe: &[f64]) -> f64 {
    let memberships = fuzzy_set.membership_vec(universe);
    
    let numerator: f64 = universe
        .iter()
        .zip(memberships.iter())
        .map(|(&x, &mu)| x * mu)
        .sum();
    
    let denominator: f64 = memberships.iter().sum();
    
    if denominator == 0.0 {
        // Return middle of universe if no membership
        (universe.first().unwrap() + universe.last().unwrap()) / 2.0
    } else {
        numerator / denominator
    }
}

/// Bisector defuzzification
/// Returns the value that divides the area under the curve into two equal parts
pub fn bisector(fuzzy_set: &FuzzySet, universe: &[f64]) -> f64 {
    let memberships = fuzzy_set.membership_vec(universe);
    let total_area: f64 = memberships.iter().sum();
    let half_area = total_area / 2.0;
    
    let mut cumulative_area = 0.0;
    
    for (i, &mu) in memberships.iter().enumerate() {
        cumulative_area += mu;
        if cumulative_area >= half_area {
            return universe[i];
        }
    }
    
    *universe.last().unwrap()
}

/// Mean of Maximum (MOM) defuzzification
/// Returns the average of all values with maximum membership
pub fn mean_of_maximum(fuzzy_set: &FuzzySet, universe: &[f64]) -> f64 {
    let memberships = fuzzy_set.membership_vec(universe);
    
    // Find maximum membership value
    let max_membership = memberships
        .iter()
        .copied()
        .fold(f64::NEG_INFINITY, f64::max);
    
    // Find all x values with maximum membership
    let max_values: Vec<f64> = universe
        .iter()
        .zip(memberships.iter())
        .filter(|(_, &mu)| (mu - max_membership).abs() < 1e-10)
        .map(|(&x, _)| x)
        .collect();
    
    if max_values.is_empty() {
        (universe.first().unwrap() + universe.last().unwrap()) / 2.0
    } else {
        max_values.iter().sum::<f64>() / max_values.len() as f64
    }
}

/// Smallest of Maximum (SOM) defuzzification
/// Returns the smallest value with maximum membership
pub fn smallest_of_maximum(fuzzy_set: &FuzzySet, universe: &[f64]) -> f64 {
    let memberships = fuzzy_set.membership_vec(universe);
    
    let max_membership = memberships
        .iter()
        .copied()
        .fold(f64::NEG_INFINITY, f64::max);
    
    universe
        .iter()
        .zip(memberships.iter())
        .find(|(_, &mu)| (mu - max_membership).abs() < 1e-10)
        .map(|(&x, _)| x)
        .unwrap_or_else(|| *universe.first().unwrap())
}

/// Largest of Maximum (LOM) defuzzification
/// Returns the largest value with maximum membership
pub fn largest_of_maximum(fuzzy_set: &FuzzySet, universe: &[f64]) -> f64 {
    let memberships = fuzzy_set.membership_vec(universe);
    
    let max_membership = memberships
        .iter()
        .copied()
        .fold(f64::NEG_INFINITY, f64::max);
    
    universe
        .iter()
        .zip(memberships.iter())
        .rev()
        .find(|(_, &mu)| (mu - max_membership).abs() < 1e-10)
        .map(|(&x, _)| x)
        .unwrap_or_else(|| *universe.last().unwrap())
}

/// Helper to create a universe of discourse with equal spacing
pub fn create_universe(min: f64, max: f64, points: usize) -> Vec<f64> {
    let step = (max - min) / (points - 1) as f64;
    (0..points)
        .map(|i| min + i as f64 * step)
        .collect()
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::membership::MembershipFunction;

    #[test]
    fn test_centroid() {
        let mf = MembershipFunction::Triangular { a: 0.0, b: 5.0, c: 10.0 };
        let set = FuzzySet::new("test", Box::new(move |x| mf.evaluate(x)));
        let universe = create_universe(0.0, 10.0, 101);
        
        let result = centroid(&set, &universe);
        // For a symmetric triangular function, centroid should be at peak
        assert!((result - 5.0).abs() < 0.1);
    }

    #[test]
    fn test_mean_of_maximum() {
        let mf = MembershipFunction::Triangular { a: 0.0, b: 5.0, c: 10.0 };
        let set = FuzzySet::new("test", Box::new(move |x| mf.evaluate(x)));
        let universe = create_universe(0.0, 10.0, 101);
        
        let result = mean_of_maximum(&set, &universe);
        // MOM should return the peak
        assert!((result - 5.0).abs() < 0.1);
    }

    #[test]
    fn test_create_universe() {
        let universe = create_universe(0.0, 10.0, 11);
        assert_eq!(universe.len(), 11);
        assert_eq!(universe[0], 0.0);
        assert_eq!(universe[10], 10.0);
    }
}