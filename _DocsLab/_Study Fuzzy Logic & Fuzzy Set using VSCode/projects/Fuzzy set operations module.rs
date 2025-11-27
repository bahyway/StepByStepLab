//! Fuzzy set operations module
//! 
//! Implements standard fuzzy set operations including:
//! - Union (OR)
//! - Intersection (AND)
//! - Complement (NOT)
//! - T-norms and S-norms

use crate::FuzzySet;

/// Fuzzy set union (OR operation) using maximum
pub fn union(set1: &FuzzySet, set2: &FuzzySet, name: &str) -> FuzzySet {
    let s1 = set1.clone();
    let s2 = set2.clone();
    
    FuzzySet::new(
        name,
        Box::new(move |x| {
            let mu1 = (s1.membership_fn)(x);
            let mu2 = (s2.membership_fn)(x);
            mu1.max(mu2)
        }),
    )
}

/// Fuzzy set intersection (AND operation) using minimum
pub fn intersection(set1: &FuzzySet, set2: &FuzzySet, name: &str) -> FuzzySet {
    let s1 = set1.clone();
    let s2 = set2.clone();
    
    FuzzySet::new(
        name,
        Box::new(move |x| {
            let mu1 = (s1.membership_fn)(x);
            let mu2 = (s2.membership_fn)(x);
            mu1.min(mu2)
        }),
    )
}

/// Fuzzy set complement (NOT operation)
pub fn complement(set: &FuzzySet, name: &str) -> FuzzySet {
    let s = set.clone();
    
    FuzzySet::new(
        name,
        Box::new(move |x| 1.0 - (s.membership_fn)(x)),
    )
}

/// T-norms (triangular norms) for fuzzy intersection
pub mod tnorms {
    /// Minimum T-norm (standard)
    pub fn minimum(a: f64, b: f64) -> f64 {
        a.min(b)
    }

    /// Algebraic product T-norm
    pub fn algebraic_product(a: f64, b: f64) -> f64 {
        a * b
    }

    /// Bounded difference T-norm (Lukasiewicz)
    pub fn bounded_difference(a: f64, b: f64) -> f64 {
        (a + b - 1.0).max(0.0)
    }

    /// Drastic T-norm
    pub fn drastic(a: f64, b: f64) -> f64 {
        if a == 1.0 {
            b
        } else if b == 1.0 {
            a
        } else {
            0.0
        }
    }

    /// Nilpotent minimum T-norm
    pub fn nilpotent_minimum(a: f64, b: f64) -> f64 {
        if a + b > 1.0 {
            a.min(b)
        } else {
            0.0
        }
    }

    /// Hamacher product T-norm
    pub fn hamacher_product(a: f64, b: f64) -> f64 {
        if a == 0.0 && b == 0.0 {
            0.0
        } else {
            (a * b) / (a + b - a * b)
        }
    }
}

/// S-norms (triangular conorms) for fuzzy union
pub mod snorms {
    /// Maximum S-norm (standard)
    pub fn maximum(a: f64, b: f64) -> f64 {
        a.max(b)
    }

    /// Algebraic sum S-norm
    pub fn algebraic_sum(a: f64, b: f64) -> f64 {
        a + b - a * b
    }

    /// Bounded sum S-norm (Lukasiewicz)
    pub fn bounded_sum(a: f64, b: f64) -> f64 {
        (a + b).min(1.0)
    }

    /// Drastic S-norm
    pub fn drastic(a: f64, b: f64) -> f64 {
        if a == 0.0 {
            b
        } else if b == 0.0 {
            a
        } else {
            1.0
        }
    }

    /// Nilpotent maximum S-norm
    pub fn nilpotent_maximum(a: f64, b: f64) -> f64 {
        if a + b < 1.0 {
            a.max(b)
        } else {
            1.0
        }
    }

    /// Einstein sum S-norm
    pub fn einstein_sum(a: f64, b: f64) -> f64 {
        (a + b) / (1.0 + a * b)
    }
}

/// Fuzzy intersection using a custom T-norm
pub fn intersection_tnorm<F>(
    set1: &FuzzySet,
    set2: &FuzzySet,
    name: &str,
    tnorm: F,
) -> FuzzySet
where
    F: Fn(f64, f64) -> f64 + 'static + Send + Sync,
{
    let s1 = set1.clone();
    let s2 = set2.clone();
    
    FuzzySet::new(
        name,
        Box::new(move |x| {
            let mu1 = (s1.membership_fn)(x);
            let mu2 = (s2.membership_fn)(x);
            tnorm(mu1, mu2)
        }),
    )
}

