"""
Performance Comparison Tool - Python vs Rust
Compare execution time, memory usage, and accuracy between implementations

Run with: python performance_comparison.py
"""

import time
import numpy as np
import skfuzzy as fuzz
from skfuzzy import control as ctrl
import matplotlib.pyplot as plt
from typing import Dict, List, Tuple
import subprocess
import json


class PerformanceBenchmark:
    """Benchmark fuzzy logic operations"""
    
    def __init__(self):
        self.results = {}
    
    def benchmark_membership_functions(self, n_iterations: int = 10000):
        """Benchmark membership function evaluation"""
        print("Benchmarking Membership Functions...")
        
        x = np.arange(0, 11, 0.1)
        
        # Triangular
        start = time.perf_counter()
        for _ in range(n_iterations):
            _ = fuzz.trimf(x, [0, 5, 10])
        triangular_time = time.perf_counter() - start
        
        # Trapezoidal
        start = time.perf_counter()
        for _ in range(n_iterations):
            _ = fuzz.trapmf(x, [0, 2, 8, 10])
        trapezoidal_time = time.perf_counter() - start
        
        # Gaussian
        start = time.perf_counter()
        for _ in range(n_iterations):
            _ = fuzz.gaussmf(x, 5, 1.5)
        gaussian_time = time.perf_counter() - start
        
        self.results['membership_functions'] = {
            'triangular': triangular_time,
            'trapezoidal': trapezoidal_time,
            'gaussian': gaussian_time,
            'iterations': n_iterations
        }
        
        print(f"  Triangular:   {triangular_time*1000:.2f} ms")
        print(f"  Trapezoidal:  {trapezoidal_time*1000:.2f} ms")
        print(f"  Gaussian:     {gaussian_time*1000:.2f} ms")
        print()
    
    def benchmark_fuzzy_operations(self, n_iterations: int = 1000):
        """Benchmark fuzzy set operations"""
        print("Benchmarking Fuzzy Operations...")
        
        x = np.arange(0, 11, 0.1)
        set_a = fuzz.trimf(x, [0, 5, 10])
        set_b = fuzz.trimf(x, [5, 10, 15])
        
        # Union
        start = time.perf_counter()
        for _ in range(n_iterations):
            _ = fuzz.fuzzy_or(x, set_a, x, set_b)
        union_time = time.perf_counter() - start
        
        # Intersection
        start = time.perf_counter()
        for _ in range(n_iterations):
            _ = fuzz.fuzzy_and(x, set_a, x, set_b)
        intersection_time = time.perf_counter() - start
        
        # Complement
        start = time.perf_counter()
        for _ in range(n_iterations):
            _ = fuzz.fuzzy_not(set_a)
        complement_time = time.perf_counter() - start
        
        self.results['fuzzy_operations'] = {
            'union': union_time,
            'intersection': intersection_time,
            'complement': complement_time,
            'iterations': n_iterations
        }
        
        print(f"  Union:        {union_time*1000:.2f} ms")
        print(f"  Intersection: {intersection_time*1000:.2f} ms")
        print(f"  Complement:   {complement_time*1000:.2f} ms")
        print()
    
    def benchmark_defuzzification(self, n_iterations: int = 1000):
        """Benchmark defuzzification methods"""
        print("Benchmarking Defuzzification...")
        
        x = np.arange(0, 11, 0.1)
        mf = fuzz.trimf(x, [0, 5, 10])
        
        methods = ['centroid', 'bisector', 'mom', 'som', 'lom']
        times = {}
        
        for method in methods:
            start = time.perf_counter()
            for _ in range(n_iterations):
                _ = fuzz.defuzz(x, mf, method)
            times[method] = time.perf_counter() - start
            print(f"  {method:10s}: {times[method]*1000:.2f} ms")
        
        self.results['defuzzification'] = {
            **times,
            'iterations': n_iterations
        }
        print()
    
    def benchmark_inference_system(self, n_iterations: int = 100):
        """Benchmark complete inference system"""
        print("Benchmarking Fuzzy Inference System...")
        
        # Create simple FIS
        temperature = ctrl.Antecedent(np.arange(0, 41, 1), 'temperature')
        power = ctrl.Consequent(np.arange(0, 101, 1), 'power')
        
        temperature['cold'] = fuzz.trimf(temperature.universe, [0, 0, 20])
        temperature['warm'] = fuzz.trimf(temperature.universe, [15, 25, 35])
        temperature['hot'] = fuzz.trimf(temperature.universe, [30, 40, 40])
        
        power['low'] = fuzz.trimf(power.universe, [0, 0, 50])
        power['medium'] = fuzz.trimf(power.universe, [25, 50, 75])
        power['high'] = fuzz.trimf(power.universe, [50, 100, 100])
        
        rule1 = ctrl.Rule(temperature['cold'], power['high'])
        rule2 = ctrl.Rule(temperature['warm'], power['medium'])
        rule3 = ctrl.Rule(temperature['hot'], power['low'])
        
        control_system = ctrl.ControlSystem([rule1, rule2, rule3])
        controller = ctrl.ControlSystemSimulation(control_system)
        
        # Benchmark
        test_values = np.random.uniform(0, 40, n_iterations)
        
        start = time.perf_counter()
        for temp_val in test_values:
            controller.input['temperature'] = temp_val
            controller.compute()
            _ = controller.output['power']
        inference_time = time.perf_counter() - start
        
        self.results['inference_system'] = {
            'total_time': inference_time,
            'avg_time_per_inference': inference_time / n_iterations,
            'iterations': n_iterations
        }
        
        print(f"  Total time:    {inference_time*1000:.2f} ms")
        print(f"  Avg per call:  {(inference_time/n_iterations)*1000:.2f} ms")
        print(f"  Throughput:    {n_iterations/inference_time:.0f} inferences/sec")
        print()
    
    def visualize_results(self):
        """Create visualization of benchmark results"""
        fig, axes = plt.subplots(2, 2, figsize=(14, 10))
        
        # Membership Functions
        if 'membership_functions' in self.results:
            data = self.results['membership_functions']
            labels = ['Triangular', 'Trapezoidal', 'Gaussian']
            values = [data['triangular'], data['trapezoidal'], data['gaussian']]
            values = [v * 1000 for v in values]  # Convert to ms
            
            axes[0, 0].bar(labels, values, color=['blue', 'green', 'red'])
            axes[0, 0].set_ylabel('Time (ms)')
            axes[0, 0].set_title('Membership Function Performance')
            axes[0, 0].grid(True, alpha=0.3)
        
        # Fuzzy Operations
        if 'fuzzy_operations' in self.results:
            data = self.results['fuzzy_operations']
            labels = ['Union', 'Intersection', 'Complement']
            values = [data['union'], data['intersection'], data['complement']]
            values = [v * 1000 for v in values]
            
            axes[0, 1].bar(labels, values, color=['purple', 'orange', 'brown'])
            axes[0, 1].set_ylabel('Time (ms)')
            axes[0, 1].set_title('Fuzzy Operations Performance')
            axes[0, 1].grid(True, alpha=0.3)
        
        # Defuzzification
        if 'defuzzification' in self.results:
            data = self.results['defuzzification']
            labels = ['Centroid', 'Bisector', 'MOM', 'SOM', 'LOM']
            values = [data['centroid'], data['bisector'], data['mom'], 
                     data['som'], data['lom']]
            values = [v * 1000 for v in values]
            
            axes[1, 0].bar(labels, values, color=['cyan', 'magenta', 'yellow', 
                                                   'lime', 'pink'])
            axes[1, 0].set_ylabel('Time (ms)')
            axes[1, 0].set_title('Defuzzification Methods Performance')
            axes[1, 0].grid(True, alpha=0.3)
            axes[1, 0].tick_params(axis='x', rotation=45)
        
        # Inference System
        if 'inference_system' in self.results:
            data = self.results['inference_system']
            
            info_text = f"""
Fuzzy Inference System Benchmark

Total Time: {data['total_time']*1000:.2f} ms
Iterations: {data['iterations']}
Avg Time: {data['avg_time_per_inference']*1000:.2f} ms
Throughput: {data['iterations']/data['total_time']:.0f} calls/sec
            """
            
            axes[1, 1].text(0.1, 0.5, info_text, 
                          fontsize=12, family='monospace',
                          verticalalignment='center')
            axes[1, 1].set_title('Inference System Performance')
            axes[1, 1].axis('off')
        
        plt.tight_layout()
        plt.savefig('performance_benchmark.png', dpi=150, bbox_inches='tight')
        print("📊 Visualization saved as 'performance_benchmark.png'")
        plt.show()
    
    def generate_report(self):
        """Generate text report"""
        print("\n" + "="*60)
        print("PERFORMANCE BENCHMARK REPORT")
        print("="*60 + "\n")
        
        for category, data in self.results.items():
            print(f"{category.upper().replace('_', ' ')}:")
            for key, value in data.items():
                if key != 'iterations':
                    if isinstance(value, float):
                        print(f"  {key:20s}: {value*1000:8.2f} ms")
            print()


