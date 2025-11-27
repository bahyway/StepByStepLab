"""
Fuzzy Logic Study - Main Examples
Author: Your Name
Date: 2025-10-09

This script demonstrates fundamental fuzzy logic concepts using scikit-fuzzy.
"""

import numpy as np
import skfuzzy as fuzz
from skfuzzy import control as ctrl
import matplotlib.pyplot as plt

def lesson_1_membership_functions():
    """
    Lesson 1: Understanding Membership Functions
    
    Membership functions map crisp values to fuzzy degrees [0, 1]
    Common types: Triangular, Trapezoidal, Gaussian
    """
    print("=" * 60)
    print("LESSON 1: Membership Functions")
    print("=" * 60)
    
    # Create universe of discourse (range of values)
    x = np.arange(0, 11, 0.1)
    
    # Create different types of membership functions
    mf_triangular = fuzz.trimf(x, [0, 5, 10])
    mf_trapezoidal = fuzz.trapmf(x, [0, 2, 8, 10])
    mf_gaussian = fuzz.gaussmf(x, 5, 1.5)
    mf_sigmoid = fuzz.sigmf(x, 5, 1)
    
    # Visualize
    fig, axes = plt.subplots(2, 2, figsize=(12, 8))
    fig.suptitle('Common Membership Functions', fontsize=16)
    
    axes[0, 0].plot(x, mf_triangular, 'b', linewidth=2)
    axes[0, 0].set_title('Triangular MF')
    axes[0, 0].set_ylabel('Membership Degree')
    axes[0, 0].grid(True)
    
    axes[0, 1].plot(x, mf_trapezoidal, 'g', linewidth=2)
    axes[0, 1].set_title('Trapezoidal MF')
    axes[0, 1].grid(True)
    
    axes[1, 0].plot(x, mf_gaussian, 'r', linewidth=2)
    axes[1, 0].set_title('Gaussian MF')
    axes[1, 0].set_ylabel('Membership Degree')
    axes[1, 0].set_xlabel('Universe of Discourse')
    axes[1, 0].grid(True)
    
    axes[1, 1].plot(x, mf_sigmoid, 'm', linewidth=2)
    axes[1, 1].set_title('Sigmoid MF')
    axes[1, 1].set_xlabel('Universe of Discourse')
    axes[1, 1].grid(True)
    
    plt.tight_layout()
    plt.show()
    
    print("\n✓ Membership functions visualized!")
    print("Notice how each function maps crisp values to fuzzy degrees.\n")


def lesson_2_fuzzy_operations():
    """
    Lesson 2: Fuzzy Set Operations
    
    Basic operations: Union (OR), Intersection (AND), Complement (NOT)
    """
    print("=" * 60)
    print("LESSON 2: Fuzzy Set Operations")
    print("=" * 60)
    
    x = np.arange(0, 11, 0.1)
    
    # Two fuzzy sets
    young = fuzz.trapmf(x, [0, 0, 25, 35])
    middle_aged = fuzz.trapmf(x, [30, 40, 50, 60])
    
    # Operations
    union = fuzz.fuzzy_or(x, young, x, middle_aged)[1]
    intersection = fuzz.fuzzy_and(x, young, x, middle_aged)[1]
    complement_young = fuzz.fuzzy_not(young)
    
    # Visualize
    fig, axes = plt.subplots(2, 2, figsize=(12, 8))
    fig.suptitle('Fuzzy Set Operations', fontsize=16)
    
    # Original sets
    axes[0, 0].plot(x, young, 'b', label='Young', linewidth=2)
    axes[0, 0].plot(x, middle_aged, 'r', label='Middle-aged', linewidth=2)
    axes[0, 0].set_title('Original Fuzzy Sets')
    axes[0, 0].legend()
    axes[0, 0].grid(True)
    
    # Union
    axes[0, 1].fill_between(x, 0, union, alpha=0.3, color='purple')
    axes[0, 1].plot(x, union, 'purple', linewidth=2)
    axes[0, 1].set_title('Union (OR) - Young OR Middle-aged')
    axes[0, 1].grid(True)
    
    # Intersection
    axes[1, 0].fill_between(x, 0, intersection, alpha=0.3, color='green')
    axes[1, 0].plot(x, intersection, 'green', linewidth=2)
    axes[1, 0].set_title('Intersection (AND) - Young AND Middle-aged')
    axes[1, 0].set_xlabel('Age')
    axes[1, 0].grid(True)
    
    # Complement
    axes[1, 1].fill_between(x, 0, complement_young, alpha=0.3, color='orange')
    axes[1, 1].plot(x, complement_young, 'orange', linewidth=2)
    axes[1, 1].set_title('Complement (NOT) - NOT Young')
    axes[1, 1].set_xlabel('Age')
    axes[1, 1].grid(True)
    
    plt.tight_layout()
    plt.show()
    
    print("\n✓ Fuzzy operations demonstrated!")
    print("Union: Maximum values | Intersection: Minimum values | Complement: 1 - value\n")


