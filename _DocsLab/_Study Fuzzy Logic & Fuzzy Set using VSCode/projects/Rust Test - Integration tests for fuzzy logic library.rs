//! Integration tests for fuzzy logic library
//! 
//! Run with: cargo test

use fuzzy_logic_study::{
    membership::MembershipFunction,
    operations::{union, intersection, complement, tnorms, snorms},
    defuzzification::{defuzzify, DefuzzMethod, create_universe},
    inference::{FuzzyInferenceSystem, FuzzyRule},
    FuzzySet, LinguisticVariable,
};
use std::collections::HashMap;

#[test]
fn test_triangular_membership() {
    let mf = MembershipFunction::Triangular {
        a: 0.0,
        b: 5.0,
        c: 10.0,
    };

    // Test key points
    assert_eq!(mf.evaluate(0.0), 0.0, "Left boundary should be 0");
    assert_eq!(mf.evaluate(5.0), 1.0, "Peak should be 1");
    assert_eq!(mf.evaluate(10.0), 0.0, "Right boundary should be 0");
    
    // Test slopes
    assert!((mf.evaluate(2.5) - 0.5).abs() < 1e-10, "Rising slope should be linear");
    assert!((mf.evaluate(7.5) - 0.5).abs() < 1e-10, "Falling slope should be linear");
    
    // Test outside bounds
    assert_eq!(mf.evaluate(-1.0), 0.0, "Below range should be 0");
    assert_eq!(mf.evaluate(11.0), 0.0, "Above range should be 0");
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
    assert_eq!(mf.evaluate(2.0), 1.0);
    assert_eq!(mf.evaluate(5.0), 1.0, "Flat top should be 1");
    assert_eq!(mf.evaluate(8.0), 1.0);
    assert_eq!(mf.evaluate(10.0), 0.0);
}

#[test]
fn test_gaussian_membership() {
    let mf = MembershipFunction::Gaussian {
        mean: 5.0,
        sigma: 1.0,
    };

    assert_eq!(mf.evaluate(5.0), 1.0, "At mean, membership should be 1");
    assert!(mf.evaluate(4.0) < 1.0, "Away from mean should be less than 1");
    assert!(mf.evaluate(6.0) < 1.0, "Away from mean should be less than 1");
    assert!(mf.evaluate(4.0) > 0.5, "One sigma away should be > 0.5");
}

#[test]
fn test_fuzzy_set_operations() {
    let mf1 = MembershipFunction::Triangular {
        a: 0.0,
        b: 5.0,
        c: 10.0,
    };
    let mf2 = MembershipFunction::Triangular {
        a: 5.0,
        b: 10.0,
        c: 15.0,
    };

    let set1 = FuzzySet::new("Set1", Box::new(move |x| mf1.evaluate(x)));
    let set2 = FuzzySet::new("Set2", Box::new(move |x| mf2.evaluate(x)));

    // Test union
    let union_set = union(&set1, &set2, "Union");
    assert_eq!(union_set.membership(5.0), 1.0, "Union at overlap peak");
    
    // Test intersection
    let intersection_set = intersection(&set1, &set2, "Intersection");
    let intersection_val = intersection_set.membership(7.5);
    assert!(intersection_val > 0.0 && intersection_val < 1.0, "Intersection in overlap");
    
    // Test complement
    let complement_set = complement(&set1, "Not Set1");
    assert_eq!(complement_set.membership(5.0), 0.0, "Complement at peak");
    assert_eq!(complement_set.membership(0.0), 1.0, "Complement outside");
}

#[test]
fn test_tnorms_properties() {
    let a = 0.7;
    let b = 0.3;

    // Test minimum
    let min = tnorms::minimum(a, b);
    assert_eq!(min, 0.3);
    
    // Test commutativity
    assert_eq!(tnorms::minimum(a, b), tnorms::minimum(b, a));
    assert_eq!(tnorms::algebraic_product(a, b), tnorms::algebraic_product(b, a));
    
    // Test boundary conditions
    assert_eq!(tnorms::minimum(1.0, a), a, "Identity element");
    assert_eq!(tnorms::minimum(0.0, a), 0.0, "Annihilator");
}

#[test]
fn test_snorms_properties() {
    let a = 0.7;
    let b = 0.3;

    // Test maximum
    let max = snorms::maximum(a, b);
    assert_eq!(max, 0.7);
    
    // Test commutativity
    assert_eq!(snorms::maximum(a, b), snorms::maximum(b, a));
    assert_eq!(snorms::algebraic_sum(a, b), snorms::algebraic_sum(b, a));
    
    // Test boundary conditions
    assert_eq!(snorms::maximum(0.0, a), a, "Identity element");
    assert_eq!(snorms::maximum(1.0, a), 1.0, "Annihilator");
}

