"""
YOLOv8 Detector for Pipeline Defect Detection
Handles visual detection from satellite and drone imagery
"""

from ultralytics import YOLO
import numpy as np
from typing import List, Dict, Any, Optional
import logging
import cv2
from pathlib import Path

logger = logging.getLogger(__name__)


class YOLODetector:
    """
    YOLOv8-based detector for pipeline defects
    Detects pipes, leaks, cracks, corrosion from RGB imagery
    """
    
    def __init__(self, model_path: str = "yolov8x.pt"):
        self.model_path = model_path
        self.model = None
        self.loaded = False
        
        # Detection classes
        self.classes = {
            0: 'pipe',
            1: 'faulty_pipe',
            2: 'leak',
            3: 'crack',
            4: 'corrosion',
            5: 'junction',
            6: 'valve'
        }
        
        # Detection parameters
        self.conf_threshold = 0.25
        self.iou_threshold = 0.45
        self.max_det = 300
        
    def load_model(self):
        """Load YOLO model"""
        try:
            logger.info(f"Loading YOLOv8 model from {self.model_path}")
            
            self.model = YOLO(self.model_path)
            self.loaded = True
            
            logger.info("YOLOv8 model loaded successfully")
            
        except Exception as e:
            logger.error(f"Failed to load YOLO model: {str(e)}")
            raise
    
    def is_loaded(self) -> bool:
        """Check if model is loaded"""
        return self.loaded
    
    def detect(
        self,
        image: np.ndarray,
        conf: Optional[float] = None,
        iou: Optional[float] = None
    ) -> List[Dict[str, Any]]:
        """
        Run detection on image
        
        Args:
            image: RGB image as numpy array
            conf: Confidence threshold (optional)
            iou: IoU threshold (optional)
            
        Returns:
            List of detections with bounding boxes and metadata
        """
        if not self.loaded:
            raise RuntimeError("Model not loaded. Call load_model() first.")
        
        try:
            logger.info(f"Running YOLOv8 detection on image: {image.shape}")
            
            # Run inference
            results = self.model.predict(
                image,
                conf=conf or self.conf_threshold,
                iou=iou or self.iou_threshold,
                max_det=self.max_det,
                verbose=False
            )
            
            # Parse results
            detections = self._parse_results(results[0], image.shape)
            
            logger.info(f"YOLOv8 detected {len(detections)} objects")
            
            return detections
            
        except Exception as e:
            logger.error(f"YOLOv8 detection failed: {str(e)}", exc_info=True)
            raise
    
    def detect_tiles(
        self,
        image: np.ndarray,
        tile_size: int = 1024,
        overlap: int = 128
    ) -> List[Dict[str, Any]]:
        """
        Detect on large images using tiled approach
        Essential for satellite imagery
        
        Args:
            image: Large RGB image
            tile_size: Size of each tile
            overlap: Overlap between tiles (for edge detection)
            
        Returns:
            Combined detections from all tiles
        """
        try:
            logger.info(f"Tiled detection: image {image.shape}, tile {tile_size}, overlap {overlap}")
            
            height, width = image.shape[:2]
            all_detections = []
            
            # Calculate tile positions
            stride = tile_size - overlap
            y_positions = range(0, height - tile_size + 1, stride)
            x_positions = range(0, width - tile_size + 1, stride)
            
            # Add final positions to cover edges
            if y_positions[-1] + tile_size < height:
                y_positions = list(y_positions) + [height - tile_size]
            if x_positions[-1] + tile_size < width:
                x_positions = list(x_positions) + [width - tile_size]
            
            total_tiles = len(y_positions) * len(x_positions)
            logger.info(f"Processing {total_tiles} tiles")
            
            # Process each tile
            for tile_idx, y in enumerate(y_positions):
                for x in x_positions:
                    # Extract tile
                    tile = image[y:y+tile_size, x:x+tile_size]
                    
                    # Detect on tile
                    tile_detections = self.detect(tile)
                    
                    # Transform coordinates to global space
                    for det in tile_detections:
                        det['bbox'][0] += x  # x1
                        det['bbox'][1] += y  # y1
                        det['bbox'][2] += x  # x2
                        det['bbox'][3] += y  # y2
                        det['tile_origin'] = (x, y)
                    
                    all_detections.extend(tile_detections)
            
            # Apply Non-Maximum Suppression across tiles
            final_detections = self._global_nms(all_detections)
            
            logger.info(f"Tiled detection complete: {len(final_detections)} objects after NMS")
            
            return final_detections
            
        except Exception as e:
            logger.error(f"Tiled detection failed: {str(e)}")
            raise
    
    def detect_from_path(self, image_path: str) -> List[Dict[str, Any]]:
        """Load image from path and detect"""
        try:
            image = cv2.imread(image_path)
            image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            
            return self.detect(image)
            
        except Exception as e:
            logger.error(f"Detection from path failed: {str(e)}")
            raise
    
    def visualize_detections(
        self,
        image: np.ndarray,
        detections: List[Dict[str, Any]],
        output_path: Optional[str] = None
    ) -> np.ndarray:
        """
        Draw bounding boxes on image
        
        Args:
            image: Input image
            detections: List of detections
            output_path: Optional path to save visualization
            
        Returns:
            Image with drawn boxes
        """
        try:
            vis_image = image.copy()
            
            for det in detections:
                bbox = det['bbox']
                class_name = det['class_name']
                confidence = det['confidence']
                
                # Draw bounding box
                x1, y1, x2, y2 = map(int, bbox)
                
                # Color based on defect type
                color = self._get_color_for_class(class_name)
                
                cv2.rectangle(vis_image, (x1, y1), (x2, y2), color, 2)
                
                # Draw label
                label = f"{class_name}: {confidence:.2f}"
                label_size, _ = cv2.getTextSize(label, cv2.FONT_HERSHEY_SIMPLEX, 0.5, 1)
                
                cv2.rectangle(
                    vis_image,
                    (x1, y1 - label_size[1] - 4),
                    (x1 + label_size[0], y1),
                    color,
                    -1
                )
                
                cv2.putText(
                    vis_image,
                    label,
                    (x1, y1 - 2),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.5,
                    (255, 255, 255),
                    1
                )
            
            if output_path:
                cv2.imwrite(output_path, cv2.cvtColor(vis_image, cv2.COLOR_RGB2BGR))
                logger.info(f"Visualization saved to {output_path}")
            
            return vis_image
            
        except Exception as e:
            logger.error(f"Visualization failed: {str(e)}")
            return image
    
    def train(
        self,
        data_yaml: str,
        epochs: int = 100,
        batch_size: int = 16,
        imgsz: int = 640,
        name: str = "wpdd_model"
    ):
        """
        Train YOLOv8 model on custom dataset
        
        Args:
            data_yaml: Path to data configuration YAML
            epochs: Number of training epochs
            batch_size: Batch size
            imgsz: Image size
            name: Experiment name
        """
        try:
            logger.info(f"Starting YOLOv8 training: {epochs} epochs, batch {batch_size}")
            
            results = self.model.train(
                data=data_yaml,
                epochs=epochs,
                batch=batch_size,
                imgsz=imgsz,
                name=name,
                patience=50,
                save=True,
                device=0,  # GPU
                workers=8,
                optimizer='auto',
                verbose=True,
                val=True,
                plots=True
            )
            
            logger.info("Training complete")
            
            return results
            
        except Exception as e:
            logger.error(f"Training failed: {str(e)}")
            raise
    
    # Private methods
    
    def _parse_results(
        self,
        result,
        image_shape: tuple
    ) -> List[Dict[str, Any]]:
        """Parse YOLO results into structured format"""
        detections = []
        
        if result.boxes is None or len(result.boxes) == 0:
            return detections
        
        boxes = result.boxes.data.cpu().numpy()
        
        for box in boxes:
            x1, y1, x2, y2, conf, cls = box
            
            detection = {
                'bbox': [float(x1), float(y1), float(x2), float(y2)],
                'confidence': float(conf),
                'class_id': int(cls),
                'class_name': self.classes.get(int(cls), 'unknown'),
                'area': float((x2 - x1) * (y2 - y1)),
                'center': [float((x1 + x2) / 2), float((y1 + y2) / 2)],
                'image_shape': image_shape,
                'detection_source': 'yolov8'
            }
            
            detections.append(detection)
        
        return detections
    
    def _global_nms(
        self,
        detections: List[Dict[str, Any]],
        iou_threshold: float = 0.5
    ) -> List[Dict[str, Any]]:
        """
        Apply Non-Maximum Suppression across all detections
        Essential after tiled detection to remove duplicates
        """
        if len(detections) == 0:
            return []
        
        # Extract boxes and scores
        boxes = np.array([det['bbox'] for det in detections])
        scores = np.array([det['confidence'] for det in detections])
        classes = np.array([det['class_id'] for det in detections])
        
        # Apply NMS per class
        keep_indices = []
        
        for class_id in np.unique(classes):
            class_mask = classes == class_id
            class_boxes = boxes[class_mask]
            class_scores = scores[class_mask]
            class_indices = np.where(class_mask)[0]
            
            # NMS using OpenCV
            keep = cv2.dnn.NMSBoxes(
                class_boxes.tolist(),
                class_scores.tolist(),
                self.conf_threshold,
                iou_threshold
            )
            
            if len(keep) > 0:
                keep_indices.extend(class_indices[keep.flatten()].tolist())
        
        # Return filtered detections
        filtered = [detections[i] for i in keep_indices]
        
        logger.debug(f"NMS: {len(detections)} -> {len(filtered)} detections")
        
        return filtered
    
    def _get_color_for_class(self, class_name: str) -> tuple:
        """Get color for visualization based on class"""
        color_map = {
            'pipe': (0, 255, 0),          # Green
            'faulty_pipe': (255, 165, 0), # Orange
            'leak': (255, 0, 0),          # Red
            'crack': (255, 255, 0),       # Yellow
            'corrosion': (139, 69, 19),   # Brown
            'junction': (0, 255, 255),    # Cyan
            'valve': (255, 0, 255)        # Magenta
        }
        
        return color_map.get(class_name, (128, 128, 128))  # Gray default
