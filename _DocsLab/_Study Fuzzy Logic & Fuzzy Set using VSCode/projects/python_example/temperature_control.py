"""
Temperature Control System using Fuzzy Logic
A smart thermostat that decides heating/cooling based on temperature and humidity

Run with: python examples/temperature_control.py
"""

import numpy as np
import skfuzzy as fuzz
from skfuzzy import control as ctrl
import matplotlib.pyplot as plt

def create_fuzzy_controller():
    """Create the fuzzy temperature control system"""
    
    # Define input variables
    temperature = ctrl.Antecedent(np.arange(0, 46, 1), 'temperature')
    humidity = ctrl.Antecedent(np.arange(0, 101, 1), 'humidity')
    
    # Define output variable
    power = ctrl.Consequent(np.arange(0, 101, 1), 'power')

    # Temperature membership functions
    temperature['very_cold'] = fuzz.trapmf(temperature.universe, [0, 0, 10, 15])
    temperature['cold'] = fuzz.trimf(temperature.universe, [10, 15, 20])
    temperature['comfortable'] = fuzz.trimf(temperature.universe, [18, 23, 28])
    temperature['hot'] = fuzz.trimf(temperature.universe, [25, 30, 35])
    temperature['very_hot'] = fuzz.trapmf(temperature.universe, [32, 37, 45, 45])

    # Humidity membership functions
    humidity['low'] = fuzz.trapmf(humidity.universe, [0, 0, 30, 40])
    humidity['medium'] = fuzz.trimf(humidity.universe, [35, 50, 65])
    humidity['high'] = fuzz.trapmf(humidity.universe, [60, 70, 100, 100])

    # Power output membership functions (0-100% power)
    power['off'] = fuzz.trimf(power.universe, [0, 0, 20])
    power['low'] = fuzz.trimf(power.universe, [10, 30, 50])
    power['medium'] = fuzz.trimf(power.universe, [40, 60, 80])
    power['high'] = fuzz.trimf(power.universe, [70, 100, 100])

    return temperature, humidity, power


def create_fuzzy_rules(temperature, humidity, power):
    """Define the fuzzy control rules"""
    
    rules = [
        # Heating rules
        ctrl.Rule(temperature['very_cold'], power['high']),
        ctrl.Rule(temperature['cold'], power['medium']),
        
        # Comfort zone
        ctrl.Rule(temperature['comfortable'] & humidity['medium'], power['off']),
        ctrl.Rule(temperature['comfortable'] & humidity['low'], power['low']),
        ctrl.Rule(temperature['comfortable'] & humidity['high'], power['low']),
        
        # Cooling rules
        ctrl.Rule(temperature['hot'] & humidity['high'], power['high']),
        ctrl.Rule(temperature['hot'] & humidity['medium'], power['medium']),
        ctrl.Rule(temperature['hot'] & humidity['low'], power['low']),
        ctrl.Rule(temperature['very_hot'], power['high']),
        
        # Edge cases
        ctrl.Rule(temperature['cold'] & humidity['high'], power['high']),
        ctrl.Rule(temperature['hot'] & humidity['low'], power['medium']),
    ]
    
    return rules


def test_controller(controller, scenarios):
    """Test the controller with different scenarios"""
    
    print("\n" + "="*80)
    print("TESTING TEMPERATURE CONTROL SYSTEM")
    print("="*80 + "\n")
    
    print(f"{'Temp (°C)':<12} {'Humidity (%)':<15} {'Power (%)':<12} {'Action':<25} Description")
    print("-"*80)
    
    for temp, humid, description in scenarios:
        try:
            controller.input['temperature'] = temp
            controller.input['humidity'] = humid
            controller.compute()
            
            power_output = controller.output['power']
            
            # Determine action based on temperature and power
            if temp < 20 and power_output > 50:
                action = "🔥 Heating HIGH"
            elif temp < 20 and power_output > 20:
                action = "🔥 Heating MEDIUM"
            elif temp > 28 and power_output > 50:
                action = "❄️  Cooling HIGH"
            elif temp > 28 and power_output > 20:
                action = "❄️  Cooling MEDIUM"
            elif power_output < 20:
                action = "✅ Maintain"
            else:
                action = "⚙️  Adjusting"
            
            print(f"{temp:<12.1f} {humid:<15.1f} {power_output:<12.1f} {action:<25} {description}")
            
        except Exception as e:
            print(f"{temp:<12.1f} {humid:<15.1f} {'ERROR':<12} {'❌ Error':<25} {str(e)}")


