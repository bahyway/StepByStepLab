"""
WPDD Advanced ML Service
Main FastAPI application integrating SPy, YOLOv8, and TinkerPop
"""

from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.responses import JSONResponse, FileResponse
from pydantic import BaseModel
from typing import List, Optional, Dict, Any
import numpy as np
import uvicorn
from pathlib import Path
import logging
import tempfile
import shutil

# Import our custom modules
from models.yolo_detector import YOLODetector
from models.spectral_analyzer import SpectralAnalyzer
from models.fusion_engine import DetectionFusionEngine
from graph.tinkerpop_client import TinkerPopClient
from graph.graph_builder import PipelineGraphBuilder
from visualization.networkx_viz import NetworkXVisualizer
from utils.preprocessing import ImagePreprocessor

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI(
    title="WPDD Advanced ML Service",
    description="Multi-modal pipeline defect detection using SPy, YOLOv8, and Graph Analytics",
    version="1.0.0"
)

# Initialize components
yolo_detector = YOLODetector(model_path="models/yolov8x.pt")
spectral_analyzer = SpectralAnalyzer()
fusion_engine = DetectionFusionEngine()
graph_client = TinkerPopClient(endpoint="ws://janusgraph:8182/gremlin")
graph_builder = PipelineGraphBuilder(graph_client)
visualizer = NetworkXVisualizer(graph_client)
preprocessor = ImagePreprocessor()

# Pydantic models for API
class DetectionResult(BaseModel):
    detection_id: str
    bbox: List[float]
    geo_coordinates: Dict[str, float]
    defect_type: str
    visual_confidence: float
    spectral_confidence: float
    combined_confidence: float
    severity: int
    spectral_signature: Optional[List[float]]
    metadata: Dict[str, Any]

class ProcessingRequest(BaseModel):
    area_id: str
    timestamp: str
    priority: str = "normal"

class GraphQuery(BaseModel):
    query_type: str
    parameters: Dict[str, Any]

class NetworkAnalysis(BaseModel):
    total_segments: int
    total_defects: int
    critical_defects: int
    affected_population: int
    network_health_score: float
    priority_repairs: List[Dict[str, Any]]

# Health check endpoint
@app.get("/health")
async def health_check():
    """Check service health and component status"""
    return {
        "status": "healthy",
        "components": {
            "yolo_detector": yolo_detector.is_loaded(),
            "spectral_analyzer": spectral_analyzer.is_ready(),
            "graph_client": graph_client.is_connected(),
            "visualizer": True
        },
        "version": "1.0.0"
    }

# Main detection endpoint
@app.post("/api/detect/multi-modal", response_model=List[DetectionResult])
async def detect_multimodal(
    rgb_image: UploadFile = File(...),
    hyperspectral_image: UploadFile = File(...),
    metadata: str = None
):
    """
    Process RGB satellite image and hyperspectral cube for pipeline defect detection
    
    Args:
        rgb_image: RGB satellite image (GeoTIFF)
        hyperspectral_image: Hyperspectral image cube (ENVI format)
        metadata: JSON string with area info and parameters
    
    Returns:
        List of detected defects with multi-modal confidence scores
    """
    try:
        logger.info("Starting multi-modal detection")
        
        # Save uploaded files temporarily
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_path = Path(temp_dir)
            
            # Save RGB image
            rgb_path = temp_path / rgb_image.filename
            with open(rgb_path, "wb") as f:
                shutil.copyfileobj(rgb_image.file, f)
            
            # Save hyperspectral image
            hyper_path = temp_path / hyperspectral_image.filename
            with open(hyper_path, "wb") as f:
                shutil.copyfileobj(hyperspectral_image.file, f)
            
            logger.info(f"Processing images: {rgb_path.name}, {hyper_path.name}")
            
            # 1. Preprocess images
            rgb_preprocessed = preprocessor.preprocess_rgb(str(rgb_path))
            hyper_preprocessed = preprocessor.preprocess_hyperspectral(str(hyper_path))
            
            # 2. YOLOv8 detection on RGB
            logger.info("Running YOLOv8 detection...")
            yolo_detections = yolo_detector.detect(rgb_preprocessed)
            
            # 3. Spectral analysis with SPy
            logger.info("Running spectral analysis...")
            spectral_results = spectral_analyzer.analyze(hyper_preprocessed)
            
            # 4. Fuse detections
            logger.info("Fusing multi-modal detections...")
            fused_detections = fusion_engine.fuse(
                yolo_detections,
                spectral_results,
                rgb_preprocessed,
                hyper_preprocessed
            )
            
            # 5. Store in graph database
            logger.info("Storing results in graph database...")
            for detection in fused_detections:
                await graph_builder.add_defect(detection)
            
            logger.info(f"Detection complete: {len(fused_detections)} defects found")
            
            return fused_detections
            
    except Exception as e:
        logger.error(f"Detection error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/detect/satellite-only", response_model=List[DetectionResult])