class RustComparison:
    """Compare Python performance with Rust"""
    
    @staticmethod
    def run_rust_benchmark():
        """Run Rust benchmarks if available"""
        try:
            print("Running Rust benchmarks...")
            result = subprocess.run(
                ['cargo', 'test', '--release', 'bench_'],
                cwd='../rust-fuzzy-logic',
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if result.returncode == 0:
                print("✅ Rust benchmarks completed")
                print(result.stdout)
            else:
                print("⚠️  Rust benchmarks not available")
                print(result.stderr)
        
        except FileNotFoundError:
            print("⚠️  Rust project not found or cargo not installed")
        except Exception as e:
            print(f"⚠️  Error running Rust benchmarks: {e}")


def main():
    """Run complete performance benchmark suite"""
    print("╔════════════════════════════════════════════════════════╗")
    print("║      FUZZY LOGIC PERFORMANCE BENCHMARK SUITE          ║")
    print("╚════════════════════════════════════════════════════════╝\n")
    
    benchmark = PerformanceBenchmark()
    
    # Run all benchmarks
    benchmark.benchmark_membership_functions(n_iterations=10000)
    benchmark.benchmark_fuzzy_operations(n_iterations=1000)
    benchmark.benchmark_defuzzification(n_iterations=1000)
    benchmark.benchmark_inference_system(n_iterations=100)
    
    # Generate visualizations and report
    benchmark.visualize_results()
    benchmark.generate_report()
    
    # Try to run Rust comparison
    print("\n" + "="*60)
    print("RUST COMPARISON")
    print("="*60 + "\n")
    RustComparison.run_rust_benchmark()
    
    print("\n✅ Performance benchmark complete!")
    print("\n💡 Tips for improvement:")
    print("   - Use Rust for performance-critical applications")
    print("   - Cache membership function evaluations")
    print("   - Reduce universe resolution if possible")
    print("   - Use simpler membership functions")
    print("   - Optimize rule base")


if __name__ == "__main__":
    main()