def lesson_3_fuzzy_inference_system():
    """
    Lesson 3: Building a Fuzzy Inference System (Tipping Problem)
    
    Classic example: Determine tip amount based on service quality and food quality
    """
    print("=" * 60)
    print("LESSON 3: Fuzzy Inference System (Tipping)")
    print("=" * 60)
    
    # Define fuzzy variables
    quality = ctrl.Antecedent(np.arange(0, 11, 1), 'quality')
    service = ctrl.Antecedent(np.arange(0, 11, 1), 'service')
    tip = ctrl.Consequent(np.arange(0, 26, 1), 'tip')
    
    # Auto-membership function population
    quality.automf(3)  # poor, average, good
    service.automf(3)
    
    # Custom membership functions for tip
    tip['low'] = fuzz.trimf(tip.universe, [0, 0, 13])
    tip['medium'] = fuzz.trimf(tip.universe, [0, 13, 25])
    tip['high'] = fuzz.trimf(tip.universe, [13, 25, 25])
    
    # Define fuzzy rules
    rule1 = ctrl.Rule(quality['poor'] | service['poor'], tip['low'])
    rule2 = ctrl.Rule(service['average'], tip['medium'])
    rule3 = ctrl.Rule(service['good'] | quality['good'], tip['high'])
    
    # Create control system
    tipping_ctrl = ctrl.ControlSystem([rule1, rule2, rule3])
    tipping = ctrl.ControlSystemSimulation(tipping_ctrl)
    
    # Test the system
    test_cases = [
        (6.5, 9.8, "Good food, excellent service"),
        (3.2, 4.1, "Poor food, poor service"),
        (5.0, 6.5, "Average food, decent service")
    ]
    
    print("\nTesting Fuzzy Inference System:\n")
    for q, s, description in test_cases:
        tipping.input['quality'] = q
        tipping.input['service'] = s
        tipping.compute()
        
        print(f"Scenario: {description}")
        print(f"  Quality: {q}/10, Service: {s}/10")
        print(f"  → Recommended Tip: {tipping.output['tip']:.1f}%")
        print()
    
    # Visualize membership functions
    quality.view()
    service.view()
    tip.view()
    plt.show()
    
    print("✓ Fuzzy Inference System created and tested!")
    print("This demonstrates how fuzzy logic handles ambiguous inputs.\n")


def lesson_4_defuzzification():
    """
    Lesson 4: Defuzzification Methods
    
    Converting fuzzy output back to crisp values
    """
    print("=" * 60)
    print("LESSON 4: Defuzzification Methods")
    print("=" * 60)
    
    x = np.arange(0, 21, 1)
    
    # Create a fuzzy output
    mf_output = fuzz.trimf(x, [5, 10, 15])
    
    # Different defuzzification methods
    centroid = fuzz.defuzz(x, mf_output, 'centroid')
    bisector = fuzz.defuzz(x, mf_output, 'bisector')
    mom = fuzz.defuzz(x, mf_output, 'mom')  # Mean of Maximum
    som = fuzz.defuzz(x, mf_output, 'som')  # Smallest of Maximum
    lom = fuzz.defuzz(x, mf_output, 'lom')  # Largest of Maximum
    
    # Visualize
    plt.figure(figsize=(10, 6))
    plt.plot(x, mf_output, 'b', linewidth=2, label='Fuzzy Output')
    plt.axvline(centroid, color='r', linestyle='--', linewidth=2, label=f'Centroid: {centroid:.2f}')
    plt.axvline(bisector, color='g', linestyle='--', linewidth=2, label=f'Bisector: {bisector:.2f}')
    plt.axvline(mom, color='orange', linestyle='--', linewidth=2, label=f'MOM: {mom:.2f}')
    plt.fill_between(x, 0, mf_output, alpha=0.2)
    
    plt.title('Defuzzification Methods', fontsize=14)
    plt.xlabel('Output Universe')
    plt.ylabel('Membership Degree')
    plt.legend()
    plt.grid(True)
    plt.show()
    
    print("\nDefuzzification Results:")
    print(f"  Centroid (Center of Gravity): {centroid:.2f}")
    print(f"  Bisector (Area Division):     {bisector:.2f}")
    print(f"  Mean of Maximum:              {mom:.2f}")
    print(f"  Smallest of Maximum:          {som:.2f}")
    print(f"  Largest of Maximum:           {lom:.2f}")
    print("\n✓ Most common method: Centroid (weighted average)\n")


def main():
    """Run all lessons"""
    print("\n" + "=" * 60)
    print("FUZZY LOGIC & FUZZY SETS - INTERACTIVE STUDY")
    print("=" * 60 + "\n")
    
    lessons = [
        ("1", "Membership Functions", lesson_1_membership_functions),
        ("2", "Fuzzy Operations", lesson_2_fuzzy_operations),
        ("3", "Fuzzy Inference System", lesson_3_fuzzy_inference_system),
        ("4", "Defuzzification", lesson_4_defuzzification)
    ]
    
    print("Available Lessons:")
    for num, title, _ in lessons:
        print(f"  {num}. {title}")
    print("  5. Run All Lessons")
    print("  0. Exit")
    
    choice = input("\nSelect lesson (0-5): ").strip()
    
    if choice == "0":
        print("Happy learning! 🎓")
        return
    elif choice == "5":
        for _, _, lesson_func in lessons:
            lesson_func()
            input("\nPress Enter to continue to next lesson...")
    elif choice in ["1", "2", "3", "4"]:
        lessons[int(choice) - 1][2]()
    else:
        print("Invalid choice!")


if __name__ == "__main__":
    main()