#[test]
fn test_linguistic_variable() {
    let mut temperature = LinguisticVariable::new("Temperature", (0.0, 50.0));

    let cold = MembershipFunction::Triangular {
        a: 0.0,
        b: 0.0,
        c: 20.0,
    };
    let hot = MembershipFunction::Triangular {
        a: 30.0,
        b: 50.0,
        c: 50.0,
    };

    temperature.add_set(FuzzySet::new("Cold", Box::new(move |x| cold.evaluate(x))));
    temperature.add_set(FuzzySet::new("Hot", Box::new(move |x| hot.evaluate(x))));

    // Test fuzzification
    let memberships = temperature.fuzzify(10.0);
    assert_eq!(memberships.len(), 2);
    
    // Test classification
    let classification = temperature.classify(10.0);
    assert_eq!(classification, Some("Cold".to_string()));
    
    let classification = temperature.classify(40.0);
    assert_eq!(classification, Some("Hot".to_string()));
}

#[test]
fn test_defuzzification_methods() {
    let mf = MembershipFunction::Triangular {
        a: 0.0,
        b: 5.0,
        c: 10.0,
    };
    let set = FuzzySet::new("Test", Box::new(move |x| mf.evaluate(x)));
    let universe = create_universe(0.0, 10.0, 101);

    // Test centroid (should be close to 5.0 for symmetric triangle)
    let centroid = defuzzify(&set, &universe, DefuzzMethod::Centroid);
    assert!((centroid - 5.0).abs() < 0.1, "Centroid should be near peak");

    // Test mean of maximum
    let mom = defuzzify(&set, &universe, DefuzzMethod::MeanOfMaximum);
    assert!((mom - 5.0).abs() < 0.1, "MOM should be at peak");

    // Test smallest of maximum
    let som = defuzzify(&set, &universe, DefuzzMethod::SmallestOfMaximum);
    assert!((som - 5.0).abs() < 0.1, "SOM should be at peak");
}

#[test]
fn test_fuzzy_inference_system() {
    let mut fis = FuzzyInferenceSystem::new("Test FIS");

    // Create simple rule: IF input1 is high THEN output is high
    let rule = FuzzyRule::new(
        "Rule1",
        Box::new(|inputs| inputs.get("input1").copied().unwrap_or(0.0)),
        "output_high",
    );

    fis.add_rule(rule);

    // Test inference
    let mut inputs = HashMap::new();
    inputs.insert("input1".to_string(), 0.8);

    let outputs = fis.infer(&inputs);
    assert_eq!(outputs.get("output_high"), Some(&0.8));
}

#[test]
fn test_create_universe() {
    let universe = create_universe(0.0, 10.0, 11);
    
    assert_eq!(universe.len(), 11);
    assert_eq!(universe[0], 0.0);
    assert_eq!(universe[10], 10.0);
    assert_eq!(universe[5], 5.0);
}

#[test]
fn test_membership_function_continuity() {
    let mf = MembershipFunction::Gaussian {
        mean: 5.0,
        sigma: 1.0,
    };

    // Test that function is continuous (no sudden jumps)
    let x1 = 5.0;
    let x2 = 5.01;
    let diff = (mf.evaluate(x1) - mf.evaluate(x2)).abs();
    
    assert!(diff < 0.01, "Function should be continuous");
}

#[test]
fn test_membership_function_normality() {
    // Test that membership functions can reach 1.0
    let mf = MembershipFunction::Triangular {
        a: 0.0,
        b: 5.0,
        c: 10.0,
    };
    
    let max_value = (0..=100)
        .map(|i| mf.evaluate(i as f64 / 10.0))
        .fold(f64::NEG_INFINITY, f64::max);
    
    assert_eq!(max_value, 1.0, "MF should be normal (reach 1.0)");
}

#[test]
fn test_fuzzy_set_convexity() {
    // A fuzzy set is convex if all α-cuts are convex sets
    let mf = MembershipFunction::Triangular {
        a: 0.0,
        b: 5.0,
        c: 10.0,
    };

    // Test for convexity: μ(λx₁ + (1-λ)x₂) ≥ min(μ(x₁), μ(x₂))
    let x1 = 2.0;
    let x2 = 8.0;
    let lambda = 0.5;
    let x_mid = lambda * x1 + (1.0 - lambda) * x2;

    let mu_x1 = mf.evaluate(x1);
    let mu_x2 = mf.evaluate(x2);
    let mu_mid = mf.evaluate(x_mid);

    assert!(
        mu_mid >= mu_x1.min(mu_x2),
        "Triangular MF should be convex"
    );
}