async def detect_satellite_only(
    satellite_image: UploadFile = File(...),
    area_bounds: Optional[str] = None
):
    """
    Process satellite RGB imagery only (when hyperspectral not available)
    """
    try:
        logger.info("Starting satellite-only detection")
        
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_path = Path(temp_dir)
            image_path = temp_path / satellite_image.filename
            
            with open(image_path, "wb") as f:
                shutil.copyfileobj(satellite_image.file, f)
            
            # Preprocess and detect
            preprocessed = preprocessor.preprocess_rgb(str(image_path))
            detections = yolo_detector.detect(preprocessed)
            
            # Store in graph
            for detection in detections:
                await graph_builder.add_defect(detection)
            
            logger.info(f"Satellite detection complete: {len(detections)} defects found")
            
            return detections
            
    except Exception as e:
        logger.error(f"Satellite detection error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/analyze/spectral-signature")
async def analyze_spectral_signature(
    hyperspectral_image: UploadFile = File(...),
    roi_coords: Optional[str] = None
):
    """
    Analyze spectral signatures in a specific region of interest
    Useful for identifying leak signatures
    """
    try:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_path = Path(temp_dir)
            image_path = temp_path / hyperspectral_image.filename
            
            with open(image_path, "wb") as f:
                shutil.copyfileobj(hyperspectral_image.file, f)
            
            # Analyze spectral signatures
            result = spectral_analyzer.extract_signatures(
                str(image_path),
                roi_coords
            )
            
            return result
            
    except Exception as e:
        logger.error(f"Spectral analysis error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/graph/query", response_model=Dict[str, Any])
async def query_graph(query: GraphQuery):
    """
    Execute Gremlin queries on the pipeline graph
    
    Supported query types:
    - get_critical_defects
    - find_affected_infrastructure
    - get_isolated_zones
    - prioritize_repairs
    """
    try:
        logger.info(f"Executing graph query: {query.query_type}")
        
        if query.query_type == "get_critical_defects":
            result = await graph_builder.get_critical_defects(
                severity_threshold=query.parameters.get("severity", 7)
            )
        elif query.query_type == "find_affected_infrastructure":
            result = await graph_builder.find_affected_infrastructure(
                defect_id=query.parameters.get("defect_id")
            )
        elif query.query_type == "get_isolated_zones":
            result = await graph_builder.get_isolated_zones()
        elif query.query_type == "prioritize_repairs":
            result = await graph_builder.prioritize_repairs()
        else:
            raise HTTPException(status_code=400, detail=f"Unknown query type: {query.query_type}")
        
        return result
        
    except Exception as e:
        logger.error(f"Graph query error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/graph/network-analysis", response_model=NetworkAnalysis)
async def analyze_network():
    """
    Perform comprehensive network analysis
    Returns metrics about network health and critical nodes
    """
    try:
        logger.info("Performing network analysis")
        
        analysis = await graph_builder.analyze_network()
        
        return analysis
        
    except Exception as e:
        logger.error(f"Network analysis error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/visualize/network-map")
async def visualize_network_map(
    area_id: Optional[str] = None,
    include_defects: bool = True
):
    """
    Generate interactive network visualization map
    Returns HTML file with Folium map
    """
    try:
        logger.info(f"Generating network map for area: {area_id}")
        
        output_path = visualizer.create_geospatial_map(
            area_id=area_id,
            include_defects=include_defects
        )
        
        return FileResponse(
            output_path,
            media_type="text/html",
            filename="pipeline_network.html"
        )
        
    except Exception as e:
        logger.error(f"Visualization error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/visualize/network-topology")
