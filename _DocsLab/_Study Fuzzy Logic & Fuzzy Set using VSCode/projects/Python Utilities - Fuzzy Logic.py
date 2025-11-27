"""
Fuzzy Logic Utility Functions
Helpful tools for working with fuzzy systems
"""

import numpy as np
import skfuzzy as fuzz
import matplotlib.pyplot as plt
from typing import List, Tuple, Dict, Callable


class FuzzyVisualizer:
    """Visualization tools for fuzzy systems"""
    
    @staticmethod
    def plot_membership_functions(x: np.ndarray, 
                                  functions: Dict[str, np.ndarray],
                                  title: str = "Membership Functions",
                                  xlabel: str = "Universe of Discourse",
                                  ylabel: str = "Membership Degree"):
        """
        Plot multiple membership functions on the same graph
        
        Args:
            x: Universe of discourse array
            functions: Dictionary of {label: membership_values}
            title: Plot title
            xlabel: X-axis label
            ylabel: Y-axis label
        """
        plt.figure(figsize=(10, 6))
        
        colors = ['blue', 'green', 'red', 'orange', 'purple', 'brown', 'pink', 'gray']
        
        for (label, values), color in zip(functions.items(), colors):
            plt.plot(x, values, color=color, linewidth=2, label=label)
            plt.fill_between(x, 0, values, alpha=0.1, color=color)
        
        plt.title(title, fontsize=14, fontweight='bold')
        plt.xlabel(xlabel, fontsize=12)
        plt.ylabel(ylabel, fontsize=12)
        plt.legend(loc='best')
        plt.grid(True, alpha=0.3)
        plt.ylim(-0.05, 1.05)
        plt.tight_layout()
        plt.show()
    
    @staticmethod
    def plot_fuzzy_operation(x: np.ndarray,
                            set_a: np.ndarray,
                            set_b: np.ndarray,
                            operation: str,
                            result: np.ndarray):
        """
        Visualize fuzzy set operations
        
        Args:
            x: Universe array
            set_a: First fuzzy set
            set_b: Second fuzzy set
            operation: Operation name (Union, Intersection, etc.)
            result: Result of the operation
        """
        fig, axes = plt.subplots(2, 2, figsize=(12, 10))
        
        # Original sets
        axes[0, 0].plot(x, set_a, 'b-', linewidth=2, label='Set A')
        axes[0, 0].fill_between(x, 0, set_a, alpha=0.2, color='blue')
        axes[0, 0].set_title('Set A')
        axes[0, 0].grid(True, alpha=0.3)
        axes[0, 0].set_ylim(-0.05, 1.05)
        
        axes[0, 1].plot(x, set_b, 'g-', linewidth=2, label='Set B')
        axes[0, 1].fill_between(x, 0, set_b, alpha=0.2, color='green')
        axes[0, 1].set_title('Set B')
        axes[0, 1].grid(True, alpha=0.3)
        axes[0, 1].set_ylim(-0.05, 1.05)
        
        # Combined view
        axes[1, 0].plot(x, set_a, 'b-', linewidth=2, label='Set A')
        axes[1, 0].plot(x, set_b, 'g-', linewidth=2, label='Set B')
        axes[1, 0].set_title('Both Sets')
        axes[1, 0].legend()
        axes[1, 0].grid(True, alpha=0.3)
        axes[1, 0].set_ylim(-0.05, 1.05)
        
        # Result
        axes[1, 1].plot(x, result, 'r-', linewidth=2, label=operation)
        axes[1, 1].fill_between(x, 0, result, alpha=0.3, color='red')
        axes[1, 1].set_title(f'Result: {operation}')
        axes[1, 1].legend()
        axes[1, 1].grid(True, alpha=0.3)
        axes[1, 1].set_ylim(-0.05, 1.05)
        
        plt.tight_layout()
        plt.show()
    
    @staticmethod
    def plot_defuzzification(x: np.ndarray,
                            mf: np.ndarray,
                            methods: Dict[str, float]):
        """
        Visualize different defuzzification methods
        
        Args:
            x: Universe array
            mf: Membership function values
            methods: Dictionary of {method_name: crisp_value}
        """
        plt.figure(figsize=(12, 6))
        
        plt.plot(x, mf, 'b-', linewidth=2, label='Fuzzy Output')
        plt.fill_between(x, 0, mf, alpha=0.2, color='blue')
        
        colors = ['red', 'green', 'orange', 'purple', 'brown']
        for (method, value), color in zip(methods.items(), colors):
            plt.axvline(value, color=color, linestyle='--', linewidth=2, 
                       label=f'{method}: {value:.2f}')
        
        plt.title('Defuzzification Methods Comparison', fontsize=14, fontweight='bold')
        plt.xlabel('Universe of Discourse')
        plt.ylabel('Membership Degree')
        plt.legend(loc='best')
        plt.grid(True, alpha=0.3)
        plt.ylim(-0.05, 1.05)
        plt.tight_layout()
        plt.show()


