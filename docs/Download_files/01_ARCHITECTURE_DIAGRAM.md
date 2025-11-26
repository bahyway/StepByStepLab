# WPDD Advanced - System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           USER INTERFACE LAYER                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │   Web UI     │  │  Mobile App  │  │   Desktop    │  │  Field App   │   │
│  │   (Blazor)   │  │   (React)    │  │  (Avalonia)  │  │   (Mobile)   │   │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘   │
└─────────┼──────────────────┼──────────────────┼──────────────────┼──────────┘
          │                  │                  │                  │
          └──────────────────┴──────────────────┴──────────────────┘
                                      │
┌─────────────────────────────────────▼───────────────────────────────────────┐
│                        .NET 8 WEB API (Clean Architecture)                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        API Controllers Layer                         │   │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐      │   │
│  │  │ Detection  │ │   Graph    │ │Visualization│ │  Analysis  │      │   │
│  │  │ Controller │ │ Controller │ │ Controller  │ │ Controller │      │   │
│  │  └─────┬──────┘ └─────┬──────┘ └─────┬──────┘ └─────┬──────┘      │   │
│  └────────┼──────────────┼──────────────┼──────────────┼──────────────┘   │
│           │              │              │              │                    │
│  ┌────────▼──────────────▼──────────────▼──────────────▼──────────────┐   │
│  │                    Application Layer (CQRS + MediatR)               │   │
│  │  ┌──────────────────────┐         ┌──────────────────────┐         │   │
│  │  │      Commands        │         │       Queries        │         │   │
│  │  │ ┌──────────────────┐ │         │ ┌──────────────────┐ │         │   │
│  │  │ │ProcessSatellite  │ │         │ │GetNetworkStatus  │ │         │   │
│  │  │ │AssessWarZone     │ │         │ │GetCriticalDefects│ │         │   │
│  │  │ │CreateDefect      │ │         │ │GetRepairPriority │ │         │   │
│  │  │ └──────────────────┘ │         │ └──────────────────┘ │         │   │
│  │  └──────────┬───────────┘         └──────────┬───────────┘         │   │
│  └─────────────┼────────────────────────────────┼─────────────────────┘   │
│                │                                 │                          │
│  ┌─────────────▼─────────────────────────────────▼─────────────────────┐   │
│  │                      Domain Layer                                    │   │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐       │   │
│  │  │  Pipeline  │ │   Defect   │ │  Detection │ │   Network  │       │   │
│  │  │   Entity   │ │   Entity   │ │   Result   │ │  Aggregate │       │   │
│  │  └────────────┘ └────────────┘ └────────────┘ └────────────┘       │   │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐                      │   │
│  │  │    Geo     │ │  Spectral  │ │   Defect   │  Value Objects       │   │
│  │  │ Coordinate │ │ Signature  │ │  Severity  │                      │   │
│  │  └────────────┘ └────────────┘ └────────────┘                      │   │
│  └──────────────────────────────────────────────────────────────────────   │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Infrastructure Layer                              │  │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                │  │
│  │  │  ML Service  │ │  PostgreSQL  │ │    Redis     │                │  │
│  │  │    Client    │ │   (EF Core)  │ │    Cache     │                │  │
│  │  └──────┬───────┘ └──────┬───────┘ └──────────────┘                │  │
│  │         │HTTP            │                                           │  │
│  └─────────┼────────────────┼───────────────────────────────────────────┘  │
└────────────┼────────────────┼──────────────────────────────────────────────┘
             │                │
             │                │
┌────────────▼────────────────▼──────────────────────────────────────────────┐
│                    PYTHON ML SERVICE (FastAPI)                               │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                        REST API Endpoints                            │  │
│  │  /api/detect/multi-modal  |  /api/warzone/damage-assessment         │  │
│  │  /api/graph/query         |  /api/visualize/network-map             │  │
│  └────────┬─────────────────┬─────────────────┬─────────────────────────┘  │
│           │                 │                 │                             │
│  ┌────────▼────────┐ ┌──────▼─────────┐ ┌────▼────────────┐               │
│  │  YOLOv8         │ │  Spectral      │ │   Detection     │               │
│  │  Detector       │ │  Analyzer      │ │   Fusion        │               │
│  │                 │ │  (SPy)         │ │   Engine        │               │
│  │ ┌─────────────┐ │ │ ┌────────────┐ │ │ ┌─────────────┐ │               │
│  │ │ RGB Image   │ │ │ │Hyperspect- │ │ │ │Spatial      │ │               │
│  │ │ Processing  │ │ │ │ral Cube    │ │ │ │Matching     │ │               │
│  │ │             │ │ │ │Processing  │ │ │ │             │ │               │
│  │ │ - Tiling    │ │ │ │            │ │ │ │- IoU Calc   │ │               │
│  │ │ - Inference │ │ │ │- RX Anomaly│ │ │ │- Confidence │ │               │
│  │ │ - NMS       │ │ │ │- NDWI/NDVI │ │ │ │- Severity   │ │               │
│  │ │ - Bbox      │ │ │ │- ACE/MF    │ │ │ │- Priority   │ │               │
│  │ └─────────────┘ │ │ │- MNF       │ │ │ └─────────────┘ │               │
│  │                 │ │ │- SAM       │ │ │                 │               │
│  │  Visual         │ │ │            │ │ │  Combined       │               │
│  │  Detections     │ │ │ Spectral   │ │ │  Detections     │               │
│  └────────┬────────┘ │ │ Detections │ │ └────────┬────────┘               │
│           │          │ └────────┬───┘ │          │                         │
│           └──────────┴──────────┴─────┴──────────┘                         │
│                              │                                              │
│  ┌───────────────────────────▼──────────────────────────────────────────┐  │
│  │                    Graph Database Layer                              │  │
│  │  ┌──────────────────┐         ┌──────────────────┐                  │  │
│  │  │  TinkerPop       │         │   Graph          │                  │  │
│  │  │  Client          │◄────────┤   Builder        │                  │  │
│  │  │                  │         │                  │                  │  │
│  │  │ - Add Vertices   │         │ - Network Model  │                  │  │
│  │  │ - Add Edges      │         │ - Relationships  │                  │  │
│  │  │ - Gremlin Query  │         │ - Impact Calc    │                  │  │
│  │  └─────────┬────────┘         └──────────────────┘                  │  │
│  └────────────┼─────────────────────────────────────────────────────────┘  │
│               │                                                             │
│  ┌────────────▼─────────────────────────────────────────────────────────┐  │
│  │                  NetworkX Visualizer                                 │  │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                │  │
│  │  │   Folium     │ │  Matplotlib  │ │    Plotly    │                │  │
│  │  │  (2D Maps)   │ │  (Topology)  │ │  (3D Viz)    │                │  │
│  │  │              │ │              │ │              │                │  │
│  │  │- Interactive │ │- Spring      │ │- Interactive │                │  │
│  │  │- Markers     │ │- Hierarchical│ │- Rotation    │                │  │
│  │  │- Heatmaps    │ │- Circular    │ │- Elevation   │                │  │
│  │  │- Layers      │ │- Metrics     │ │- Hover Info  │                │  │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────┬───────────────────────────────────────────┘
                                   │
