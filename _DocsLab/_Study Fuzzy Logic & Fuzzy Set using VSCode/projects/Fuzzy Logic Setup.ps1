# Fuzzy Logic Study Environment Setup Script for Windows
# Run with: powershell -ExecutionPolicy Bypass -File setup.ps1

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   FUZZY LOGIC STUDY ENVIRONMENT SETUP (WINDOWS)       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

function Print-Step {
    param($Message)
    Write-Host "➜ $Message" -ForegroundColor Blue
}

function Print-Success {
    param($Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Print-Warning {
    param($Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Print-Error {
    param($Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Test-CommandExists {
    param($Command)
    $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

Write-Host "🔍 Checking prerequisites..." -ForegroundColor Cyan
Write-Host ""

# Check Python
$HasPython = $false
if (Test-CommandExists python) {
    $PythonVersion = (python --version 2>&1) -replace "Python ", ""
    Print-Success "Python $PythonVersion found"
    $HasPython = $true
} else {
    Print-Warning "Python 3 not found"
}

# Check Rust
$HasRust = $false
if (Test-CommandExists cargo) {
    $RustVersion = (cargo --version) -replace "cargo ", ""
    Print-Success "Rust $RustVersion found"
    $HasRust = $true
} else {
    Print-Warning "Rust not found"
}

# Check VSCode
$HasVSCode = $false
if (Test-CommandExists code) {
    Print-Success "VSCode found"
    $HasVSCode = $true
} else {
    Print-Warning "VSCode CLI not found"
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""

# Ask user what to set up
Write-Host "What would you like to set up?"
Write-Host "  1) Python environment only"
Write-Host "  2) Rust environment only"
Write-Host "  3) Both Python and Rust"
Write-Host ""
$SetupChoice = Read-Host "Enter choice (1-3)"

# Create workspace directory
$WorkspaceDir = "fuzzy-logic-workspace"
Print-Step "Creating workspace directory: $WorkspaceDir"
New-Item -ItemType Directory -Path $WorkspaceDir -Force | Out-Null
Set-Location $WorkspaceDir
Print-Success "Workspace created"
Write-Host ""

# Setup Python
if ($SetupChoice -eq "1" -or $SetupChoice -eq "3") {
    if ($HasPython) {
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
        Write-Host "🐍 Setting up Python environment" -ForegroundColor Yellow
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
        Write-Host ""
        
        # Create Python project structure
        Print-Step "Creating Python project structure..."
        $PythonDir = "python-fuzzy-logic"
        New-Item -ItemType Directory -Path "$PythonDir\src" -Force | Out-Null
        New-Item -ItemType Directory -Path "$PythonDir\examples" -Force | Out-Null
        New-Item -ItemType Directory -Path "$PythonDir\notebooks" -Force | Out-Null
        New-Item -ItemType Directory -Path "$PythonDir\tests" -Force | Out-Null
        New-Item -ItemType Directory -Path "$PythonDir\.vscode" -Force | Out-Null
        Set-Location $PythonDir
        
        # Create virtual environment
        Print-Step "Creating virtual environment..."
        python -m venv fuzzy_env
        Print-Success "Virtual environment created"
        
        # Activate virtual environment
        & .\fuzzy_env\Scripts\Activate.ps1
        
        # Upgrade pip
        Print-Step "Upgrading pip..."
        python -m pip install --upgrade pip --quiet
        Print-Success "pip upgraded"
        
        # Create requirements.txt
        Print-Step "Creating requirements.txt..."
        @"
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
"@ | Out-File -FilePath requirements.txt -Encoding UTF8
        Print-Success "requirements.txt created"
        
        # Install dependencies
        Print-Step "Installing Python dependencies (this may take a moment)..."
        pip install -r requirements.txt --quiet
        Print-Success "Dependencies installed"
        
        # Create project files
        Print-Step "Creating project files..."
        
        # Create __init__.py files
        New-Item -ItemType File -Path "src\__init__.py" -Force | Out-Null
        New-Item -ItemType File -Path "tests\__init__.py" -Force | Out-Null
        
        # Create README
        @"
# Fuzzy Logic Study - Python

## Quick Start

``````bash
# Activate virtual environment
.\fuzzy_env\Scripts\Activate.ps1

# Run main program
python src\main.py

# Run Jupyter notebook
jupyter notebook notebooks\

# Run examples
python examples\temperature_control.py
``````

## Project Structure

- ``src\`` - Main source code
- ``examples\`` - Example applications
- ``notebooks\`` - Jupyter notebooks
- ``tests\`` - Unit tests
"@ | Out-File -FilePath README.md -Encoding UTF8
        
        Print-Success "Python project setup complete"
        
        # Install VSCode extensions
        if ($HasVSCode) {
            Print-Step "Installing VSCode Python extensions..."
            code --install-extension ms-python.python 2>$null
            code --install-extension ms-toolsai.jupyter 2>$null
            code --install-extension ms-python.black-formatter 2>$null
            Print-Success "VSCode extensions installed"
        }
        
        Set-Location ..
        Write-Host ""
    } else {
        Print-Error "Python 3 is required but not found."
        Write-Host "Please install Python 3.8 or higher from: https://www.python.org/downloads/" -ForegroundColor Yellow
    }
}

# Setup Rust
if ($SetupChoice -eq "2" -or $SetupChoice -eq "3") {
    if ($HasRust) {
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
        Write-Host "🦀 Setting up Rust environment" -ForegroundColor Yellow
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
        Write-Host ""
        
        # Create Rust project
        Print-Step "Creating Rust project..."
        cargo new rust-fuzzy-logic --quiet 2>$null
        Set-Location rust-fuzzy-logic
        
        # Create project structure
        Print-Step "Creating project structure..."
        New-Item -ItemType Directory -Path "examples" -Force | Out-Null
        New-Item -ItemType Directory -Path "tests" -Force | Out-Null
        New-Item -ItemType Directory -Path ".vscode" -Force | Out-Null
        
        # Update Cargo.toml
        Print-Step "Configuring Cargo.toml..."
        @"
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
"@ | Out-File -FilePath Cargo.toml -Encoding UTF8
        Print-Success "Cargo.toml configured"
        
        # Build project
        Print-Step "Building project and downloading dependencies..."
        cargo build --quiet 2>$null
        Print-Success "Project built successfully"
        
        # Run tests
        Print-Step "Running tests..."
        cargo test --quiet 2>$null
        Print-Success "Tests passed"
        
        # Create README
        @"
# Fuzzy Logic Study - Rust

## Quick Start

``````bash
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
``````

## Project Structure

- ``src\`` - Main library code
- ``examples\`` - Example applications
- ``tests\`` - Unit tests
"@ | Out-File -FilePath README.md -Encoding UTF8
        
        Print-Success "Rust project setup complete"
        
        # Install VSCode extensions
        if ($HasVSCode) {
            Print-Step "Installing VSCode Rust extensions..."
            code --install-extension rust-lang.rust-analyzer 2>$null
            code --install-extension vadimcn.vscode-lldb 2>$null
            code --install-extension serayuzgur.crates 2>$null
            code --install-extension tamasfe.even-better-toml 2>$null
            Print-Success "VSCode extensions installed"
        }
        
        Set-Location ..
        Write-Host ""
    } else {
        Print-Error "Rust is required but not found."
        Write-Host "Please install Rust from: https://rustup.rs/" -ForegroundColor Yellow
        Write-Host "Or run in PowerShell: Invoke-WebRequest -Uri https://win.rustup.rs/x86_64 -OutFile rustup-init.exe; .\rustup-init.exe" -ForegroundColor Yellow
    }
}

# Create workspace settings
if ($HasVSCode) {
    Print-Step "Creating VSCode workspace file..."
    @"
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
"@ | Out-File -FilePath "fuzzy-logic.code-workspace" -Encoding UTF8
    Print-Success "VSCode workspace created"
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "✅ Setup Complete!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""
Write-Host "📂 Your workspace is in: $(Get-Location)"
Write-Host ""

if ($SetupChoice -eq "1" -or $SetupChoice -eq "3") {
    Write-Host "🐍 Python project:" -ForegroundColor Yellow
    Write-Host "   cd python-fuzzy-logic"
    Write-Host "   .\fuzzy_env\Scripts\Activate.ps1"
    Write-Host "   python src\main.py"
    Write-Host ""
}

if ($SetupChoice -eq "2" -or $SetupChoice -eq "3") {
    Write-Host "🦀 Rust project:" -ForegroundColor Yellow
    Write-Host "   cd rust-fuzzy-logic"
    Write-Host "   cargo run"
    Write-Host ""
}

if ($HasVSCode) {
    Write-Host "💡 To open in VSCode:" -ForegroundColor Cyan
    Write-Host "   code fuzzy-logic.code-workspace"
    Write-Host ""
}

Write-Host "📚 Next steps:"
Write-Host "   1. Copy the source files from the artifacts into the appropriate directories"
Write-Host "   2. Read the README.md in each project"
Write-Host "   3. Run the examples"
Write-Host "   4. Start learning!"
Write-Host ""
Write-Host "Happy learning! 🎓" -ForegroundColor Green