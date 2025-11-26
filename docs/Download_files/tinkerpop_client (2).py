"""
Apache TinkerPop Client for Pipeline Network Graph
Handles all graph database operations using Gremlin
"""

from gremlin_python.driver import client, serializer
from gremlin_python.driver.driver_remote_connection import DriverRemoteConnection
from gremlin_python.process.anonymous_traversal import traversal
from gremlin_python.process.graph_traversal import __
from gremlin_python.process.traversal import T, P, Order
from typing import Dict, List, Any, Optional
import logging
import asyncio
import json
from datetime import datetime
import uuid

logger = logging.getLogger(__name__)


class TinkerPopClient:
    """
    Client for Apache TinkerPop / JanusGraph
    Manages pipeline network graph structure
    """
    
    def __init__(self, endpoint: str = "ws://localhost:8182/gremlin"):
        self.endpoint = endpoint
        self.client = None
        self.g = None
        self.connected = False
        
    async def connect(self):
        """Establish connection to graph database"""
        try:
            logger.info(f"Connecting to TinkerPop at {self.endpoint}")
            
            self.client = client.Client(
                self.endpoint,
                'g',
                message_serializer=serializer.GraphSONSerializersV3d0()
            )
            
            # Test connection
            result = await self._submit("g.V().count()")
            
            self.connected = True
            logger.info(f"Connected to TinkerPop. Vertices: {result[0]}")
            
        except Exception as e:
            logger.error(f"Failed to connect to TinkerPop: {str(e)}")
            raise
    
    async def disconnect(self):
        """Close connection"""
        if self.client:
            self.client.close()
            self.connected = False
            logger.info("Disconnected from TinkerPop")
    
    def is_connected(self) -> bool:
        """Check connection status"""
        return self.connected
    
    async def _submit(self, query: str, bindings: Optional[Dict] = None) -> List:
        """Submit Gremlin query"""
        try:
            if bindings:
                result = self.client.submit(query, bindings).all().result()
            else:
                result = self.client.submit(query).all().result()
            
            return result
            
        except Exception as e:
            logger.error(f"Query failed: {str(e)}\nQuery: {query}")
            raise
    
    # Graph Schema Creation
    
    async def create_schema(self):
        """Create graph schema for pipeline network"""
        try:
            logger.info("Creating graph schema")
            
            # Create vertex labels
            vertex_labels = [
                'PipelineSegment',
                'Junction',
                'Valve',
                'Pump',
                'WaterSource',
                'Defect',
                'DetectionEvent',
                'Building',
                'Zone',
                'MaintenanceRecord'
            ]
            
            # Create edge labels
            edge_labels = [
                'CONNECTS',
                'FLOWS_TO',
                'CONTAINS',
                'HAS_DEFECT',
                'DETECTED_BY',
                'SERVES',
                'LOCATED_IN',
                'UPSTREAM_OF',
                'DOWNSTREAM_OF',
                'REQUIRES_MAINTENANCE'
            ]
            
            # Note: Schema creation is JanusGraph-specific
            # For now, graph will auto-create labels on insert
            
            logger.info("Schema ready")
            
        except Exception as e:
            logger.error(f"Schema creation failed: {str(e)}")
            raise
    
    # Vertex Operations
    
    async def add_pipeline_segment(
        self,
        segment_id: str,
        properties: Dict[str, Any]
    ) -> str:
        """Add pipeline segment vertex"""
        try:
            query = """
                g.addV('PipelineSegment')
                    .property('segment_id', segment_id)
                    .property('material', material)
                    .property('diameter', diameter)
                    .property('length', length)
                    .property('installation_date', install_date)
                    .property('start_lat', start_lat)
                    .property('start_lon', start_lon)
                    .property('end_lat', end_lat)
                    .property('end_lon', end_lon)
                    .property('status', status)
                    .property('created_at', created_at)
                    .id()
            """
            
            bindings = {
                'segment_id': segment_id,
                'material': properties.get('material', 'unknown'),
                'diameter': properties.get('diameter', 0),
                'length': properties.get('length', 0),
                'install_date': properties.get('installation_date', ''),
                'start_lat': properties.get('start_lat', 0.0),
                'start_lon': properties.get('start_lon', 0.0),
                'end_lat': properties.get('end_lat', 0.0),
                'end_lon': properties.get('end_lon', 0.0),
                'status': properties.get('status', 'active'),
                'created_at': datetime.utcnow().isoformat()
            }
            
            result = await self._submit(query, bindings)
            
            logger.debug(f"Added pipeline segment: {segment_id}")
            
            return result[0] if result else None
            
        except Exception as e:
            logger.error(f"Failed to add pipeline segment: {str(e)}")
            raise
    
    async def add_defect(
        self,
        detection: Dict[str, Any]
    ) -> str:
        """Add defect vertex from detection"""
        try:
            defect_id = detection.get('detection_id', str(uuid.uuid4()))
            
            query = """
                g.addV('Defect')
                    .property('defect_id', defect_id)
                    .property('defect_type', defect_type)
                    .property('severity', severity)
                    .property('confidence', confidence)
                    .property('visual_confidence', visual_conf)
                    .property('spectral_confidence', spectral_conf)
                    .property('latitude', lat)
                    .property('longitude', lon)
                    .property('bbox', bbox)
                    .property('area', area)
                    .property('spectral_signature', spectrum)
                    .property('detection_methods', methods)
                    .property('fusion_type', fusion_type)
                    .property('detected_at', detected_at)
                    .id()
            """
            
            bindings = {
                'defect_id': defect_id,
                'defect_type': detection.get('defect_type', 'unknown'),
                'severity': detection.get('severity', 5),
                'confidence': detection.get('combined_confidence', 0.5),
                'visual_conf': detection.get('visual_confidence', 0.0),
                'spectral_conf': detection.get('spectral_confidence', 0.0),
                'lat': detection.get('geo_coordinates', {}).get('latitude', 0.0),
                'lon': detection.get('geo_coordinates', {}).get('longitude', 0.0),
                'bbox': json.dumps(detection.get('bbox', [])),
                'area': detection.get('area', 0),
                'spectrum': json.dumps(detection.get('spectral_signature', [])),
                'methods': json.dumps(detection.get('detection_methods', {})),
                'fusion_type': detection.get('fusion_type', 'unknown'),
                'detected_at': datetime.utcnow().isoformat()
            }
            
            result = await self._submit(query, bindings)
            
            logger.debug(f"Added defect: {defect_id}")
            
            return result[0] if result else None
            
        except Exception as e:
            logger.error(f"Failed to add defect: {str(e)}")
            raise
    
    async def add_building(
        self,
        building_id: str,
        properties: Dict[str, Any]
    ) -> str:
        """Add building vertex"""
        try:
            query = """
                g.addV('Building')
                    .property('building_id', building_id)
                    .property('name', name)
                    .property('type', building_type)
                    .property('population', population)
                    .property('latitude', lat)
                    .property('longitude', lon)
                    .property('is_critical', is_critical)
                    .id()
            """
            
            bindings = {
                'building_id': building_id,
                'name': properties.get('name', ''),
                'building_type': properties.get('type', 'residential'),
                'population': properties.get('population', 0),
                'lat': properties.get('latitude', 0.0),
                'lon': properties.get('longitude', 0.0),
                'is_critical': properties.get('type') in ['Hospital', 'School', 'Shelter']
            }
            
            result = await self._submit(query, bindings)
            
            return result[0] if result else None
            
        except Exception as e:
            logger.error(f"Failed to add building: {str(e)}")
            raise
    
    # Edge Operations
    
    async def connect_segments(
        self,
        from_segment_id: str,
        to_segment_id: str,
        edge_type: str = 'FLOWS_TO'
    ):
        """Connect two pipeline segments"""
        try:
            query = f"""
                g.V().has('PipelineSegment', 'segment_id', from_id).as('from')
                .V().has('PipelineSegment', 'segment_id', to_id).as('to')
                .addE('{edge_type}')
                .from('from')
                .to('to')
                .property('created_at', created_at)
            """
            
            bindings = {
                'from_id': from_segment_id,
                'to_id': to_segment_id,
                'created_at': datetime.utcnow().isoformat()
            }
            
            await self._submit(query, bindings)
            
            logger.debug(f"Connected segments: {from_segment_id} -> {to_segment_id}")
            
        except Exception as e:
            logger.error(f"Failed to connect segments: {str(e)}")
            raise
    
    async def link_defect_to_segment(
        self,
        defect_id: str,
        segment_id: str
    ):
        """Link defect to pipeline segment"""
        try:
            query = """
                g.V().has('Defect', 'defect_id', defect_id).as('defect')
                .V().has('PipelineSegment', 'segment_id', segment_id).as('segment')
                .addE('HAS_DEFECT')
                .from('segment')
                .to('defect')
                .property('linked_at', linked_at)
            """
            
            bindings = {
                'defect_id': defect_id,
                'segment_id': segment_id,
                'linked_at': datetime.utcnow().isoformat()
            }
            
            await self._submit(query, bindings)
            
            logger.debug(f"Linked defect {defect_id} to segment {segment_id}")
            
        except Exception as e:
            logger.error(f"Failed to link defect: {str(e)}")
            raise
    
    async def link_segment_to_building(
        self,
        segment_id: str,
        building_id: str
    ):
        """Link pipeline segment to building it serves"""
        try:
            query = """
                g.V().has('PipelineSegment', 'segment_id', segment_id).as('segment')
                .V().has('Building', 'building_id', building_id).as('building')
                .addE('SERVES')
                .from('segment')
                .to('building')
            """
            
            bindings = {
                'segment_id': segment_id,
                'building_id': building_id
            }
            
            await self._submit(query, bindings)
            
        except Exception as e:
            logger.error(f"Failed to link segment to building: {str(e)}")
            raise
    
    # Query Operations
    
    async def get_critical_defects(
        self,
        severity_threshold: int = 7
    ) -> List[Dict[str, Any]]:
        """Get all critical defects and affected infrastructure"""
        try:
            query = """
                g.V().hasLabel('Defect')
                    .has('severity', P.gte(severity_threshold))
                    .as('defect')
                    .in('HAS_DEFECT').as('segment')
                    .out('SERVES').as('building')
                    .select('defect', 'segment', 'building')
                    .by(valueMap(true))
            """
            
            bindings = {'severity_threshold': severity_threshold}
            
            results = await self._submit(query, bindings)
            
            critical_defects = []
            for result in results:
                critical_defects.append({
                    'defect': self._parse_vertex(result['defect']),
                    'segment': self._parse_vertex(result['segment']),
                    'building': self._parse_vertex(result['building'])
                })
            
            logger.info(f"Found {len(critical_defects)} critical defects")
            
            return critical_defects
            
        except Exception as e:
            logger.error(f"Failed to get critical defects: {str(e)}")
            return []
    
    async def find_affected_infrastructure(
        self,
        defect_id: str,
        max_hops: int = 5
    ) -> Dict[str, Any]:
        """Find all infrastructure affected by a defect"""
        try:
            query = """
                g.V().has('Defect', 'defect_id', defect_id)
                    .in('HAS_DEFECT').as('defect_segment')
                    .repeat(out('FLOWS_TO'))
                    .until(has('label', 'Building').or().loops().is(P.gte(max_hops)))
                    .has('label', 'Building')
                    .as('affected_building')
                    .select('defect_segment', 'affected_building')
                    .by(valueMap(true))
            """
            
            bindings = {
                'defect_id': defect_id,
                'max_hops': max_hops
            }
            
            results = await self._submit(query, bindings)
            
            affected = {
                'defect_id': defect_id,
                'affected_buildings': [],
                'affected_population': 0
            }
            
            for result in results:
                building = self._parse_vertex(result['affected_building'])
                affected['affected_buildings'].append(building)
                affected['affected_population'] += building.get('population', 0)
            
            return affected
            
        except Exception as e:
            logger.error(f"Failed to find affected infrastructure: {str(e)}")
            return {'defect_id': defect_id, 'affected_buildings': [], 'affected_population': 0}
    
    async def get_isolated_zones(self) -> List[Dict[str, Any]]:
        """Find zones with no operational water supply"""
        try:
            query = """
                g.V().hasLabel('Zone')
                    .where(
                        not(
                            __.in('LOCATED_IN')
                              .hasLabel('PipelineSegment')
                              .has('status', 'operational')
                              .in('FLOWS_TO')
                              .hasLabel('WaterSource')
                        )
                    )
                    .valueMap(true)
            """
            
            results = await self._submit(query)
            
            isolated_zones = [self._parse_vertex(r) for r in results]
            
            logger.info(f"Found {len(isolated_zones)} isolated zones")
            
            return isolated_zones
            
        except Exception as e:
            logger.error(f"Failed to get isolated zones: {str(e)}")
            return []
    
    async def prioritize_repairs(self) -> List[Dict[str, Any]]:
        """Get repair priorities based on impact"""
        try:
            query = """
                g.V().hasLabel('Defect')
                    .as('defect')
                    .in('HAS_DEFECT').as('segment')
                    .out('SERVES')
                    .groupCount().by('type').as('served_buildings')
                    .select('defect', 'segment', 'served_buildings')
                    .order().by(select('served_buildings'), Order.desc)
                    .limit(20)
            """
            
            results = await self._submit(query)
            
            priorities = []
            for idx, result in enumerate(results):
                defect = self._parse_vertex(result['defect'])
                priorities.append({
                    'priority_rank': idx + 1,
                    'defect_id': defect.get('defect_id'),
                    'severity': defect.get('severity'),
                    'confidence': defect.get('confidence'),
                    'buildings_served': result.get('served_buildings', {}),
                    'estimated_impact': self._calculate_impact(result)
                })
            
            return priorities
            
        except Exception as e:
            logger.error(f"Failed to prioritize repairs: {str(e)}")
            return []
    
    async def get_network_statistics(self) -> Dict[str, Any]:
        """Get overall network statistics"""
        try:
            queries = {
                'total_segments': "g.V().hasLabel('PipelineSegment').count()",
                'operational_segments': "g.V().hasLabel('PipelineSegment').has('status', 'operational').count()",
                'total_defects': "g.V().hasLabel('Defect').count()",
                'critical_defects': "g.V().hasLabel('Defect').has('severity', P.gte(7)).count()",
                'total_buildings': "g.V().hasLabel('Building').count()",
                'critical_buildings': "g.V().hasLabel('Building').has('is_critical', true).count()"
            }
            
            stats = {}
            for key, query in queries.items():
                result = await self._submit(query)
                stats[key] = result[0] if result else 0
            
            # Calculate health score
            if stats['total_segments'] > 0:
                stats['network_health_score'] = (
                    stats['operational_segments'] / stats['total_segments']
                ) * 100
            else:
                stats['network_health_score'] = 0.0
            
            return stats
            
        except Exception as e:
            logger.error(f"Failed to get network statistics: {str(e)}")
            return {}
    
    async def export_graph(self, format: str = 'graphml') -> str:
        """Export entire graph"""
        try:
            # This would use graph export functionality
            # Implementation depends on JanusGraph capabilities
            logger.info(f"Exporting graph in {format} format")
            
            # Placeholder
            return "graph_export_path"
            
        except Exception as e:
            logger.error(f"Graph export failed: {str(e)}")
            raise
    
    # Helper methods
    
    def _parse_vertex(self, vertex_data: Dict) -> Dict[str, Any]:
        """Parse vertex data from Gremlin response"""
        parsed = {}
        
        for key, value in vertex_data.items():
            if isinstance(value, list) and len(value) > 0:
                parsed[key] = value[0]
            else:
                parsed[key] = value
        
        return parsed
    
    def _calculate_impact(self, result: Dict) -> int:
        """Calculate impact score for prioritization"""
        buildings_served = result.get('served_buildings', {})
        
        # Weight different building types
        weights = {
            'Hospital': 10,
            'School': 8,
            'Shelter': 9,
            'Residential': 5,
            'Commercial': 3
        }
        
        impact = sum(
            count * weights.get(building_type, 1)
            for building_type, count in buildings_served.items()
        )
        
        return impact
