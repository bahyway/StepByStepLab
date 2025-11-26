#!/bin/bash

# WPDD Advanced - Automated Setup Script
# This script sets up the complete WPDD Advanced environment

set -e  # Exit on error

echo "========================================="
echo "WPDD Advanced - Setup Script"
echo "========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Helper functions
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# Check prerequisites
echo "Checking prerequisites..."

# Check Docker
if command -v docker &> /dev/null; then
    print_success "Docker is installed"
else
    print_error "Docker is not installed. Please install Docker first."
    exit 1
fi

# Check Docker Compose
if command -v docker-compose &> /dev/null; then
    print_success "Docker Compose is installed"
else
    print_error "Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Check Python
if command -v python3 &> /dev/null; then
    PYTHON_VERSION=$(python3 --version | cut -d ' ' -f 2)
    print_success "Python $PYTHON_VERSION is installed"
else
    print_error "Python 3 is not installed. Please install Python 3.11+ first."
    exit 1
fi

# Check .NET
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    print_success ".NET $DOTNET_VERSION is installed"
else
    print_info ".NET SDK not found (optional for C# development)"
fi

# Check NVIDIA GPU (optional)
if command -v nvidia-smi &> /dev/null; then
    print_success "NVIDIA GPU detected"
    GPU_AVAILABLE=true
else
    print_info "No NVIDIA GPU detected (CPU-only mode will be used)"
    GPU_AVAILABLE=false
fi

echo ""
echo "========================================="
echo "Step 1: Project Setup"
echo "========================================="

# Create directory structure
print_info "Creating directory structure..."
mkdir -p data/{satellite,hyperspectral,ground_truth,models}
mkdir -p outputs/{maps,reports,visualizations}
mkdir -p python/ml_service/{models,graph,visualization,utils}
mkdir -p src/{Domain,Application,Infrastructure,API}
mkdir -p docker
mkdir -p logs

print_success "Directory structure created"

echo ""
echo "========================================="
echo "Step 2: Environment Configuration"
echo "========================================="

# Create .env file
if [ ! -f .env ]; then
    print_info "Creating .env file..."
    cat > .env << EOL
# Database
DB_PASSWORD=wpdd_secure_password_$(openssl rand -hex 8)
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_DB=wpdd

# Redis
REDIS_HOST=redis
REDIS_PORT=6379

# JanusGraph
JANUSGRAPH_ENDPOINT=ws://janusgraph:8182/gremlin

# ML Service
ML_SERVICE_URL=http://ml_service:8000
MODEL_PATH=/models/yolov8x.pt

# Logging
LOG_LEVEL=INFO

# Optional: GPU settings
CUDA_VISIBLE_DEVICES=0
OMP_NUM_THREADS=4
EOL
    print_success ".env file created"
else
    print_info ".env file already exists, skipping"
fi

echo ""
echo "========================================="
echo "Step 3: Download Pre-trained Models"
echo "========================================="

# Download YOLOv8 models
print_info "Downloading YOLOv8 models..."
if [ ! -f data/models/yolov8x.pt ]; then
    python3 << PYTHON_SCRIPT
from ultralytics import YOLO
import os

os.makedirs('data/models', exist_ok=True)

# Download YOLOv8 extra-large model
print("Downloading YOLOv8x...")
model = YOLO('yolov8x.pt')
model.export(format='onnx')

# Save to models directory
import shutil
shutil.copy('yolov8x.pt', 'data/models/yolov8x.pt')

print("YOLOv8 model downloaded successfully")
PYTHON_SCRIPT
    print_success "YOLOv8 models downloaded"
else
    print_info "YOLOv8 models already exist, skipping"
fi

echo ""
echo "========================================="
echo "Step 4: Python Environment Setup"
echo "========================================="

# Create Python virtual environment
if [ ! -d "venv" ]; then
    print_info "Creating Python virtual environment..."
    python3 -m venv venv
    print_success "Virtual environment created"
fi

# Activate virtual environment
print_info "Activating virtual environment..."
source venv/bin/activate

# Upgrade pip
print_info "Upgrading pip..."
pip install --upgrade pip > /dev/null 2>&1

# Install Python dependencies
print_info "Installing Python dependencies (this may take several minutes)..."
cat > requirements_temp.txt << EOL
# Core
fastapi==0.104.1
uvicorn[standard]==0.24.0
python-multipart==0.0.6

# ML
ultralytics==8.0.230
torch==2.1.1
opencv-python==4.8.1.78
spectral==0.23.1

