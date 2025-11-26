"""
NetworkX Visualizer for Pipeline Network
Creates interactive maps and graph visualizations
"""

import networkx as nx
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.colors import LinearSegmentedColormap
import numpy as np
from typing import Dict, List, Any, Optional, Tuple
import logging
import folium
from folium import plugins
import plotly.graph_objects as go
from pathlib import Path
import json

logger = logging.getLogger(__name__)


class NetworkXVisualizer:
    """
    Creates visualizations of pipeline network using NetworkX
    Supports 2D static graphs, interactive maps, and 3D visualizations
    """
    
    def __init__(self, graph_client):
        self.graph_client = graph_client
        self.graph = nx.DiGraph()
        self.output_dir = Path("/tmp/wpdd_visualizations")
        self.output_dir.mkdir(exist_ok=True, parents=True)
        
        # Color schemes
        self.defect_colors = {
            'leak': '#FF0000',          # Red
            'crack': '#FFA500',         # Orange
            'corrosion': '#8B4513',     # Brown
            'faulty_pipe': '#FF4500',   # Orange-Red
            'pipe': '#00FF00',          # Green
            'none': '#90EE90'           # Light Green
        }
        
        self.severity_colormap = plt.cm.RdYlGn_r  # Red (high) to Green (low)
        
    async def load_from_graph_db(self, area_id: Optional[str] = None):
        """Load graph data from TinkerPop into NetworkX"""
        try:
            logger.info(f"Loading graph from database (area: {area_id or 'all'})")
            
            self.graph.clear()
            
            # Get all vertices
            if area_id:
                vertices_query = f"g.V().has('area_id', '{area_id}').valueMap(true)"
            else:
                vertices_query = "g.V().valueMap(true)"
            
            vertices = await self.graph_client._submit(vertices_query)
            
            for vertex in vertices:
                vertex_data = self._parse_vertex(vertex)
                node_id = vertex_data.get('id', vertex_data.get('defect_id', vertex_data.get('segment_id')))
                
                self.graph.add_node(
                    node_id,
                    **vertex_data
                )
            
            # Get all edges
            if area_id:
                edges_query = f"g.E().where(outV().has('area_id', '{area_id}'))" \
                             ".project('out', 'in', 'label', 'props')" \
                             ".by(outV().id()).by(inV().id()).by(label()).by(valueMap())"
            else:
                edges_query = "g.E().project('out', 'in', 'label', 'props')" \
                             ".by(outV().id()).by(inV().id()).by(label()).by(valueMap())"
            
            edges = await self.graph_client._submit(edges_query)
            
            for edge in edges:
                self.graph.add_edge(
                    edge['out'],
                    edge['in'],
                    label=edge['label'],
                    **edge.get('props', {})
                )
            
            logger.info(f"Loaded graph: {self.graph.number_of_nodes()} nodes, {self.graph.number_of_edges()} edges")
            
        except Exception as e:
            logger.error(f"Failed to load graph: {str(e)}")
            raise
    
    def create_geospatial_map(
        self,
        area_id: Optional[str] = None,
        include_defects: bool = True
    ) -> str:
        """
        Create interactive geospatial map with Folium
        
        Returns:
            Path to HTML file
        """
        try:
            logger.info("Creating geospatial map")
            
            # Calculate map center
            center_lat, center_lon = self._calculate_center()
            
            # Create base map
            m = folium.Map(
                location=[center_lat, center_lon],
                zoom_start=13,
                tiles='OpenStreetMap'
            )
            
            # Add different tile layers
            folium.TileLayer('CartoDB positron').add_to(m)
            folium.TileLayer('CartoDB dark_matter').add_to(m)
            
            # Add pipeline segments
            pipeline_layer = folium.FeatureGroup(name='Pipeline Network')
            
            for node in self.graph.nodes(data=True):
                if node[1].get('label') == 'PipelineSegment':
                    self._add_segment_to_map(pipeline_layer, node)
            
            pipeline_layer.add_to(m)
            
            # Add defects
            if include_defects:
                defect_cluster = plugins.MarkerCluster(name='Defects')
                
                for node in self.graph.nodes(data=True):
                    if node[1].get('label') == 'Defect':
                        self._add_defect_to_map(defect_cluster, node)
                
                defect_cluster.add_to(m)
            
            # Add buildings
            buildings_layer = folium.FeatureGroup(name='Buildings')
            
            for node in self.graph.nodes(data=True):
                if node[1].get('label') == 'Building':
                    self._add_building_to_map(buildings_layer, node)
            
            buildings_layer.add_to(m)
            
            # Add defect heatmap
            if include_defects:
                self._add_defect_heatmap(m)
            
            # Add mini map
            plugins.MiniMap().add_to(m)
            
            # Add measure control
            plugins.MeasureControl(position='topright').add_to(m)
            
            # Add layer control
            folium.LayerControl().add_to(m)
            
            # Add legend
            self._add_map_legend(m)
            
            # Save
            output_path = self.output_dir / "pipeline_network_map.html"
            m.save(str(output_path))
            
            logger.info(f"Geospatial map saved to {output_path}")
            
            return str(output_path)
            
        except Exception as e:
            logger.error(f"Failed to create geospatial map: {str(e)}")
            raise
    
    def create_topology_visualization(
        self,
        area_id: Optional[str] = None,
        layout: str = "spring"
    ) -> str:
        """
        Create network topology visualization
        
        Args:
            area_id: Optional area filter
            layout: Layout algorithm ('spring', 'circular', 'kamada_kawai', 'hierarchical')
            
        Returns:
            Path to PNG file
        """
        try:
            logger.info(f"Creating topology visualization with {layout} layout")
            
            fig, ax = plt.subplots(figsize=(20, 16), dpi=300)
            
            # Calculate layout
            if layout == "spring":
                pos = nx.spring_layout(self.graph, k=2, iterations=50, seed=42)
            elif layout == "circular":
                pos = nx.circular_layout(self.graph)
            elif layout == "kamada_kawai":
                pos = nx.kamada_kawai_layout(self.graph)
            elif layout == "hierarchical":
                pos = self._hierarchical_layout()
            else:
                pos = nx.spring_layout(self.graph)
            
            # Separate nodes by type
            node_types = {
                'pipeline': [],
                'defect': [],
                'building': [],
                'critical_building': [],
                'other': []
            }
            
            for node, data in self.graph.nodes(data=True):
                label = data.get('label', '')
                
                if label == 'PipelineSegment':
                    node_types['pipeline'].append(node)
                elif label == 'Defect':
                    node_types['defect'].append(node)
                elif label == 'Building':
                    if data.get('is_critical', False):
                        node_types['critical_building'].append(node)
                    else:
                        node_types['building'].append(node)
                else:
                    node_types['other'].append(node)
            
            # Draw pipeline network
            nx.draw_networkx_nodes(
                self.graph, pos,
                nodelist=node_types['pipeline'],
                node_color='lightblue',
                node_size=100,
                alpha=0.7,
                label='Pipeline Segments',
                ax=ax
            )
            
            # Draw defects with size based on severity
            if node_types['defect']:
                defect_sizes = [
                    self.graph.nodes[n].get('severity', 5) * 100
                    for n in node_types['defect']
                ]
                defect_colors = [
                    self.severity_colormap(self.graph.nodes[n].get('severity', 5) / 10)
                    for n in node_types['defect']
                ]
                
                nx.draw_networkx_nodes(
                    self.graph, pos,
                    nodelist=node_types['defect'],
                    node_color=defect_colors,
                    node_size=defect_sizes,
                    alpha=0.9,
                    label='Defects (size = severity)',
                    ax=ax
                )
            
            # Draw regular buildings
            nx.draw_networkx_nodes(
                self.graph, pos,
                nodelist=node_types['building'],
                node_color='gray',
                node_size=150,
                node_shape='s',
                alpha=0.6,
                label='Buildings',
                ax=ax
            )
            
            # Draw critical buildings
            nx.draw_networkx_nodes(
                self.graph, pos,
                nodelist=node_types['critical_building'],
                node_color='green',
                node_size=200,
                node_shape='s',
                alpha=0.9,
                label='Critical Buildings',
                ax=ax
            )
            
            # Draw edges with different styles
            flow_edges = [(u, v) for u, v, d in self.graph.edges(data=True)
                         if d.get('label') in ['FLOWS_TO', 'CONNECTS']]
            serves_edges = [(u, v) for u, v, d in self.graph.edges(data=True)
                           if d.get('label') == 'SERVES']
            defect_edges = [(u, v) for u, v, d in self.graph.edges(data=True)
                           if d.get('label') == 'HAS_DEFECT']
            
            # Flow edges
            nx.draw_networkx_edges(
                self.graph, pos,
                edgelist=flow_edges,
                edge_color='gray',
                arrows=True,
                arrowsize=10,
                alpha=0.5,
                width=1,
                ax=ax
            )
            
            # Service edges
            nx.draw_networkx_edges(
                self.graph, pos,
                edgelist=serves_edges,
                edge_color='blue',
                arrows=True,
                arrowsize=8,
                alpha=0.3,
                width=0.5,
                style='dashed',
                ax=ax
            )
            
            # Defect edges
            nx.draw_networkx_edges(
                self.graph, pos,
                edgelist=defect_edges,
                edge_color='red',
                arrows=True,
                arrowsize=8,
                alpha=0.7,
                width=2,
                ax=ax
            )
            
            # Add title and legend
            ax.set_title(
                "Water Pipeline Network Topology\n"
                f"Nodes: {self.graph.number_of_nodes()}, "
                f"Edges: {self.graph.number_of_edges()}, "
                f"Defects: {len(node_types['defect'])}",
                fontsize=16,
                fontweight='bold'
            )
            
            ax.legend(loc='upper left', fontsize=12)
            ax.axis('off')
            
            plt.tight_layout()
            
            # Save
            output_path = self.output_dir / "network_topology.png"
            plt.savefig(output_path, dpi=300, bbox_inches='tight', facecolor='white')
            plt.close()
            
            logger.info(f"Topology visualization saved to {output_path}")
            
            return str(output_path)
            
        except Exception as e:
            logger.error(f"Failed to create topology visualization: {str(e)}")
            raise
    
    def create_3d_visualization(self, area_id: Optional[str] = None) -> str:
        """
        Create interactive 3D visualization with Plotly
        
        Returns:
            Path to HTML file
        """
        try:
            logger.info("Creating 3D visualization")
            
            # Get node positions (use lat/lon + elevation)
            node_coords = {}
            for node, data in self.graph.nodes(data=True):
                lat = data.get('latitude', 0)
                lon = data.get('longitude', 0)
                elev = data.get('elevation', 0)
                node_coords[node] = (lon, lat, elev)
            
            # Create edge traces
            edge_traces = []
            
            for edge in self.graph.edges():
                source_pos = node_coords.get(edge[0], (0, 0, 0))
                target_pos = node_coords.get(edge[1], (0, 0, 0))
                
                edge_traces.append(
                    go.Scatter3d(
                        x=[source_pos[0], target_pos[0], None],
                        y=[source_pos[1], target_pos[1], None],
                        z=[source_pos[2], target_pos[2], None],
                        mode='lines',
                        line=dict(color='gray', width=2),
                        hoverinfo='none',
                        showlegend=False
                    )
                )
            
            # Create node traces by type
            node_traces = []
            
            # Pipeline segments
            pipeline_nodes = [n for n, d in self.graph.nodes(data=True)
                             if d.get('label') == 'PipelineSegment']
            if pipeline_nodes:
                coords = [node_coords.get(n, (0, 0, 0)) for n in pipeline_nodes]
                node_traces.append(
                    go.Scatter3d(
                        x=[c[0] for c in coords],
                        y=[c[1] for c in coords],
                        z=[c[2] for c in coords],
                        mode='markers',
                        marker=dict(size=5, color='lightblue'),
                        text=[f"Segment: {n}" for n in pipeline_nodes],
                        hoverinfo='text',
                        name='Pipeline Segments'
                    )
                )
            
            # Defects
            defect_nodes = [n for n, d in self.graph.nodes(data=True)
                           if d.get('label') == 'Defect']
            if defect_nodes:
                coords = [node_coords.get(n, (0, 0, 0)) for n in defect_nodes]
                severities = [self.graph.nodes[n].get('severity', 5) for n in defect_nodes]
                
                node_traces.append(
                    go.Scatter3d(
                        x=[c[0] for c in coords],
                        y=[c[1] for c in coords],
                        z=[c[2] for c in coords],
                        mode='markers',
                        marker=dict(
                            size=[s * 2 for s in severities],
                            color=severities,
                            colorscale='Reds',
                            showscale=True,
                            colorbar=dict(title="Severity")
                        ),
                        text=[f"Defect: {n}<br>Severity: {s}" 
                              for n, s in zip(defect_nodes, severities)],
                        hoverinfo='text',
                        name='Defects'
                    )
                )
            
            # Buildings
            building_nodes = [n for n, d in self.graph.nodes(data=True)
                             if d.get('label') == 'Building']
            if building_nodes:
                coords = [node_coords.get(n, (0, 0, 0)) for n in building_nodes]
                colors = ['green' if self.graph.nodes[n].get('is_critical', False)
                         else 'gray' for n in building_nodes]
                
                node_traces.append(
                    go.Scatter3d(
                        x=[c[0] for c in coords],
                        y=[c[1] for c in coords],
                        z=[c[2] for c in coords],
                        mode='markers',
                        marker=dict(size=8, color=colors, symbol='square'),
                        text=[f"Building: {self.graph.nodes[n].get('name', n)}" 
                              for n in building_nodes],
                        hoverinfo='text',
                        name='Buildings'
                    )
                )
            
            # Create figure
            fig = go.Figure(data=edge_traces + node_traces)
            
            fig.update_layout(
                title="3D Pipeline Network Visualization",
                scene=dict(
                    xaxis_title="Longitude",
                    yaxis_title="Latitude",
                    zaxis_title="Elevation",
                    camera=dict(
                        eye=dict(x=1.5, y=1.5, z=1.5)
                    )
                ),
                showlegend=True,
                hovermode='closest',
                width=1200,
                height=800
            )
            
            # Save
            output_path = self.output_dir / "network_3d.html"
            fig.write_html(str(output_path))
            
            logger.info(f"3D visualization saved to {output_path}")
            
            return str(output_path)
            
        except Exception as e:
            logger.error(f"Failed to create 3D visualization: {str(e)}")
            raise
    
    def analyze_network_metrics(self) -> Dict[str, Any]:
        """Calculate network analysis metrics"""
        try:
            logger.info("Calculating network metrics")
            
            metrics = {
                'total_nodes': self.graph.number_of_nodes(),
                'total_edges': self.graph.number_of_edges(),
                'network_density': nx.density(self.graph),
                'average_degree': sum(dict(self.graph.degree()).values()) / self.graph.number_of_nodes()
                    if self.graph.number_of_nodes() > 0 else 0
            }
            
            # Connected components
            if self.graph.is_directed():
                metrics['weakly_connected_components'] = nx.number_weakly_connected_components(self.graph)
            
            # Critical nodes (high betweenness centrality)
            if self.graph.number_of_nodes() > 0:
                betweenness = nx.betweenness_centrality(self.graph)
                metrics['critical_nodes'] = sorted(
                    betweenness.items(),
                    key=lambda x: x[1],
                    reverse=True
                )[:10]
            
            # Defect statistics
            defect_nodes = [n for n, d in self.graph.nodes(data=True)
                           if d.get('label') == 'Defect']
            metrics['total_defects'] = len(defect_nodes)
            
            if defect_nodes:
                severities = [self.graph.nodes[n].get('severity', 0) for n in defect_nodes]
                metrics['average_severity'] = np.mean(severities)
                metrics['max_severity'] = max(severities)
            
            logger.info(f"Network metrics calculated: {metrics}")
            
            return metrics
            
        except Exception as e:
            logger.error(f"Failed to calculate metrics: {str(e)}")
            return {}
    
    # Helper methods
    
    def _parse_vertex(self, vertex_data: Dict) -> Dict[str, Any]:
        """Parse vertex data"""
        parsed = {}
        for key, value in vertex_data.items():
            if isinstance(value, list) and len(value) > 0:
                parsed[key] = value[0]
            else:
                parsed[key] = value
        return parsed
    
    def _calculate_center(self) -> Tuple[float, float]:
        """Calculate map center from node coordinates"""
        lats, lons = [], []
        
        for node, data in self.graph.nodes(data=True):
            lat = data.get('latitude', data.get('start_lat'))
            lon = data.get('longitude', data.get('start_lon'))
            
            if lat and lon:
                lats.append(lat)
                lons.append(lon)
        
        if lats and lons:
            return np.mean(lats), np.mean(lons)
        else:
            return 0.0, 0.0  # Default
    
    def _add_segment_to_map(self, layer, node):
        """Add pipeline segment to map"""
        data = node[1]
        start = [data.get('start_lat'), data.get('start_lon')]
        end = [data.get('end_lat'), data.get('end_lon')]
        
        if all(start) and all(end):
            color = 'green' if data.get('status') == 'operational' else 'red'
            
            folium.PolyLine(
                locations=[start, end],
                color=color,
                weight=3,
                opacity=0.7,
                popup=f"Segment: {node[0]}<br>Status: {data.get('status')}"
            ).add_to(layer)
    
    def _add_defect_to_map(self, layer, node):
        """Add defect marker to map"""
        data = node[1]
        lat = data.get('latitude')
        lon = data.get('longitude')
        
        if lat and lon:
            defect_type = data.get('defect_type', 'unknown')
            severity = data.get('severity', 5)
            
            # Icon and color based on defect type and severity
            icon_color = 'red' if severity >= 7 else 'orange' if severity >= 4 else 'yellow'
            icon = 'exclamation-triangle'
            
            popup_html = f"""
                <b>Defect ID:</b> {node[0]}<br>
                <b>Type:</b> {defect_type}<br>
                <b>Severity:</b> {severity}/10<br>
                <b>Confidence:</b> {data.get('confidence', 0):.2f}<br>
                <b>Detection:</b> {data.get('fusion_type', 'unknown')}
            """
            
            folium.Marker(
                location=[lat, lon],
                popup=folium.Popup(popup_html, max_width=300),
                icon=folium.Icon(color=icon_color, icon=icon, prefix='fa'),
                tooltip=f"Severity: {severity}"
            ).add_to(layer)
    
    def _add_building_to_map(self, layer, node):
        """Add building marker to map"""
        data = node[1]
        lat = data.get('latitude')
        lon = data.get('longitude')
        
        if lat and lon:
            is_critical = data.get('is_critical', False)
            color = 'green' if is_critical else 'blue'
            
            folium.Marker(
                location=[lat, lon],
                popup=f"Building: {data.get('name', node[0])}<br>Type: {data.get('type')}",
                icon=folium.Icon(color=color, icon='building', prefix='fa')
            ).add_to(layer)
    
    def _add_defect_heatmap(self, map_obj):
        """Add heatmap of defect density"""
        defect_locations = []
        
        for node, data in self.graph.nodes(data=True):
            if data.get('label') == 'Defect':
                lat = data.get('latitude')
                lon = data.get('longitude')
                severity = data.get('severity', 5)
                
                if lat and lon:
                    # Add point multiple times based on severity
                    for _ in range(severity):
                        defect_locations.append([lat, lon])
        
        if defect_locations:
            plugins.HeatMap(
                defect_locations,
                name='Defect Density',
                radius=15,
                blur=25,
                max_zoom=13
            ).add_to(map_obj)
    
    def _add_map_legend(self, map_obj):
        """Add legend to map"""
        legend_html = '''
        <div style="position: fixed; 
                    bottom: 50px; right: 50px; width: 200px; height: auto; 
                    background-color: white; z-index:9999; font-size:14px;
                    border:2px solid grey; border-radius: 5px; padding: 10px">
        <p style="margin-bottom:5px;"><b>Legend</b></p>
        <p style="margin:3px;"><span style="color:green;">━━━</span> Operational Pipeline</p>
        <p style="margin:3px;"><span style="color:red;">━━━</span> Damaged Pipeline</p>
        <p style="margin:3px;"><i class="fa fa-exclamation-triangle" style="color:red;"></i> Critical Defect</p>
        <p style="margin:3px;"><i class="fa fa-building" style="color:green;"></i> Critical Building</p>
        </div>
        '''
        map_obj.get_root().html.add_child(folium.Element(legend_html))
    
    def _hierarchical_layout(self) -> Dict:
        """Create hierarchical layout for directed graphs"""
        # Simple hierarchical layout based on topological sort
        try:
            layers = list(nx.topological_generations(self.graph))
            pos = {}
            
            for layer_idx, layer in enumerate(layers):
                y = -layer_idx
                for node_idx, node in enumerate(layer):
                    x = node_idx - len(layer) / 2
                    pos[node] = (x, y)
            
            return pos
        except:
            # Fallback to spring layout if not DAG
            return nx.spring_layout(self.graph)
