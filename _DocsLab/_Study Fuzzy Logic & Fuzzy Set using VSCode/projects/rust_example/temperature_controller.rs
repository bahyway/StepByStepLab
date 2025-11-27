//! Temperature Controller Example
//!
//! A smart thermostat using fuzzy logic to control heating/cooling
//!
//! Run with: cargo run --example temperature_controller

use fuzzy_logic_study::{
    defuzzification::{create_universe, defuzzify, DefuzzMethod},
    membership::MembershipFunction,
    operations::{intersection, union},
    FuzzySet, LinguisticVariable,
};
use std::collections::HashMap;

fn main() {
    println!("╔════════════════════════════════════════════════════════╗");
    println!("║     SMART THERMOSTAT - FUZZY TEMPERATURE CONTROL      ║");
    println!("╚════════════════════════════════════════════════════════╝\n");

    // Create linguistic variables for inputs
    let mut temperature = create_temperature_variable();
    let mut humidity = create_humidity_variable();

    // Create linguistic variable for output
    let mut power = create_power_variable();

    // Test scenarios
    let scenarios = vec![
        (12.0, 30.0, "Cold morning, low humidity"),
        (22.0, 50.0, "Comfortable conditions"),
        (28.0, 75.0, "Hot day, high humidity"),
        (18.0, 80.0, "Cool but very humid"),
        (35.0, 40.0, "Very hot, moderate humidity"),
    ];

    println!("🌡️  Testing Temperature Control System\n");
    println!(
        "{:<6} {:<8} {:<45} {:<15}",
        "Temp", "Humid", "Analysis", "Action"
    );
    println!("{}", "─".repeat(85));

    for (temp, humid, description) in scenarios {
        // Fuzzify inputs
        let temp_memberships = temperature.fuzzify(temp);
        let humid_memberships = humidity.fuzzify(humid);

        // Apply fuzzy rules
        let power_level = apply_fuzzy_rules(&temp_memberships, &humid_memberships);

        // Determine action
        let action = determine_action(power_level);

        println!(
            "{:<6.1} {:<8.1} {:<45} {:<15}",
            temp, humid, description, action
        );

        // Detailed breakdown
        println!("   Temperature:");
        for (label, degree) in &temp_memberships {
            if *degree > 0.01 {
                println!("      {:<12} : {:.3}", label, degree);
            }
        }

        println!("   Humidity:");
        for (label, degree) in &humid_memberships {
            if *degree > 0.01 {
                println!("      {:<12} : {:.3}", label, degree);
            }
        }

        println!("   Power Level: {:.2}%\n", power_level * 100.0);
    }

    // Interactive mode
    println!("\n{'=':.>60}", "");
    println!("💡 Try Your Own Values!");
    println!("{'=':.>60}\n", "");

    use std::io::{self, Write};

    loop {
        print!("Enter temperature (°C) or 'q' to quit: ");
        io::stdout().flush().unwrap();

        let mut input = String::new();
        io::stdin().read_line(&mut input).unwrap();

        if input.trim() == "q" {
            break;
        }

        let temp: f64 = match input.trim().parse() {
            Ok(val) => val,
            Err(_) => {
                println!("❌ Invalid input!");
                continue;
            }
        };

        print!("Enter humidity (%): ");
        io::stdout().flush().unwrap();

        input.clear();
        io::stdin().read_line(&mut input).unwrap();

        let humid: f64 = match input.trim().parse() {
            Ok(val) => val,
            Err(_) => {
                println!("❌ Invalid input!");
                continue;
            }
        };

        let temp_memberships = temperature.fuzzify(temp);
        let humid_memberships = humidity.fuzzify(humid);
        let power_level = apply_fuzzy_rules(&temp_memberships, &humid_memberships);
        let action = determine_action(power_level);

        println!("\n📊 Results:");
        println!(
            "   Temperature: {}°C → {}",
            temp,
            temperature.classify(temp).unwrap_or("unknown".to_string())
        );
        println!(
            "   Humidity:    {}% → {}",
            humid,
            humidity.classify(humid).unwrap_or("unknown".to_string())
        );
        println!("   Power Level: {:.1}%", power_level * 100.0);
        println!("   Action:      {}\n", action);
    }

    println!("\n✅ Temperature control simulation complete!");
}