async def visualize_network_topology(
    area_id: Optional[str] = None,
    layout: str = "spring"
):
    """
    Generate network topology visualization
    Returns PNG image with NetworkX graph layout
    """
    try:
        logger.info(f"Generating topology visualization: {layout}")
        
        output_path = visualizer.create_topology_visualization(
            area_id=area_id,
            layout=layout
        )
        
        return FileResponse(
            output_path,
            media_type="image/png",
            filename="network_topology.png"
        )
        
    except Exception as e:
        logger.error(f"Topology visualization error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/visualize/3d-network")
async def visualize_3d_network(area_id: Optional[str] = None):
    """
    Generate interactive 3D network visualization
    Returns HTML file with Plotly 3D visualization
    """
    try:
        logger.info("Generating 3D network visualization")
        
        output_path = visualizer.create_3d_visualization(area_id=area_id)
        
        return FileResponse(
            output_path,
            media_type="text/html",
            filename="network_3d.html"
        )
        
    except Exception as e:
        logger.error(f"3D visualization error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/warzone/damage-assessment")
async def warzone_damage_assessment(
    area_id: str,
    before_image: UploadFile = File(...),
    after_image: UploadFile = File(...),
    hyperspectral_after: Optional[UploadFile] = File(None)
):
    """
    Specialized endpoint for war zone damage assessment
    Compares before/after imagery to identify infrastructure damage
    """
    try:
        logger.info(f"Starting war zone assessment for area: {area_id}")
        
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_path = Path(temp_dir)
            
            # Save images
            before_path = temp_path / before_image.filename
            after_path = temp_path / after_image.filename
            
            with open(before_path, "wb") as f:
                shutil.copyfileobj(before_image.file, f)
            with open(after_path, "wb") as f:
                shutil.copyfileobj(after_image.file, f)
            
            # Detect changes
            before_detections = yolo_detector.detect(
                preprocessor.preprocess_rgb(str(before_path))
            )
            after_detections = yolo_detector.detect(
                preprocessor.preprocess_rgb(str(after_path))
            )
            
            # Change detection
            changes = fusion_engine.detect_changes(
                before_detections,
                after_detections
            )
            
            # If hyperspectral available, add spectral analysis
            if hyperspectral_after:
                hyper_path = temp_path / hyperspectral_after.filename
                with open(hyper_path, "wb") as f:
                    shutil.copyfileobj(hyperspectral_after.file, f)
                
                spectral_results = spectral_analyzer.analyze(
                    preprocessor.preprocess_hyperspectral(str(hyper_path))
                )
                
                changes = fusion_engine.enhance_with_spectral(
                    changes,
                    spectral_results
                )
            
            # Store in graph and calculate impact
            impact_assessment = await graph_builder.assess_damage_impact(
                area_id,
                changes
            )
            
            logger.info(f"Damage assessment complete: {len(changes)} changes detected")
            
            return {
                "area_id": area_id,
                "total_changes": len(changes),
                "new_defects": len([c for c in changes if c["change_type"] == "new_damage"]),
                "affected_population": impact_assessment["affected_population"],
                "critical_infrastructure_at_risk": impact_assessment["critical_count"],
                "priority_repairs": impact_assessment["priorities"],
                "changes": changes
            }
            
    except Exception as e:
        logger.error(f"War zone assessment error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/export/report")
async def export_report(
    area_id: str,
    format: str = "pdf"
):
    """
    Export comprehensive damage assessment report
    Includes maps, statistics, and repair priorities
    """
    try:
        logger.info(f"Generating report for area: {area_id}")
        
        # Generate report with visualizations
        report_path = await graph_builder.generate_report(
            area_id=area_id,
            format=format
        )
        
        return FileResponse(
            report_path,
            media_type=f"application/{format}",
            filename=f"wpdd_report_{area_id}.{format}"
        )
        
    except Exception as e:
        logger.error(f"Report generation error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

# Startup event
@app.on_event("startup")
async def startup_event():
    """Initialize components on startup"""
    logger.info("Starting WPDD Advanced ML Service")
    
    # Initialize graph database connection
    await graph_client.connect()
    
    # Load ML models
    yolo_detector.load_model()
    spectral_analyzer.initialize()
    
    logger.info("All components initialized successfully")

# Shutdown event
@app.on_event("shutdown")
async def shutdown_event():
    """Cleanup on shutdown"""
    logger.info("Shutting down WPDD Advanced ML Service")
    
    await graph_client.disconnect()
    
    logger.info("Shutdown complete")

if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level="info"
    )