┌──────────────────────────────────▼───────────────────────────────────────────┐
│                         DATA PERSISTENCE LAYER                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │  PostgreSQL  │  │  JanusGraph  │  │   Cassandra  │  │     Redis    │    │
│  │              │  │              │  │              │  │              │    │
│  │ - Defects    │  │ - Network    │  │ - Graph      │  │ - Session    │    │
│  │ - Segments   │  │ - Vertices   │  │   Backend    │  │ - Cache      │    │
│  │ - Reports    │  │ - Edges      │  │ - Storage    │  │ - Jobs       │    │
│  │ - Geo Data   │  │ - Traversals │  │              │  │              │    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
└──────────────────────────────────────────────────────────────────────────────┘

                              DATA FLOW EXAMPLE
                              
┌─────────────┐
│ Satellite   │
│ RGB Image   │────┐
└─────────────┘    │
                   │
┌─────────────┐    │    ┌──────────────┐
│Hyperspectral│────┼───►│ ML Service   │
│   Cube      │    │    │              │
└─────────────┘    │    │ 1. YOLOv8    │────► Visual Detections
                   │    │ 2. SPy       │────► Spectral Detections
                        │ 3. Fusion    │────► Combined Results
                        └──────┬───────┘
                               │
                        ┌──────▼────────┐
                        │  TinkerPop    │
                        │  Graph DB     │
                        │               │
                        │ - Store nodes │
                        │ - Store edges │
                        │ - Run queries │
                        └──────┬────────┘
                               │
                        ┌──────▼────────┐
                        │  NetworkX     │
                        │  Visualizer   │
                        │               │
                        │ - Maps        │
                        │ - Graphs      │
                        │ - 3D Views    │
                        └───────────────┘
                               │
                        ┌──────▼────────┐
                        │   Reports &   │
                        │ Visualizations│
                        └───────────────┘
```

## Key Component Interactions

### 1. Detection Pipeline
```
User Upload → API → ML Service → YOLOv8 + SPy → Fusion → Graph → Response
```

### 2. War Zone Assessment
```
Before/After Images → Change Detection → Impact Analysis → Priority Ranking
```

### 3. Network Analysis
```
Graph Query → TinkerPop → NetworkX → Visualization → User
```

### 4. Real-time Monitoring
```
Satellite Feed → Auto-Detection → Alert Generation → Priority Notification
```

## Technology Stack Summary

| Layer | Technologies |
|-------|-------------|
| **Frontend** | Blazor, React, Avalonia |
| **Backend API** | .NET 8, ASP.NET Core, MediatR |
| **ML Service** | Python 3.11, FastAPI, Uvicorn |
| **Detection** | YOLOv8 (Ultralytics), PyTorch |
| **Spectral** | Spectral Python (SPy), NumPy, SciPy |
| **Graph DB** | JanusGraph, Apache TinkerPop, Gremlin |
| **Visualization** | NetworkX, Folium, Plotly, Matplotlib |
| **Data Storage** | PostgreSQL, Cassandra, Redis |
| **Orchestration** | Docker, Docker Compose |
| **Architecture** | Clean Architecture, DDD, CQRS |

## Deployment Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    DOCKER COMPOSE                       │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │   API    │  │    ML    │  │   Viz    │            │
│  │ Service  │  │ Service  │  │ Service  │            │
│  │ :5000    │  │ :8000    │  │ :8050    │            │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘            │
│       │             │              │                   │
│  ┌────▼─────────────▼──────────────▼─────┐            │
│  │         wpdd_network                   │            │
│  │         (Docker Bridge)                │            │
│  └────┬──────────┬──────────┬─────────┬──┘            │
│       │          │          │         │                │
│  ┌────▼────┐ ┌──▼────┐ ┌───▼───┐ ┌──▼──┐             │
│  │Postgres │ │Cassan-│ │  Jana │ │Redis│             │
│  │  :5432  │ │dra    │ │sGraph │ │:6379│             │
│  │         │ │ :9042 │ │ :8182 │ │     │             │
│  └─────────┘ └───────┘ └───────┘ └─────┘             │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

This architecture provides:
- ✅ Horizontal scalability
- ✅ Service isolation
- ✅ Easy deployment
- ✅ Health monitoring
- ✅ Data persistence
- ✅ Network security
