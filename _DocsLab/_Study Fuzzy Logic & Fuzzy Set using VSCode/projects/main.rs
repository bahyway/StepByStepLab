//! Fuzzy Logic Study - Interactive Learning Program
//! 
//! Run with: cargo run
//! Run examples: cargo run --example temperature_controller

use fuzzy_logic_study::{
    membership::{MembershipFunction, presets},
    operations::{union, intersection, complement, tnorms, snorms},
    FuzzySet, LinguisticVariable,
};
use std::io::{self, Write};

pub mod Fuzzy set operations module;
pub mod Rust_Membership;
pub mod Rustlib;


fn main() {
    println!("╔════════════════════════════════════════════════════════╗");
    println!("║   FUZZY LOGIC & FUZZY SETS - RUST STUDY PROGRAM      ║");
    println!("╚════════════════════════════════════════════════════════╝\n");

    loop {
        println!("\n📚 Choose a lesson:");
        println!("  1. Membership Functions");
        println!("  2. Fuzzy Set Operations");
        println!("  3. Linguistic Variables");
        println!("  4. T-norms and S-norms");
        println!("  5. Temperature Classification Example");
        println!("  6. Run All Lessons");
        println!("  0. Exit");
        
        print!("\nEnter your choice: ");
        io::stdout().flush().unwrap();
        
        let mut input = String::new();
        io::stdin().read_line(&mut input).unwrap();
        
        match input.trim() {
            "1" => lesson_1_membership_functions(),
            "2" => lesson_2_fuzzy_operations(),
            "3" => lesson_3_linguistic_variables(),
            "4" => lesson_4_tnorms_snorms(),
            "5" => lesson_5_temperature_example(),
            "6" => run_all_lessons(),
            "0" => {
                println!("\n🎓 Happy learning! Goodbye!");
                break;
            }
            _ => println!("❌ Invalid choice! Please try again."),
        }
    }
}

fn lesson_1_membership_functions() {
    println!("\n{'=':.>60}", "");
    println!("LESSON 1: Membership Functions");
    println!("{'=':.>60}\n", "");

    println!("Membership functions map crisp values to fuzzy degrees [0, 1].\n");

    // Triangular
    let triangular = MembershipFunction::Triangular {
        a: 0.0,
        b: 5.0,
        c: 10.0,
    };
    
    println!("📐 Triangular MF (a=0, b=5, c=10):");
    for x in [0.0, 2.5, 5.0, 7.5, 10.0] {
        println!("   x={:<4} → μ(x)={:.3}", x, triangular.evaluate(x));
    }

    // Gaussian
    let gaussian = MembershipFunction::Gaussian {
        mean: 5.0,
        sigma: 1.5,
    };
    
    println!("\n🔔 Gaussian MF (mean=5, sigma=1.5):");
    for x in [0.0, 2.5, 5.0, 7.5, 10.0] {
        println!("   x={:<4} → μ(x)={:.3}", x, gaussian.evaluate(x));
    }

    // Trapezoidal
    let trapezoidal = MembershipFunction::Trapezoidal {
        a: 0.0,
        b: 2.0,
        c: 8.0,
        d: 10.0,
    };
    
    println!("\n⏹️  Trapezoidal MF (a=0, b=2, c=8, d=10):");
    for x in [0.0, 2.5, 5.0, 7.5, 10.0] {
        println!("   x={:<4} → μ(x)={:.3}", x, trapezoidal.evaluate(x));
    }

    println!("\n✅ Key Insight:");
    println!("   Different MF shapes give different characteristics:");
    println!("   • Triangular: Simple, sharp peak");
    println!("   • Gaussian: Smooth, bell-curved");
    println!("   • Trapezoidal: Flat top, good for ranges");
}

fn lesson_2_fuzzy_operations() {
    println!("\n{'=':.>60}", "");
    println!("LESSON 2: Fuzzy Set Operations");
    println!("{'=':.>60}\n", "");

    // Create two fuzzy sets
    let young_mf = MembershipFunction::Trapezoidal {
        a: 0.0,
        b: 0.0,
        c: 25.0,
        d: 35.0,
    };
    
    let middle_aged_mf = MembershipFunction::Trapezoidal {
        a: 30.0,
        b: 40.0,
        c: 50.0,
        d: 60.0,
    };

    let young = FuzzySet::new("Young", Box::new(move |x| young_mf.evaluate(x)));
    let middle_aged = FuzzySet::new("Middle-aged", Box::new(move |x| middle_aged_mf.evaluate(x)));

    // Perform operations
    let union_set = union(&young, &middle_aged, "Young OR Middle-aged");
    let intersection_set = intersection(&young, &middle_aged, "Young AND Middle-aged");
    let not_young = complement(&young, "NOT Young");

    println!("Testing age = 35 years old:\n");
    
    let test_age = 35.0;
    
    println!("Original Sets:");
    println!("   Young:       μ = {:.3}", young.membership(test_age));
    println!("   Middle-aged: μ = {:.3}", middle_aged.membership(test_age));
    
    println!("\nOperations:");
    println!("   Union (OR):         μ = {:.3}", union_set.membership(test_age));
    println!("   Intersection (AND): μ = {:.3}", intersection_set.membership(test_age));
    println!("   Complement (NOT):   μ = {:.3}", not_young.membership(test_age));

    println!("\n✅ Key Formulas:");
    println!("   Union:        max(μA, μB)");
    println!("   Intersection: min(μA, μB)");
    println!("   Complement:   1 - μA");
}