class FuzzyMetrics:
    """Performance metrics for fuzzy systems"""
    
    @staticmethod
    def similarity(set_a: np.ndarray, set_b: np.ndarray) -> float:
        """
        Calculate similarity between two fuzzy sets (0 to 1)
        Uses cosine similarity
        """
        dot_product = np.dot(set_a, set_b)
        norm_a = np.linalg.norm(set_a)
        norm_b = np.linalg.norm(set_b)
        
        if norm_a == 0 or norm_b == 0:
            return 0.0
        
        return dot_product / (norm_a * norm_b)
    
    @staticmethod
    def cardinality(fuzzy_set: np.ndarray) -> float:
        """Calculate fuzzy cardinality (sum of membership values)"""
        return np.sum(fuzzy_set)
    
    @staticmethod
    def height(fuzzy_set: np.ndarray) -> float:
        """Get the maximum membership value"""
        return np.max(fuzzy_set)
    
    @staticmethod
    def support(x: np.ndarray, fuzzy_set: np.ndarray, threshold: float = 0.0) -> np.ndarray:
        """Get the support of a fuzzy set (where membership > threshold)"""
        return x[fuzzy_set > threshold]
    
    @staticmethod
    def core(x: np.ndarray, fuzzy_set: np.ndarray) -> np.ndarray:
        """Get the core of a fuzzy set (where membership = 1)"""
        return x[fuzzy_set >= 0.999]  # Use 0.999 for floating point comparison
    
    @staticmethod
    def alpha_cut(x: np.ndarray, fuzzy_set: np.ndarray, alpha: float) -> np.ndarray:
        """Get α-cut of a fuzzy set"""
        return x[fuzzy_set >= alpha]


class FuzzySetOperations:
    """Advanced fuzzy set operations"""
    
    @staticmethod
    def concentration(fuzzy_set: np.ndarray, power: float = 2.0) -> np.ndarray:
        """Apply concentration (hedge 'very')"""
        return np.power(fuzzy_set, power)
    
    @staticmethod
    def dilation(fuzzy_set: np.ndarray, root: float = 2.0) -> np.ndarray:
        """Apply dilation (hedge 'somewhat')"""
        return np.power(fuzzy_set, 1.0/root)
    
    @staticmethod
    def intensification(fuzzy_set: np.ndarray) -> np.ndarray:
        """Apply intensification (hedge 'plus')"""
        result = np.zeros_like(fuzzy_set)
        mask_low = fuzzy_set <= 0.5
        mask_high = fuzzy_set > 0.5
        
        result[mask_low] = 2 * np.power(fuzzy_set[mask_low], 2)
        result[mask_high] = 1 - 2 * np.power(1 - fuzzy_set[mask_high], 2)
        
        return result
    
    @staticmethod
    def contrast_intensification(fuzzy_set: np.ndarray) -> np.ndarray:
        """Increase contrast of fuzzy set"""
        return FuzzySetOperations.intensification(fuzzy_set)


class MembershipFunctionGenerator:
    """Generate various membership functions easily"""
    
    @staticmethod
    def auto_triangular(universe: np.ndarray, n_sets: int = 3, 
                       overlap: float = 0.25) -> List[Tuple[str, np.ndarray]]:
        """
        Automatically generate n triangular membership functions
        with specified overlap
        
        Args:
            universe: Universe of discourse
            n_sets: Number of sets to generate
            overlap: Overlap ratio (0 to 0.5)
            
        Returns:
            List of (name, membership_function) tuples
        """
        min_val = np.min(universe)
        max_val = np.max(universe)
        range_val = max_val - min_val
        
        labels = ['Very Low', 'Low', 'Medium', 'High', 'Very High']
        if n_sets <= len(labels):
            labels = labels[:n_sets]
        else:
            labels = [f'Set_{i+1}' for i in range(n_sets)]
        
        step = range_val / (n_sets - 1 + 2 * overlap * (n_sets - 1))
        width = step * (1 + 2 * overlap)
        
        result = []
        for i in range(n_sets):
            center = min_val + i * step * (1 + 2 * overlap)
            a = center - width / 2
            b = center
            c = center + width / 2
            
            # Clamp to universe bounds
            a = max(a, min_val)
            c = min(c, max_val)
            
            mf = fuzz.trimf(universe, [a, b, c])
            result.append((labels[i], mf))
        
        return result
    
    @staticmethod
    def from_data(universe: np.ndarray, data: np.ndarray, 
                  mf_type: str = 'gaussian') -> np.ndarray:
        """
        Generate membership function from data distribution
        
        Args:
            universe: Universe of discourse
            data: Data samples
            mf_type: Type of MF ('gaussian' or 'triangular')
        """
        mean = np.mean(data)
        std = np.std(data)
        
        if mf_type == 'gaussian':
            return fuzz.gaussmf(universe, mean, std)
        elif mf_type == 'triangular':
            a = mean - 2 * std
            b = mean
            c = mean + 2 * std
            return fuzz.trimf(universe, [a, b, c])
        else:
            raise ValueError(f"Unknown MF type: {mf_type}")


