//! Restaurant Tipping System
//!
//! Fuzzy logic system to determine tip percentage based on service and food quality
//! Classic fuzzy logic example
//!
//! Run with: cargo run --example tipping_system

use fuzzy_logic_study::{
    defuzzification::{create_universe, defuzzify, DefuzzMethod},
    inference::{and, or, FuzzyInferenceSystem, FuzzyRule},
    membership::MembershipFunction,
    FuzzySet, LinguisticVariable,
};
use std::collections::HashMap;

fn main() {
    println!("╔════════════════════════════════════════════════════════╗");
    println!("║          RESTAURANT TIPPING FUZZY SYSTEM              ║");
    println!("╚════════════════════════════════════════════════════════╝\n");

    // Create linguistic variables
    let mut service = create_service_variable();
    let mut food = create_food_variable();
    let mut tip = create_tip_variable();

    println!("📋 Fuzzy Rules:");
    println!("   Rule 1: IF service is Poor OR food is Poor → tip is Low");
    println!("   Rule 2: IF service is Average → tip is Medium");
    println!("   Rule 3: IF service is Good OR food is Good → tip is High");
    println!("   Rule 4: IF service is Good AND food is Good → tip is Very High");
    println!();

    // Test scenarios
    let scenarios = vec![
        (3.0, 4.0, "Poor service, poor food"),
        (5.0, 6.0, "Average service, decent food"),
        (8.0, 7.0, "Good service, good food"),
        (9.5, 9.5, "Excellent service, excellent food"),
        (9.0, 4.0, "Great service, poor food"),
        (4.0, 9.0, "Poor service, great food"),
        (6.5, 5.5, "Slightly above average"),
    ];

    println!("🧪 Testing Scenarios:\n");
    println!(
        "{:<10} {:<10} {:<35} {:<12}",
        "Service", "Food", "Description", "Tip %"
    );
    println!("{}", "─".repeat(75));

    for (service_val, food_val, description) in &scenarios {
        let tip_percentage = calculate_tip(*service_val, *food_val, &service, &food, &tip);

        println!(
            "{:<10.1} {:<10.1} {:<35} {:<12.1}%",
            service_val, food_val, description, tip_percentage
        );

        // Show detailed analysis
        let service_fuzz = service.fuzzify(*service_val);
        let food_fuzz = food.fuzzify(*food_val);

        println!("   Service: {}", format_memberships(&service_fuzz));
        println!("   Food:    {}", format_memberships(&food_fuzz));
        println!();
    }

    // Interactive mode
    println!("\n{'=':.>60}", "");
    println!("🍽️  Calculate Your Tip!");
    println!("{'=':.>60}\n", "");

    use std::io::{self, Write};

    loop {
        print!("Rate the service quality (0-10) or 'q' to quit: ");
        io::stdout().flush().unwrap();

        let mut input = String::new();
        io::stdin().read_line(&mut input).unwrap();

        if input.trim() == "q" {
            break;
        }

        let service_val: f64 = match input.trim().parse() {
            Ok(val) if val >= 0.0 && val <= 10.0 => val,
            _ => {
                println!("❌ Please enter a number between 0 and 10!");
                continue;
            }
        };

        print!("Rate the food quality (0-10): ");
        io::stdout().flush().unwrap();

        input.clear();
        io::stdin().read_line(&mut input).unwrap();

        let food_val: f64 = match input.trim().parse() {
            Ok(val) if val >= 0.0 && val <= 10.0 => val,
            _ => {
                println!("❌ Please enter a number between 0 and 10!");
                continue;
            }
        };

        let tip_percentage = calculate_tip(service_val, food_val, &service, &food, &tip);

        println!("\n📊 Analysis:");
        println!(
            "   Service quality: {}/10 → {}",
            service_val,
            service
                .classify(service_val)
                .unwrap_or("unknown".to_string())
        );
        println!(
            "   Food quality:    {}/10 → {}",
            food_val,
            food.classify(food_val).unwrap_or("unknown".to_string())
        );
        println!("\n💰 Recommended tip: {:.1}%", tip_percentage);

        if tip_percentage >= 20.0 {
            println!("   🌟 Exceptional experience!");
        } else if tip_percentage >= 15.0 {
            println!("   ✅ Good experience");
        } else if tip_percentage >= 10.0 {
            println!("   👌 Acceptable experience");
        } else {
            println!("   😕 Below expectations");
        }
        println!();
    }

    println!("\n✅ Thank you for using the fuzzy tipping system!");
}

