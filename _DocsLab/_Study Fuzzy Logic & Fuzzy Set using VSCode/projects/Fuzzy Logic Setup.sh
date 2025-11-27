#!/bin/bash
# Fuzzy Logic Study Environment Setup Script
# Run with: bash setup.sh

set -e  # Exit on error

echo "╔════════════════════════════════════════════════════════╗"
echo "║   FUZZY LOGIC STUDY ENVIRONMENT SETUP                 ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_step() {
    echo -e "${BLUE}➜${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

# Check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

echo "🔍 Checking prerequisites..."
echo ""

# Check Python
if command_exists python3; then
    PYTHON_VERSION=$(python3 --version | awk '{print $2}')
    print_success "Python $PYTHON_VERSION found"
    HAS_PYTHON=true
else
    print_warning "Python 3 not found"
    HAS_PYTHON=false
fi

# Check Rust
if command_exists cargo; then
    RUST_VERSION=$(cargo --version | awk '{print $2}')
    print_success "Rust $RUST_VERSION found"
    HAS_RUST=true
else
    print_warning "Rust not found"
    HAS_RUST=false
fi

# Check VSCode
if command_exists code; then
    print_success "VSCode found"
    HAS_VSCODE=true
else
    print_warning "VSCode CLI not found"
    HAS_VSCODE=false
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Ask user what to set up
echo "What would you like to set up?"
echo "  1) Python environment only"
echo "  2) Rust environment only"
echo "  3) Both Python and Rust"
echo ""
read -p "Enter choice (1-3): " SETUP_CHOICE

# Create workspace directory
WORKSPACE_DIR="fuzzy-logic-workspace"
print_step "Creating workspace directory: $WORKSPACE_DIR"
mkdir -p "$WORKSPACE_DIR"
cd "$WORKSPACE_DIR"
print_success "Workspace created"
echo ""

# Setup Python
if [ "$SETUP_CHOICE" = "1" ] || [ "$SETUP_CHOICE" = "3" ]; then
    if [ "$HAS_PYTHON" = true ]; then
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "🐍 Setting up Python environment"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo ""
        
        # Create Python project structure
        print_step "Creating Python project structure..."
        mkdir -p python-fuzzy-logic/{src,examples,notebooks,tests,.vscode}
        cd python-fuzzy-logic
        
        # Create virtual environment
        print_step "Creating virtual environment..."
        python3 -m venv fuzzy_env
        print_success "Virtual environment created"
        
        # Activate virtual environment
        source fuzzy_env/bin/activate
        
        # Upgrade pip
        print_step "Upgrading pip..."
        pip install --upgrade pip --quiet
        print_success "pip upgraded"
        
        # Create requirements.txt
        print_step "Creating requirements.txt..."
        cat > requirements.txt << 'EOF'
# Core fuzzy logic library
scikit-fuzzy==0.4.2

# Numerical computing
numpy==1.24.3

# Visualization
matplotlib==3.7.1
seaborn==0.12.2

# Interactive notebooks
jupyter==1.0.0
ipykernel==6.23.1
ipywidgets==8.0.6

# Data manipulation
pandas==2.0.2
scipy==1.10.1

# Development tools
black==23.3.0
pylint==2.17.4
pytest==7.3.1
EOF
        print_success "requirements.txt created"
        
        # Install dependencies
        print_step "Installing Python dependencies (this may take a moment)..."
        pip install -r requirements.txt --quiet
        print_success "Dependencies installed"
        
        # Create basic project files
        print_step "Creating project files..."
        
        # Create __init__.py files
        touch src/__init__.py
        touch tests/__init__.py
        
        # Create a simple README
        cat > README.md << 'EOF'
# Fuzzy Logic Study - Python

## Quick Start

```bash
# Activate virtual environment
source fuzzy_env/bin/activate

# Run main program
python src/main.py

# Run Jupyter notebook
jupyter notebook notebooks/

# Run examples
python examples/temperature_control.py
```

## Project Structure

- `src/` - Main source code
- `examples/` - Example applications
- `notebooks/` - Jupyter notebooks
- `tests/` - Unit tests
EOF
        
        print_success "Python project setup complete"
        
        # Install VSCode extensions if VSCode is available
        if [ "$HAS_VSCODE" = true ]; then
            print_step "Installing VSCode Python extensions..."
            code --install-extension ms-python.python 2>/dev/null || true
            code --install-extension ms-toolsai.jupyter 2>/dev/null || true
            code --install-extension ms-python.black-formatter 2>/dev/null || true
            print_success "VSCode extensions installed"
        fi
        
        cd ..
        echo ""
    else
        print_error "Python 3 is required but not found. Please install Python 3.8 or higher."
        echo "Visit: https://www.python.org/downloads/"
    fi