#[test]
fn test_de_morgans_laws() {
    let mf1 = MembershipFunction::Triangular {
        a: 0.0,
        b: 5.0,
        c: 10.0,
    };
    let mf2 = MembershipFunction::Triangular {
        a: 5.0,
        b: 10.0,
        c: 15.0,
    };

    let set1 = FuzzySet::new("Set1", Box::new(move |x| mf1.evaluate(x)));
    let set2 = FuzzySet::new("Set2", Box::new(move |x| mf2.evaluate(x)));

    // De Morgan's Law: (A ∪ B)' = A' ∩ B'
    let union_set = union(&set1, &set2, "Union");
    let complement_union = complement(&union_set, "Not Union");

    let complement_set1 = complement(&set1, "Not Set1");
    let complement_set2 = complement(&set2, "Not Set2");
    let intersection_complements = intersection(&complement_set1, &complement_set2, "Intersection");

    // Test at a point
    let test_x = 7.5;
    let lhs = complement_union.membership(test_x);
    let rhs = intersection_complements.membership(test_x);

    assert!(
        (lhs - rhs).abs() < 1e-10,
        "De Morgan's law should hold"
    );
}

#[test]
fn test_fuzzy_system_temperature_controller() {
    // Integration test: complete temperature controller
    let mut temperature = LinguisticVariable::new("Temperature", (0.0, 40.0));

    let cold = MembershipFunction::Trapezoidal {
        a: 0.0,
        b: 0.0,
        c: 15.0,
        d: 20.0,
    };
    let comfortable = MembershipFunction::Triangular {
        a: 18.0,
        b: 23.0,
        c: 28.0,
    };
    let hot = MembershipFunction::Trapezoidal {
        a: 25.0,
        b: 30.0,
        c: 40.0,
        d: 40.0,
    };

    temperature.add_set(FuzzySet::new("Cold", Box::new(move |x| cold.evaluate(x))));
    temperature.add_set(FuzzySet::new("Comfortable", Box::new(move |x| comfortable.evaluate(x))));
    temperature.add_set(FuzzySet::new("Hot", Box::new(move |x| hot.evaluate(x))));

    // Test classification at different temperatures
    assert_eq!(temperature.classify(10.0), Some("Cold".to_string()));
    assert_eq!(temperature.classify(22.0), Some("Comfortable".to_string()));
    assert_eq!(temperature.classify(35.0), Some("Hot".to_string()));

    // Test fuzzification - temperature that belongs to multiple sets
    let memberships = temperature.fuzzify(20.0);
    
    // At 20°C, should have membership in both Cold and Comfortable
    let cold_membership = memberships.iter()
        .find(|(name, _)| name == "Cold")
        .map(|(_, degree)| *degree)
        .unwrap_or(0.0);
    
    let comfortable_membership = memberships.iter()
        .find(|(name, _)| name == "Comfortable")
        .map(|(_, degree)| *degree)
        .unwrap_or(0.0);

    assert!(cold_membership > 0.0, "Should have some cold membership");
    assert!(comfortable_membership > 0.0, "Should have some comfortable membership");
}

#[test]
fn test_performance_large_universe() {
    // Performance test with large universe
    let universe = create_universe(0.0, 1000.0, 10001);
    let mf = MembershipFunction::Gaussian {
        mean: 500.0,
        sigma: 100.0,
    };
    let set = FuzzySet::new("Large", Box::new(move |x| mf.evaluate(x)));

    let start = std::time::Instant::now();
    let _memberships = set.membership_vec(&universe);
    let duration = start.elapsed();

    assert!(duration.as_millis() < 100, "Should compute quickly");
}

#[cfg(test)]
mod bench {
    use super::*;

    #[test]
    fn bench_membership_evaluation() {
        let mf = MembershipFunction::Triangular {
            a: 0.0,
            b: 5.0,
            c: 10.0,
        };

        let start = std::time::Instant::now();
        for i in 0..100000 {
            let _ = mf.evaluate((i % 1000) as f64 / 100.0);
        }
        let duration = start.elapsed();
        
        println!("100k evaluations took: {:?}", duration);
        assert!(duration.as_millis() < 100, "Should be fast");
    }
}