/// Fuzzy union using a custom S-norm
pub fn union_snorm<F>(
    set1: &FuzzySet,
    set2: &FuzzySet,
    name: &str,
    snorm: F,
) -> FuzzySet
where
    F: Fn(f64, f64) -> f64 + 'static + Send + Sync,
{
    let s1 = set1.clone();
    let s2 = set2.clone();
    
    FuzzySet::new(
        name,
        Box::new(move |x| {
            let mu1 = (s1.membership_fn)(x);
            let mu2 = (s2.membership_fn)(x);
            snorm(mu1, mu2)
        }),
    )
}

/// Concentration operation (hedge "very")
pub fn concentration(set: &FuzzySet, name: &str) -> FuzzySet {
    let s = set.clone();
    
    FuzzySet::new(
        name,
        Box::new(move |x| {
            let mu = (s.membership_fn)(x);
            mu.powi(2)
        }),
    )
}

/// Dilation operation (hedge "somewhat")
pub fn dilation(set: &FuzzySet, name: &str) -> FuzzySet {
    let s = set.clone();
    
    FuzzySet::new(
        name,
        Box::new(move |x| {
            let mu = (s.membership_fn)(x);
            mu.sqrt()
        }),
    )
}

/// Intensification operation (hedge "plus")
pub fn intensification(set: &FuzzySet, name: &str) -> FuzzySet {
    let s = set.clone();
    
    FuzzySet::new(
        name,
        Box::new(move |x| {
            let mu = (s.membership_fn)(x);
            if mu <= 0.5 {
                2.0 * mu.powi(2)
            } else {
                1.0 - 2.0 * (1.0 - mu).powi(2)
            }
        }),
    )
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::membership::MembershipFunction;

    #[test]
    fn test_union() {
        let mf1 = MembershipFunction::Triangular { a: 0.0, b: 5.0, c: 10.0 };
        let mf2 = MembershipFunction::Triangular { a: 5.0, b: 10.0, c: 15.0 };
        
        let set1 = FuzzySet::new("set1", Box::new(move |x| mf1.evaluate(x)));
        let set2 = FuzzySet::new("set2", Box::new(move |x| mf2.evaluate(x)));
        
        let union_set = union(&set1, &set2, "union");
        
        // At x=5, set1 is 1.0, set2 is 0.0, so union is 1.0
        assert_eq!(union_set.membership(5.0), 1.0);
    }

    #[test]
    fn test_intersection() {
        let mf1 = MembershipFunction::Triangular { a: 0.0, b: 5.0, c: 10.0 };
        let mf2 = MembershipFunction::Triangular { a: 5.0, b: 10.0, c: 15.0 };
        
        let set1 = FuzzySet::new("set1", Box::new(move |x| mf1.evaluate(x)));
        let set2 = FuzzySet::new("set2", Box::new(move |x| mf2.evaluate(x)));
        
        let intersection_set = intersection(&set1, &set2, "intersection");
        
        // At x=7.5, both have membership 0.5, so intersection is 0.5
        let result = intersection_set.membership(7.5);
        assert!((result - 0.5).abs() < 0.01);
    }

    #[test]
    fn test_complement() {
        let mf = MembershipFunction::Triangular { a: 0.0, b: 5.0, c: 10.0 };
        let set = FuzzySet::new("set", Box::new(move |x| mf.evaluate(x)));
        
        let comp = complement(&set, "not_set");
        
        assert_eq!(comp.membership(5.0), 0.0);  // 1 - 1 = 0
        assert_eq!(comp.membership(0.0), 1.0);  // 1 - 0 = 1
    }

    #[test]
    fn test_tnorms() {
        assert_eq!(tnorms::minimum(0.7, 0.3), 0.3);
        assert_eq!(tnorms::algebraic_product(0.5, 0.5), 0.25);
        assert_eq!(tnorms::bounded_difference(0.7, 0.5), 0.2);
    }

    #[test]
    fn test_snorms() {
        assert_eq!(snorms::maximum(0.7, 0.3), 0.7);
        assert_eq!(snorms::algebraic_sum(0.5, 0.5), 0.75);
        assert_eq!(snorms::bounded_sum(0.7, 0.5), 1.0);
    }
}