fi

# Setup Rust
if [ "$SETUP_CHOICE" = "2" ] || [ "$SETUP_CHOICE" = "3" ]; then
    if [ "$HAS_RUST" = true ]; then
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "🦀 Setting up Rust environment"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo ""
        
        # Create Rust project
        print_step "Creating Rust project..."
        cargo new rust-fuzzy-logic --quiet
        cd rust-fuzzy-logic
        
        # Create project structure
        print_step "Creating project structure..."
        mkdir -p examples tests .vscode
        
        # Update Cargo.toml
        print_step "Configuring Cargo.toml..."
        cat > Cargo.toml << 'EOF'
[package]
name = "fuzzy-logic-study"
version = "0.1.0"
edition = "2021"

[dependencies]
num-traits = "0.2"

[[example]]
name = "temperature_controller"
path = "examples/temperature_controller.rs"

[[example]]
name = "tipping_system"
path = "examples/tipping_system.rs"
EOF
        print_success "Cargo.toml configured"
        
        # Build project to download dependencies
        print_step "Building project and downloading dependencies..."
        cargo build --quiet
        print_success "Project built successfully"
        
        # Run tests
        print_step "Running tests..."
        cargo test --quiet
        print_success "Tests passed"
        
        # Create README
        cat > README.md << 'EOF'
# Fuzzy Logic Study - Rust

## Quick Start

```bash
# Run main program
cargo run

# Run examples
cargo run --example temperature_controller
cargo run --example tipping_system

# Run tests
cargo test

# Build optimized version
cargo build --release

# Generate documentation
cargo doc --open
```

## Project Structure

- `src/` - Main library code
- `examples/` - Example applications
- `tests/` - Unit tests
EOF
        
        print_success "Rust project setup complete"
        
        # Install VSCode extensions if VSCode is available
        if [ "$HAS_VSCODE" = true ]; then
            print_step "Installing VSCode Rust extensions..."
            code --install-extension rust-lang.rust-analyzer 2>/dev/null || true
            code --install-extension vadimcn.vscode-lldb 2>/dev/null || true
            code --install-extension serayuzgur.crates 2>/dev/null || true
            code --install-extension tamasfe.even-better-toml 2>/dev/null || true
            print_success "VSCode extensions installed"
        fi
        
        cd ..
        echo ""
    else
        print_error "Rust is required but not found. Please install Rust."
        echo "Visit: https://rustup.rs/"
        echo "Run: curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh"
    fi
fi

# Create workspace settings
if [ "$HAS_VSCODE" = true ]; then
    print_step "Creating VSCode workspace file..."
    cat > fuzzy-logic.code-workspace << 'EOF'
{
    "folders": [
        {
            "path": "python-fuzzy-logic",
            "name": "Python - Fuzzy Logic"
        },
        {
            "path": "rust-fuzzy-logic",
            "name": "Rust - Fuzzy Logic"
        }
    ],
    "settings": {
        "files.autoSave": "afterDelay",
        "files.autoSaveDelay": 1000
    }
}
EOF
    print_success "VSCode workspace created"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📂 Your workspace is in: $(pwd)"
echo ""

if [ "$SETUP_CHOICE" = "1" ] || [ "$SETUP_CHOICE" = "3" ]; then
    echo "🐍 Python project:"
    echo "   cd python-fuzzy-logic"
    echo "   source fuzzy_env/bin/activate"
    echo "   python src/main.py"
    echo ""
fi

if [ "$SETUP_CHOICE" = "2" ] || [ "$SETUP_CHOICE" = "3" ]; then
    echo "🦀 Rust project:"
    echo "   cd rust-fuzzy-logic"
    echo "   cargo run"
    echo ""
fi

if [ "$HAS_VSCODE" = true ]; then
    echo "💡 To open in VSCode:"
    echo "   code fuzzy-logic.code-workspace"
    echo ""
fi

echo "📚 Next steps:"
echo "   1. Copy the source files from the artifacts into the appropriate directories"
echo "   2. Read the README.md in each project"
echo "   3. Run the examples"
echo "   4. Start learning!"
echo ""
echo "Happy learning! 🎓"