fn lesson_3_linguistic_variables() {
    println!("\n{'=':.>60}", "");
    println!("LESSON 3: Linguistic Variables");
    println!("{'=':.>60}\n", "");

    println!("Linguistic variables group multiple fuzzy sets.\n");

    // Create temperature linguistic variable
    let mut temperature = LinguisticVariable::new("Temperature", (0.0, 50.0));

    // Add fuzzy sets using presets
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
        c: 50.0,
        d: 50.0,
    };

    temperature.add_set(FuzzySet::new("Cold", Box::new(move |x| cold.evaluate(x))));
    temperature.add_set(FuzzySet::new("Comfortable", Box::new(move |x| comfortable.evaluate(x))));
    temperature.add_set(FuzzySet::new("Hot", Box::new(move |x| hot.evaluate(x))));

    // Test fuzzification
    let test_temps = vec![12.0, 22.0, 35.0];

    for temp in test_temps {
        println!("🌡️  Temperature: {}°C", temp);
        let fuzzy_values = temperature.fuzzify(temp);
        
        for (label, degree) in &fuzzy_values {
            println!("   {:<12} : {:.3} ({:>5.1}%)", label, degree, degree * 100.0);
        }
        
        let classification = temperature.classify(temp).unwrap();
        println!("   → Classification: {}\n", classification);
    }

    println!("✅ Fuzzification converts crisp values to fuzzy memberships!");
}

fn lesson_4_tnorms_snorms() {
    println!("\n{'=':.>60}", "");
    println!("LESSON 4: T-norms and S-norms");
    println!("{'=':.>60}\n", "");

    let a = 0.7;
    let b = 0.3;

    println!("Given two membership values:");
    println!("   μA = {}", a);
    println!("   μB = {}", b);

    println!("\n🔽 T-norms (AND operations):");
    println!("   Minimum:           {:.3}", tnorms::minimum(a, b));
    println!("   Algebraic Product: {:.3}", tnorms::algebraic_product(a, b));
    println!("   Bounded Diff:      {:.3}", tnorms::bounded_difference(a, b));
    println!("   Drastic:           {:.3}", tnorms::drastic(a, b));
    println!("   Hamacher Product:  {:.3}", tnorms::hamacher_product(a, b));

    println!("\n🔼 S-norms (OR operations):");
    println!("   Maximum:         {:.3}", snorms::maximum(a, b));
    println!("   Algebraic Sum:   {:.3}", snorms::algebraic_sum(a, b));
    println!("   Bounded Sum:     {:.3}", snorms::bounded_sum(a, b));
    println!("   Drastic:         {:.3}", snorms::drastic(a, b));
    println!("   Einstein Sum:    {:.3}", snorms::einstein_sum(a, b));

    println!("\n✅ Different norms give different behaviors!");
    println!("   Most common: Minimum (T-norm) and Maximum (S-norm)");
}

fn lesson_5_temperature_example() {
    println!("\n{'=':.>60}", "");
    println!("LESSON 5: Temperature Control System");
    println!("{'=':.>60}\n", "");

    println!("🌡️  Smart Thermostat Fuzzy Controller\n");

    // Create linguistic variable
    let mut temperature = LinguisticVariable::new("Temperature", (0.0, 45.0));

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
        c: 45.0,
        d: 45.0,
    };

    temperature.add_set(FuzzySet::new("Cold", Box::new(move |x| cold.evaluate(x))));
    temperature.add_set(FuzzySet::new("Comfortable", Box::new(move |x| comfortable.evaluate(x))));
    temperature.add_set(FuzzySet::new("Hot", Box::new(move |x| hot.evaluate(x))));

    // Fuzzy control rules (simplified)
    println!("📋 Control Rules:");
    println!("   IF Temperature is Cold       → THEN Heater ON (High)");
    println!("   IF Temperature is Comfortable → THEN Heater OFF");
    println!("   IF Temperature is Hot        → THEN AC ON (High)");

    println!("\n🔍 Testing Different Scenarios:\n");

    let scenarios = vec![
        (15.0, "Winter morning"),
        (22.0, "Pleasant day"),
        (32.0, "Hot summer afternoon"),
    ];

    for (temp, description) in scenarios {
        println!("Scenario: {}", description);
        println!("Temperature: {}°C", temp);
        
        let fuzzy_values = temperature.fuzzify(temp);
        for (label, degree) in &fuzzy_values {
            if *degree > 0.01 {
                println!("   {}: {:.2}", label, degree);
            }
        }
        
        // Simple defuzzification logic
        let action = if temp < 20.0 {
            "🔥 Turn ON Heater"
        } else if temp > 28.0 {
            "❄️  Turn ON Air Conditioner"
        } else {
            "✅ Maintain Current State"
        };
        
        println!("   → Action: {}\n", action);
    }

    println!("✅ Fuzzy control allows smooth transitions between states!");
}

fn run_all_lessons() {
    lesson_1_membership_functions();
    wait_for_continue();
    
    lesson_2_fuzzy_operations();
    wait_for_continue();
    
    lesson_3_linguistic_variables();
    wait_for_continue();
    
    lesson_4_tnorms_snorms();
    wait_for_continue();
    
    lesson_5_temperature_example();
}

fn wait_for_continue() {
    print!("\nPress Enter to continue...");
    io::stdout().flush().unwrap();
    let mut input = String::new();
    io::stdin().read_line(&mut input).unwrap();
}