# Scientific
numpy==1.24.3
scipy==1.11.4

# Graph
gremlinpython==3.7.0

# Visualization
networkx==3.2.1
matplotlib==3.8.2
folium==0.15.0
plotly==5.18.0

# Utilities
pyyaml==6.0.1
redis==5.0.1
EOL

pip install -r requirements_temp.txt > /dev/null 2>&1
rm requirements_temp.txt

print_success "Python dependencies installed"

echo ""
echo "========================================="
echo "Step 5: Docker Services Setup"
echo "========================================="

# Check if docker-compose.yml exists
if [ ! -f docker-compose.yml ]; then
    print_error "docker-compose.yml not found. Please ensure it's in the project root."
    exit 1
fi

# Pull Docker images
print_info "Pulling Docker images..."
docker-compose pull

# Build custom images
print_info "Building custom Docker images..."
docker-compose build

print_success "Docker setup complete"

echo ""
echo "========================================="
echo "Step 6: Initialize Database"
echo "========================================="

# Start database services
print_info "Starting database services..."
docker-compose up -d postgres cassandra redis

# Wait for databases to be ready
print_info "Waiting for databases to initialize (this may take 1-2 minutes)..."
sleep 30

# Check PostgreSQL
until docker-compose exec -T postgres pg_isready -U wpdd_user > /dev/null 2>&1; do
    echo -n "."
    sleep 2
done
print_success "PostgreSQL is ready"

# Check Cassandra
until docker-compose exec -T cassandra cqlsh -e "DESCRIBE KEYSPACES" > /dev/null 2>&1; do
    echo -n "."
    sleep 5
done
print_success "Cassandra is ready"

# Check Redis
until docker-compose exec -T redis redis-cli ping > /dev/null 2>&1; do
    echo -n "."
    sleep 2
done
print_success "Redis is ready"

echo ""
echo "========================================="
echo "Step 7: Start All Services"
echo "========================================="

print_info "Starting all services..."
docker-compose up -d

# Wait for services to be healthy
print_info "Waiting for services to be healthy..."
sleep 30

# Check ML service
if curl -s http://localhost:8000/health > /dev/null; then
    print_success "ML Service is running"
else
    print_error "ML Service failed to start"
fi

# Check API service (if running)
if curl -s http://localhost:5000/health > /dev/null; then
    print_success "API Service is running"
else
    print_info "API Service not running (may need manual start)"
fi

echo ""
echo "========================================="
echo "Step 8: Verification"
echo "========================================="

print_info "Running verification tests..."

# Test ML service endpoints
python3 << PYTHON_TEST
import requests
import json

try:
    # Test health endpoint
    response = requests.get('http://localhost:8000/health')
    if response.status_code == 200:
        health = response.json()
        print(f"✓ ML Service health check passed")
        print(f"  - YOLOv8: {'✓' if health['components']['yolo_detector'] else '✗'}")
        print(f"  - Spectral: {'✓' if health['components']['spectral_analyzer'] else '✗'}")
        print(f"  - Graph: {'✓' if health['components']['graph_client'] else '✗'}")
    else:
        print(f"✗ ML Service health check failed")
except Exception as e:
    print(f"✗ Could not connect to ML Service: {str(e)}")
PYTHON_TEST

echo ""
echo "========================================="
echo "Setup Complete! 🎉"
echo "========================================="
echo ""
echo "Services are now running:"
echo "  - ML Service (Python):  http://localhost:8000"
echo "  - API Service (.NET):   http://localhost:5000"
echo "  - JanusGraph:           ws://localhost:8182/gremlin"
echo "  - PostgreSQL:           localhost:5432"
echo "  - Redis:                localhost:6379"
echo ""
echo "API Documentation:"
echo "  - ML Service Docs:      http://localhost:8000/docs"
echo "  - ML Service ReDoc:     http://localhost:8000/redoc"
echo ""
echo "Useful Commands:"
echo "  - View logs:            docker-compose logs -f [service]"
echo "  - Stop services:        docker-compose down"
echo "  - Restart services:     docker-compose restart"
echo "  - View status:          docker-compose ps"
echo ""
echo "Next Steps:"
echo "  1. Check the README.md for usage examples"
echo "  2. Try the example notebooks in python/notebooks/"
echo "  3. Upload test imagery to test detection"
echo ""
echo "Sample Test Command:"
echo '  curl -X POST "http://localhost:8000/api/detect/satellite-only" \'
echo '       -F "satellite_image=@your_image.tif"'
echo ""
print_success "WPDD Advanced is ready to use!"
