"""
Detection Fusion Engine
Combines YOLOv8 visual detections with SPy spectral analysis
"""

import numpy as np
from typing import List, Dict, Any, Tuple
import logging
from scipy.spatial.distance import cdist
from datetime import datetime
import uuid

logger = logging.getLogger(__name__)


class DetectionFusionEngine:
    """
    Fuses multi-modal detections from visual and spectral sources
    Provides unified, high-confidence defect detection
    """
    
    def __init__(self):
        # Fusion weights
        self.visual_weight = 0.6
        self.spectral_weight = 0.4
        
        # Spatial matching threshold (IoU)
        self.spatial_match_threshold = 0.3
        
        # Confidence thresholds
        self.min_fused_confidence = 0.4
        self.high_confidence_threshold = 0.75
        
    def fuse(
        self,
        yolo_detections: List[Dict[str, Any]],
        spectral_results: Dict[str, Any],
        rgb_image: np.ndarray,
        hyperspectral_image: np.ndarray
    ) -> List[Dict[str, Any]]:
        """
        Main fusion function
        
        Args:
            yolo_detections: Visual detections from YOLOv8
            spectral_results: Spectral analysis from SPy
            rgb_image: RGB image array
            hyperspectral_image: Hyperspectral cube
            
        Returns:
            Fused detections with combined confidence scores
        """
        try:
            logger.info(f"Fusing {len(yolo_detections)} visual + {spectral_results['num_leaks']} spectral detections")
            
            fused_detections = []
            matched_spectral = set()
            
            spectral_leaks = spectral_results.get('leak_details', [])
            
            # 1. Match visual detections with spectral detections
            for yolo_det in yolo_detections:
                # Find spatially overlapping spectral detections
                matches = self._find_spatial_matches(
                    yolo_det,
                    spectral_leaks,
                    rgb_image.shape,
                    hyperspectral_image.shape
                )
                
                if matches:
                    # Fuse with best match
                    best_match = matches[0]
                    matched_spectral.add(best_match['id'])
                    
                    fused = self._fuse_detection_pair(
                        yolo_det,
                        best_match,
                        spectral_results,
                        rgb_image,
                        hyperspectral_image
                    )
                    
                    fused['fusion_type'] = 'visual_spectral'
                    fused_detections.append(fused)
                else:
                    # Visual-only detection
                    fused = self._create_visual_only_detection(
                        yolo_det,
                        rgb_image
                    )
                    fused['fusion_type'] = 'visual_only'
                    fused_detections.append(fused)
            
            # 2. Add unmatched spectral detections
            for spectral_leak in spectral_leaks:
                if spectral_leak['id'] not in matched_spectral:
                    fused = self._create_spectral_only_detection(
                        spectral_leak,
                        spectral_results,
                        hyperspectral_image
                    )
                    fused['fusion_type'] = 'spectral_only'
                    fused_detections.append(fused)
            
            # 3. Filter by confidence
            fused_detections = [
                d for d in fused_detections
                if d['combined_confidence'] >= self.min_fused_confidence
            ]
            
            # 4. Calculate geo-coordinates (if metadata available)
            fused_detections = self._add_geo_coordinates(
                fused_detections,
                rgb_image.shape
            )
            
            # 5. Assign severity levels
            for det in fused_detections:
                det['severity'] = self._calculate_severity(det)
            
            # 6. Sort by priority
            fused_detections.sort(
                key=lambda x: x['combined_confidence'] * x['severity'],
                reverse=True
            )
            
            logger.info(f"Fusion complete: {len(fused_detections)} final detections")
            
            return fused_detections
            
        except Exception as e:
            logger.error(f"Fusion failed: {str(e)}", exc_info=True)
            raise
    
    def detect_changes(
        self,
        before_detections: List[Dict[str, Any]],
        after_detections: List[Dict[str, Any]],
        iou_threshold: float = 0.5
    ) -> List[Dict[str, Any]]:
        """
        Detect changes between before/after imagery
        Essential for war zone damage assessment
        
        Returns:
            List of changes (new damage, repairs, etc.)
        """
        try:
            logger.info(f"Detecting changes: {len(before_detections)} before, {len(after_detections)} after")
            
            changes = []
            matched_after = set()
            
            # Find matching detections
            for before_det in before_detections:
                matches = self._find_matching_detections(
                    before_det,
                    after_detections,
                    iou_threshold
                )
                
                if matches:
                    # Object still exists - check if condition changed
                    after_det = matches[0]
                    matched_after.add(after_det['detection_id'])
                    
                    if self._has_condition_changed(before_det, after_det):
                        changes.append({
                            'change_id': str(uuid.uuid4()),
                            'change_type': 'condition_deteriorated',
                            'before': before_det,
                            'after': after_det,
                            'severity_change': after_det.get('severity', 5) - before_det.get('severity', 5),
                            'timestamp': datetime.utcnow().isoformat()
                        })
                else:
                    # Object disappeared (could be repair or destruction)
                    if before_det['class_name'] in ['faulty_pipe', 'leak']:
                        change_type = 'possible_repair'
                    else:
                        change_type = 'infrastructure_loss'
                    
                    changes.append({
                        'change_id': str(uuid.uuid4()),
                        'change_type': change_type,
                        'before': before_det,
                        'after': None,
                        'timestamp': datetime.utcnow().isoformat()
                    })
            
            # Find new detections (new damage)
            for after_det in after_detections:
                if after_det.get('detection_id') not in matched_after:
                    changes.append({
                        'change_id': str(uuid.uuid4()),
                        'change_type': 'new_damage',
                        'before': None,
                        'after': after_det,
                        'severity': after_det.get('severity', 5),
                        'timestamp': datetime.utcnow().isoformat()
                    })
            
            logger.info(f"Change detection complete: {len(changes)} changes found")
            
            return changes
            
        except Exception as e:
            logger.error(f"Change detection failed: {str(e)}")
            raise
    
    def enhance_with_spectral(
        self,
        changes: List[Dict[str, Any]],
        spectral_results: Dict[str, Any]
    ) -> List[Dict[str, Any]]:
        """
        Enhance change detections with spectral information
        """
        try:
            for change in changes:
                if change['after'] is not None:
                    # Add spectral confidence if available
                    after_det = change['after']
                    
                    # Find matching spectral detection
                    for spectral_leak in spectral_results.get('leak_details', []):
                        if self._is_spatial_overlap(after_det, spectral_leak):
                            change['spectral_confirmation'] = True
                            change['spectral_confidence'] = spectral_leak['combined_confidence']
                            change['defect_type_spectral'] = spectral_leak['defect_type']
                            break
            
            return changes
            
        except Exception as e:
            logger.error(f"Spectral enhancement failed: {str(e)}")
            return changes
    
    # Private methods
    
    def _find_spatial_matches(
        self,
        visual_det: Dict[str, Any],
        spectral_leaks: List[Dict[str, Any]],
        rgb_shape: tuple,
        hyper_shape: tuple
    ) -> List[Dict[str, Any]]:
        """Find spectral detections that spatially overlap with visual detection"""
        matches = []
        
        # Scale factor between RGB and hyperspectral
        scale_y = hyper_shape[0] / rgb_shape[0]
        scale_x = hyper_shape[1] / rgb_shape[1]
        
        visual_bbox = visual_det['bbox']
        visual_bbox_scaled = [
            visual_bbox[0] * scale_x,
            visual_bbox[1] * scale_y,
            visual_bbox[2] * scale_x,
            visual_bbox[3] * scale_y
        ]
        
        for spectral_leak in spectral_leaks:
            spectral_bbox = spectral_leak['bbox']
            
            iou = self._calculate_iou(visual_bbox_scaled, spectral_bbox)
            
            if iou >= self.spatial_match_threshold:
                matches.append({
                    **spectral_leak,
                    'iou_with_visual': iou
                })
        
        # Sort by IoU
        matches.sort(key=lambda x: x['iou_with_visual'], reverse=True)
        
        return matches
    
    def _fuse_detection_pair(
        self,
        visual_det: Dict[str, Any],
        spectral_det: Dict[str, Any],
        spectral_results: Dict[str, Any],
        rgb_image: np.ndarray,
        hyper_image: np.ndarray
    ) -> Dict[str, Any]:
        """Fuse a matched visual-spectral detection pair"""
        
        # Combined confidence
        visual_conf = visual_det['confidence']
        spectral_conf = spectral_det['combined_confidence']
        
        combined_conf = (
            visual_conf * self.visual_weight +
            spectral_conf * self.spectral_weight
        )
        
        # Determine defect type (spectral has priority)
        defect_type = spectral_det.get('defect_type', visual_det['class_name'])
        
        # Create fused detection
        fused = {
            'detection_id': str(uuid.uuid4()),
            'bbox': visual_det['bbox'],  # Use visual bbox (higher resolution)
            'visual_confidence': float(visual_conf),
            'spectral_confidence': float(spectral_conf),
            'combined_confidence': float(combined_conf),
            'defect_type': defect_type,
            'class_name': visual_det['class_name'],
            'area': visual_det['area'],
            'center': visual_det['center'],
            'spectral_signature': spectral_det.get('mean_spectrum', []),
            'spectral_scores': spectral_det.get('scores', {}),
            'detection_methods': {
                'visual': True,
                'spectral': True,
                'methods_agreed': spectral_det['detection_methods']
            },
            'metadata': {
                'visual_class': visual_det['class_name'],
                'spectral_defect_type': spectral_det.get('defect_type'),
                'spectral_area_pixels': spectral_det.get('area_pixels'),
                'iou': spectral_det.get('iou_with_visual', 0),
                'timestamp': datetime.utcnow().isoformat()
            }
        }
        
        return fused
    
    def _create_visual_only_detection(
        self,
        visual_det: Dict[str, Any],
        rgb_image: np.ndarray
    ) -> Dict[str, Any]:
        """Create detection from visual-only source"""
        
        return {
            'detection_id': str(uuid.uuid4()),
            'bbox': visual_det['bbox'],
            'visual_confidence': float(visual_det['confidence']),
            'spectral_confidence': 0.0,
            'combined_confidence': float(visual_det['confidence'] * 0.7),  # Penalty for no spectral
            'defect_type': visual_det['class_name'],
            'class_name': visual_det['class_name'],
            'area': visual_det['area'],
            'center': visual_det['center'],
            'spectral_signature': None,
            'detection_methods': {
                'visual': True,
                'spectral': False
            },
            'metadata': {
                'visual_class': visual_det['class_name'],
                'timestamp': datetime.utcnow().isoformat()
            }
        }
    
    def _create_spectral_only_detection(
        self,
        spectral_det: Dict[str, Any],
        spectral_results: Dict[str, Any],
        hyper_image: np.ndarray
    ) -> Dict[str, Any]:
        """Create detection from spectral-only source"""
        
        return {
            'detection_id': str(uuid.uuid4()),
            'bbox': spectral_det['bbox'],
            'visual_confidence': 0.0,
            'spectral_confidence': float(spectral_det['combined_confidence']),
            'combined_confidence': float(spectral_det['combined_confidence'] * 0.7),  # Penalty for no visual
            'defect_type': spectral_det.get('defect_type', 'unknown'),
            'class_name': 'spectral_anomaly',
            'area': spectral_det.get('area_pixels', 0),
            'center': spectral_det['centroid'],
            'spectral_signature': spectral_det.get('mean_spectrum', []),
            'spectral_scores': spectral_det.get('scores', {}),
            'detection_methods': {
                'visual': False,
                'spectral': True,
                'methods_agreed': spectral_det['detection_methods']
            },
            'metadata': {
                'spectral_defect_type': spectral_det.get('defect_type'),
                'spectral_area_pixels': spectral_det.get('area_pixels'),
                'timestamp': datetime.utcnow().isoformat()
            }
        }
    
    def _calculate_iou(self, bbox1: List[float], bbox2: List[float]) -> float:
        """Calculate Intersection over Union"""
        x1_min, y1_min, x1_max, y1_max = bbox1
        x2_min, y2_min, x2_max, y2_max = bbox2
        
        # Calculate intersection
        x_inter_min = max(x1_min, x2_min)
        y_inter_min = max(y1_min, y2_min)
        x_inter_max = min(x1_max, x2_max)
        y_inter_max = min(y1_max, y2_max)
        
        if x_inter_max < x_inter_min or y_inter_max < y_inter_min:
            return 0.0
        
        inter_area = (x_inter_max - x_inter_min) * (y_inter_max - y_inter_min)
        
        # Calculate union
        bbox1_area = (x1_max - x1_min) * (y1_max - y1_min)
        bbox2_area = (x2_max - x2_min) * (y2_max - y2_min)
        union_area = bbox1_area + bbox2_area - inter_area
        
        iou = inter_area / (union_area + 1e-10)
        
        return float(iou)
    
    def _add_geo_coordinates(
        self,
        detections: List[Dict[str, Any]],
        image_shape: tuple
    ) -> List[Dict[str, Any]]:
        """Add geographic coordinates to detections"""
        # This would use GDAL/rasterio to get actual geo-coords from GeoTIFF
        # For now, add placeholder
        
        for det in detections:
            center = det['center']
            
            # Placeholder coordinates
            det['geo_coordinates'] = {
                'latitude': 0.0,  # Would calculate from image metadata
                'longitude': 0.0,
                'coordinate_system': 'WGS84',
                'pixel_coordinates': center
            }
        
        return detections
    
    def _calculate_severity(self, detection: Dict[str, Any]) -> int:
        """Calculate severity score (1-10)"""
        
        base_severity = 5
        
        # Adjust based on defect type
        type_severity = {
            'leak': 8,
            'crack': 6,
            'corrosion': 5,
            'faulty_pipe': 7,
            'pipe': 0
        }
        
        defect_type = detection['defect_type']
        severity = type_severity.get(defect_type, base_severity)
        
        # Adjust based on confidence
        confidence = detection['combined_confidence']
        if confidence > 0.8:
            severity += 1
        
        # Adjust based on detection methods
        if detection['detection_methods']['visual'] and detection['detection_methods']['spectral']:
            severity += 1  # High confidence when both methods agree
        
        # Adjust based on spectral scores (if available)
        spectral_scores = detection.get('spectral_scores', {})
        if spectral_scores:
            if spectral_scores.get('ace_mean', 0) > 0.8:
                severity += 1
        
        return min(max(severity, 1), 10)
    
    def _find_matching_detections(
        self,
        reference: Dict[str, Any],
        candidates: List[Dict[str, Any]],
        iou_threshold: float
    ) -> List[Dict[str, Any]]:
        """Find detections that match spatially"""
        matches = []
        
        ref_bbox = reference['bbox']
        
        for candidate in candidates:
            iou = self._calculate_iou(ref_bbox, candidate['bbox'])
            
            if iou >= iou_threshold:
                matches.append(candidate)
        
        matches.sort(key=lambda x: self._calculate_iou(ref_bbox, x['bbox']), reverse=True)
        
        return matches
    
    def _has_condition_changed(
        self,
        before: Dict[str, Any],
        after: Dict[str, Any]
    ) -> bool:
        """Check if detection condition has changed significantly"""
        
        # Check if defect type changed
        if before['class_name'] != after['class_name']:
            return True
        
        # Check if severity changed significantly
        before_sev = before.get('severity', 5)
        after_sev = after.get('severity', 5)
        
        if abs(after_sev - before_sev) >= 2:
            return True
        
        # Check if confidence changed significantly
        before_conf = before.get('combined_confidence', before.get('confidence', 0.5))
        after_conf = after.get('combined_confidence', after.get('confidence', 0.5))
        
        if abs(after_conf - before_conf) >= 0.2:
            return True
        
        return False
    
    def _is_spatial_overlap(
        self,
        det1: Dict[str, Any],
        det2: Dict[str, Any]
    ) -> bool:
        """Check if two detections overlap spatially"""
        bbox1 = det1.get('bbox', [])
        bbox2 = det2.get('bbox', [])
        
        if not bbox1 or not bbox2:
            return False
        
        iou = self._calculate_iou(bbox1, bbox2)
        
        return iou > 0.1  # Any overlap