def visualize_system(temperature, humidity, power):
    """Visualize the membership functions"""
    
    fig, axes = plt.subplots(3, 1, figsize=(10, 12))
    
    # Temperature
    for label in temperature.terms:
        temperature[label].view(ax=axes[0])
    axes[0].set_title('Temperature Membership Functions', fontsize=14, fontweight='bold')
    axes[0].set_xlabel('Temperature (°C)')
    axes[0].set_ylabel('Membership Degree')
    axes[0].legend(loc='upper right')
    axes[0].grid(True, alpha=0.3)
    
    # Humidity
    for label in humidity.terms:
        humidity[label].view(ax=axes[1])
    axes[1].set_title('Humidity Membership Functions', fontsize=14, fontweight='bold')
    axes[1].set_xlabel('Humidity (%)')
    axes[1].set_ylabel('Membership Degree')
    axes[1].legend(loc='upper right')
    axes[1].grid(True, alpha=0.3)
    
    # Power
    for label in power.terms:
        power[label].view(ax=axes[2])
    axes[2].set_title('Power Output Membership Functions', fontsize=14, fontweight='bold')
    axes[2].set_xlabel('Power (%)')
    axes[2].set_ylabel('Membership Degree')
    axes[2].legend(loc='upper right')
    axes[2].grid(True, alpha=0.3)
    
    plt.tight_layout()
    plt.savefig('temperature_control_system.png', dpi=150, bbox_inches='tight')
    print("\n📊 System visualization saved as 'temperature_control_system.png'")
    plt.show()


def interactive_mode(controller):
    """Interactive mode for testing custom values"""
    
    print("\n" + "="*80)
    print("INTERACTIVE MODE - Test Your Own Values!")
    print("="*80 + "\n")
    
    while True:
        try:
            temp_input = input("Enter temperature (°C) or 'q' to quit: ").strip()
            if temp_input.lower() == 'q':
                break
            
            temp = float(temp_input)
            if not 0 <= temp <= 45:
                print("⚠️  Temperature should be between 0-45°C")
                continue
            
            humid = float(input("Enter humidity (%): ").strip())
            if not 0 <= humid <= 100:
                print("⚠️  Humidity should be between 0-100%")
                continue
            
            controller.input['temperature'] = temp
            controller.input['humidity'] = humid
            controller.compute()
            
            power_output = controller.output['power']
            
            print(f"\n📊 Results:")
            print(f"   Temperature: {temp}°C")
            print(f"   Humidity:    {humid}%")
            print(f"   Power Output: {power_output:.1f}%")
            
            if temp < 20 and power_output > 50:
                print(f"   Action:      🔥 Heating HIGH")
            elif temp < 20 and power_output > 20:
                print(f"   Action:      🔥 Heating MEDIUM")
            elif temp > 28 and power_output > 50:
                print(f"   Action:      ❄️  Cooling HIGH")
            elif temp > 28 and power_output > 20:
                print(f"   Action:      ❄️  Cooling MEDIUM")
            else:
                print(f"   Action:      ✅ Maintain current state")
            print()
            
        except ValueError:
            print("❌ Invalid input! Please enter numeric values.")
        except Exception as e:
            print(f"❌ Error: {e}")


def main():
    print("╔════════════════════════════════════════════════════════╗")
    print("║     SMART THERMOSTAT - FUZZY TEMPERATURE CONTROL      ║")
    print("╚════════════════════════════════════════════════════════╝")
    
    # Create the system
    print("\n🔧 Creating fuzzy control system...")
    temperature, humidity, power = create_fuzzy_controller()
    rules = create_fuzzy_rules(temperature, humidity, power)
    
    # Create control system
    control_system = ctrl.ControlSystem(rules)
    controller = ctrl.ControlSystemSimulation(control_system)
    
    print("✅ Fuzzy controller created successfully!")
    
    # Test scenarios
    scenarios = [
        (12.0, 30.0, "Cold morning, low humidity"),
        (22.0, 50.0, "Comfortable conditions"),
        (28.0, 75.0, "Hot day, high humidity"),
        (18.0, 80.0, "Cool but very humid"),
        (35.0, 40.0, "Very hot, moderate humidity"),
        (8.0, 45.0, "Very cold morning"),
        (25.0, 55.0, "Slightly warm"),
    ]
    
    test_controller(controller, scenarios)
    
    # Visualize
    print("\n📈 Generating system visualization...")
    visualize_system(temperature, humidity, power)
    
    # Interactive mode
    interactive_mode(controller)
    
    print("\n✅ Temperature control system demo complete!")
    print("💡 Tip: Modify the membership functions and rules to customize behavior")


if __name__ == "__main__":
    main()