fn create_service_variable() -> LinguisticVariable {
    let mut service = LinguisticVariable::new("Service Quality", (0.0, 10.0));

    let poor = MembershipFunction::Trapezoidal {
        a: 0.0,
        b: 0.0,
        c: 2.5,
        d: 5.0,
    };

    let average = MembershipFunction::Triangular {
        a: 2.5,
        b: 5.0,
        c: 7.5,
    };

    let good = MembershipFunction::Trapezoidal {
        a: 5.0,
        b: 7.5,
        c: 10.0,
        d: 10.0,
    };

    service.add_set(FuzzySet::new("Poor", Box::new(move |x| poor.evaluate(x))));
    service.add_set(FuzzySet::new(
        "Average",
        Box::new(move |x| average.evaluate(x)),
    ));
    service.add_set(FuzzySet::new("Good", Box::new(move |x| good.evaluate(x))));

    service
}

fn create_food_variable() -> LinguisticVariable {
    let mut food = LinguisticVariable::new("Food Quality", (0.0, 10.0));

    let poor = MembershipFunction::Trapezoidal {
        a: 0.0,
        b: 0.0,
        c: 2.5,
        d: 5.0,
    };

    let average = MembershipFunction::Triangular {
        a: 2.5,
        b: 5.0,
        c: 7.5,
    };

    let good = MembershipFunction::Trapezoidal {
        a: 5.0,
        b: 7.5,
        c: 10.0,
        d: 10.0,
    };

    food.add_set(FuzzySet::new("Poor", Box::new(move |x| poor.evaluate(x))));
    food.add_set(FuzzySet::new(
        "Average",
        Box::new(move |x| average.evaluate(x)),
    ));
    food.add_set(FuzzySet::new("Good", Box::new(move |x| good.evaluate(x))));

    food
}

fn create_tip_variable() -> LinguisticVariable {
    let mut tip = LinguisticVariable::new("Tip Percentage", (0.0, 30.0));

    let low = MembershipFunction::Triangular {
        a: 0.0,
        b: 5.0,
        c: 10.0,
    };

    let medium = MembershipFunction::Triangular {
        a: 8.0,
        b: 13.0,
        c: 18.0,
    };

    let high = MembershipFunction::Triangular {
        a: 15.0,
        b: 20.0,
        c: 25.0,
    };

    let very_high = MembershipFunction::Triangular {
        a: 22.0,
        b: 27.0,
        c: 30.0,
    };

    tip.add_set(FuzzySet::new("Low", Box::new(move |x| low.evaluate(x))));
    tip.add_set(FuzzySet::new(
        "Medium",
        Box::new(move |x| medium.evaluate(x)),
    ));
    tip.add_set(FuzzySet::new("High", Box::new(move |x| high.evaluate(x))));
    tip.add_set(FuzzySet::new(
        "Very High",
        Box::new(move |x| very_high.evaluate(x)),
    ));

    tip
}

fn calculate_tip(
    service_val: f64,
    food_val: f64,
    service: &LinguisticVariable,
    food: &LinguisticVariable,
    tip: &LinguisticVariable,
) -> f64 {
    // Fuzzify inputs
    let service_memberships = service.fuzzify(service_val);
    let food_memberships = food.fuzzify(food_val);

    // Helper to get membership
    let get_membership = |memberships: &[(String, f64)], label: &str| -> f64 {
        memberships
            .iter()
            .find(|(l, _)| l == label)
            .map(|(_, v)| *v)
            .unwrap_or(0.0)
    };

    let service_poor = get_membership(&service_memberships, "Poor");
    let service_avg = get_membership(&service_memberships, "Average");
    let service_good = get_membership(&service_memberships, "Good");

    let food_poor = get_membership(&food_memberships, "Poor");
    let food_avg = get_membership(&food_memberships, "Average");
    let food_good = get_membership(&food_memberships, "Good");

    // Apply rules
    // Rule 1: IF service is Poor OR food is Poor → tip is Low
    let rule1_activation = service_poor.max(food_poor);

    // Rule 2: IF service is Average → tip is Medium
    let rule2_activation = service_avg;

    // Rule 3: IF service is Good OR food is Good → tip is High
    let rule3_activation = service_good.max(food_good);

    // Rule 4: IF service is Good AND food is Good → tip is Very High
    let rule4_activation = service_good.min(food_good);

    // Defuzzification (weighted average)
    let tip_low = 5.0;
    let tip_medium = 13.0;
    let tip_high = 20.0;
    let tip_very_high = 27.0;

    let numerator = rule1_activation * tip_low
        + rule2_activation * tip_medium
        + rule3_activation * tip_high
        + rule4_activation * tip_very_high;

    let denominator = rule1_activation + rule2_activation + rule3_activation + rule4_activation;

    if denominator == 0.0 {
        15.0 // Default tip
    } else {
        numerator / denominator
    }
}

fn format_memberships(memberships: &[(String, f64)]) -> String {
    memberships
        .iter()
        .filter(|(_, degree)| *degree > 0.01)
        .map(|(label, degree)| format!("{}={:.2}", label, degree))
        .collect::<Vec<_>>()
        .join(", ")
}