fn create_temperature_variable() -> LinguisticVariable {
    let mut temp = LinguisticVariable::new("Temperature", (0.0, 45.0));

    let very_cold = MembershipFunction::Trapezoidal {
        a: 0.0,
        b: 0.0,
        c: 10.0,
        d: 15.0,
    };

    let cold = MembershipFunction::Triangular {
        a: 10.0,
        b: 15.0,
        c: 20.0,
    };

    let comfortable = MembershipFunction::Triangular {
        a: 18.0,
        b: 23.0,
        c: 28.0,
    };

    let hot = MembershipFunction::Triangular {
        a: 25.0,
        b: 30.0,
        c: 35.0,
    };

    let very_hot = MembershipFunction::Trapezoidal {
        a: 32.0,
        b: 37.0,
        c: 45.0,
        d: 45.0,
    };

    temp.add_set(FuzzySet::new(
        "Very Cold",
        Box::new(move |x| very_cold.evaluate(x)),
    ));
    temp.add_set(FuzzySet::new("Cold", Box::new(move |x| cold.evaluate(x))));
    temp.add_set(FuzzySet::new(
        "Comfortable",
        Box::new(move |x| comfortable.evaluate(x)),
    ));
    temp.add_set(FuzzySet::new("Hot", Box::new(move |x| hot.evaluate(x))));
    temp.add_set(FuzzySet::new(
        "Very Hot",
        Box::new(move |x| very_hot.evaluate(x)),
    ));

    temp
}

fn create_humidity_variable() -> LinguisticVariable {
    let mut humid = LinguisticVariable::new("Humidity", (0.0, 100.0));

    let low = MembershipFunction::Trapezoidal {
        a: 0.0,
        b: 0.0,
        c: 30.0,
        d: 40.0,
    };

    let medium = MembershipFunction::Triangular {
        a: 35.0,
        b: 50.0,
        c: 65.0,
    };

    let high = MembershipFunction::Trapezoidal {
        a: 60.0,
        b: 70.0,
        c: 100.0,
        d: 100.0,
    };

    humid.add_set(FuzzySet::new("Low", Box::new(move |x| low.evaluate(x))));
    humid.add_set(FuzzySet::new(
        "Medium",
        Box::new(move |x| medium.evaluate(x)),
    ));
    humid.add_set(FuzzySet::new("High", Box::new(move |x| high.evaluate(x))));

    humid
}

fn create_power_variable() -> LinguisticVariable {
    let mut power = LinguisticVariable::new("Power", (0.0, 1.0));

    let off = MembershipFunction::Triangular {
        a: 0.0,
        b: 0.0,
        c: 0.2,
    };
    let low = MembershipFunction::Triangular {
        a: 0.1,
        b: 0.3,
        c: 0.5,
    };
    let medium = MembershipFunction::Triangular {
        a: 0.4,
        b: 0.6,
        c: 0.8,
    };
    let high = MembershipFunction::Triangular {
        a: 0.7,
        b: 1.0,
        c: 1.0,
    };

    power.add_set(FuzzySet::new("Off", Box::new(move |x| off.evaluate(x))));
    power.add_set(FuzzySet::new("Low", Box::new(move |x| low.evaluate(x))));
    power.add_set(FuzzySet::new(
        "Medium",
        Box::new(move |x| medium.evaluate(x)),
    ));
    power.add_set(FuzzySet::new("High", Box::new(move |x| high.evaluate(x))));

    power
}

fn apply_fuzzy_rules(
    temp_memberships: &[(String, f64)],
    humid_memberships: &[(String, f64)],
) -> f64 {
    // Helper to get membership value
    let get_membership = |memberships: &[(String, f64)], label: &str| -> f64 {
        memberships
            .iter()
            .find(|(l, _)| l == label)
            .map(|(_, v)| *v)
            .unwrap_or(0.0)
    };

    // Get individual memberships
    let very_cold = get_membership(temp_memberships, "Very Cold");
    let cold = get_membership(temp_memberships, "Cold");
    let comfortable = get_membership(temp_memberships, "Comfortable");
    let hot = get_membership(temp_memberships, "Hot");
    let very_hot = get_membership(temp_memberships, "Very Hot");

    let humid_low = get_membership(humid_memberships, "Low");
    let humid_med = get_membership(humid_memberships, "Medium");
    let humid_high = get_membership(humid_memberships, "High");

    // Fuzzy rules (simplified Mamdani inference)
    // IF Very Cold THEN Heat High
    // IF Cold THEN Heat Medium
    // IF Comfortable THEN Off
    // IF Hot AND Humid High THEN Cool High
    // IF Very Hot THEN Cool High

    let heat_high = very_cold;
    let heat_medium = cold;
    let maintain = comfortable;
    let cool_medium = hot.min(humid_high);
    let cool_high = very_hot.max(hot.min(humid_high));

    // Aggregate (simplified - in real system would use proper defuzzification)
    // Positive = heating needed, Negative = cooling needed
    let heating_power = heat_high.max(heat_medium * 0.6);
    let cooling_power = cool_high.max(cool_medium * 0.6);

    // Return power level (0-1 scale)
    // Negative values = cooling, Positive = heating
    if heating_power > cooling_power {
        heating_power
    } else if cooling_power > heating_power {
        cooling_power
    } else {
        0.0
    }
}

fn determine_action(power_level: f64) -> String {
    if power_level < 0.2 {
        "✅ Maintain".to_string()
    } else if power_level < 0.5 {
        "🔥 Heat Low".to_string()
    } else if power_level < 0.8 {
        "🔥 Heat Medium".to_string()
    } else {
        "🔥🔥 Heat High".to_string()
    }
}