class FuzzyRuleMiner:
    """Tools for extracting fuzzy rules from data"""
    
    @staticmethod
    def extract_rules(X: np.ndarray, y: np.ndarray, 
                     input_vars: List[str],
                     output_var: str,
                     n_sets: int = 3) -> List[str]:
        """
        Extract fuzzy rules from input-output data
        
        Args:
            X: Input data matrix (n_samples, n_features)
            y: Output data vector
            input_vars: Names of input variables
            output_var: Name of output variable
            n_sets: Number of fuzzy sets per variable
            
        Returns:
            List of rule strings
        """
        rules = []
        n_samples, n_features = X.shape
        
        # Create fuzzy partitions for each variable
        partitions = []
        for i in range(n_features):
            x_min, x_max = X[:, i].min(), X[:, i].max()
            universe = np.linspace(x_min, x_max, 100)
            mfs = MembershipFunctionGenerator.auto_triangular(universe, n_sets)
            partitions.append((universe, mfs))
        
        # Output partition
        y_min, y_max = y.min(), y.max()
        y_universe = np.linspace(y_min, y_max, 100)
        y_mfs = MembershipFunctionGenerator.auto_triangular(y_universe, n_sets)
        
        # Extract rules from data samples
        for sample_idx in range(min(n_samples, 20)):  # Limit to 20 rules
            antecedents = []
            
            for feat_idx in range(n_features):
                value = X[sample_idx, feat_idx]
                universe, mfs = partitions[feat_idx]
                
                # Find best matching fuzzy set
                best_membership = 0
                best_label = ""
                for label, mf in mfs:
                    membership = np.interp(value, universe, mf)
                    if membership > best_membership:
                        best_membership = membership
                        best_label = label
                
                if best_membership > 0.3:  # Only include if significant
                    antecedents.append(f"{input_vars[feat_idx]} is {best_label}")
            
            # Find output fuzzy set
            output_value = y[sample_idx]
            best_membership = 0
            best_output = ""
            for label, mf in y_mfs:
                membership = np.interp(output_value, y_universe, mf)
                if membership > best_membership:
                    best_membership = membership
                    best_output = label
            
            if antecedents and best_output:
                rule = f"IF {' AND '.join(antecedents)} THEN {output_var} is {best_output}"
                if rule not in rules:
                    rules.append(rule)
        
        return rules


def test_utilities():
    """Test utility functions"""
    print("Testing Fuzzy Utilities\n")
    
    # Create test data
    x = np.arange(0, 11, 0.1)
    
    # Test membership functions
    cold = fuzz.trapmf(x, [0, 0, 3, 5])
    warm = fuzz.trimf(x, [3, 5, 7])
    hot = fuzz.trapmf(x, [5, 7, 10, 10])
    
    # Test visualizer
    print("1. Testing Visualizer...")
    visualizer = FuzzyVisualizer()
    visualizer.plot_membership_functions(
        x,
        {'Cold': cold, 'Warm': warm, 'Hot': hot},
        title="Temperature Fuzzy Sets"
    )
    
    # Test operations
    print("2. Testing Operations...")
    union = fuzz.fuzzy_or(x, cold, x, hot)[1]
    visualizer.plot_fuzzy_operation(x, cold, hot, "Union", union)
    
    # Test metrics
    print("3. Testing Metrics...")
    metrics = FuzzyMetrics()
    print(f"   Similarity (cold, warm): {metrics.similarity(cold, warm):.3f}")
    print(f"   Cardinality (cold): {metrics.cardinality(cold):.3f}")
    print(f"   Height (warm): {metrics.height(warm):.3f}")
    
    # Test auto generation
    print("4. Testing Auto-Generation...")
    generator = MembershipFunctionGenerator()
    auto_mfs = generator.auto_triangular(x, n_sets=5)
    auto_dict = {name: mf for name, mf in auto_mfs}
    visualizer.plot_membership_functions(
        x,
        auto_dict,
        title="Auto-Generated Membership Functions"
    )
    
    print("\n✅ All utility tests completed!")


if __name__ == "__main__":
    test_utilities()