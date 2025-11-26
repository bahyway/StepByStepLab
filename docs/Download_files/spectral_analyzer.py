"""
Spectral Analyzer using Spectral Python (SPy)
Handles all hyperspectral image processing for pipeline leak detection
"""

import spectral as spy
import numpy as np
from typing import Dict, List, Tuple, Optional, Any
import logging
from pathlib import Path
import json

logger = logging.getLogger(__name__)


class SpectralAnalyzer:
    """
    Analyzes hyperspectral imagery to detect water leaks and pipeline defects
    Uses Spectral Python (SPy) for processing
    """
    
    def __init__(self):
        self.initialized = False
        self.reference_spectra = {}
        
        # Key wavelength bands for water detection (in nanometers)
        self.water_bands = {
            'green': 560,
            'nir': 860,
            'swir1': 1450,
            'swir2': 1940
        }
        
        # Detection thresholds
        self.rx_threshold_percentile = 99  # Top 1% as anomalies
        self.ndwi_leak_threshold = 0.3
        self.ace_confidence_threshold = 0.7
        
    def initialize(self):
        """Load reference spectral signatures"""
        try:
            logger.info("Initializing Spectral Analyzer")
            
            # Load reference spectra for different materials/conditions
            self.reference_spectra = {
                'water_leak': self._create_water_signature(),
                'healthy_pipe': self._create_healthy_pipe_signature(),
                'corroded_pipe': self._create_corrosion_signature(),
                'vegetation': self._create_vegetation_signature(),
                'concrete': self._create_concrete_signature()
            }
            
            self.initialized = True
            logger.info("Spectral Analyzer initialized successfully")
            
        except Exception as e:
            logger.error(f"Failed to initialize Spectral Analyzer: {str(e)}")
            raise
    
    def is_ready(self) -> bool:
        """Check if analyzer is ready"""
        return self.initialized
    
    def analyze(self, hyperspectral_image: np.ndarray) -> Dict[str, Any]:
        """
        Main analysis function - runs all spectral detection algorithms
        
        Args:
            hyperspectral_image: 3D numpy array (rows, cols, bands)
            
        Returns:
            Dictionary with analysis results
        """
        try:
            logger.info(f"Analyzing hyperspectral image: shape {hyperspectral_image.shape}")
            
            # 1. Calculate spectral indices
            ndwi = self.calculate_ndwi(hyperspectral_image)
            ndvi = self.calculate_ndvi(hyperspectral_image)
            
            # 2. Run anomaly detection
            rx_scores = self.detect_anomalies_rx(hyperspectral_image)
            
            # 3. Run target detection
            ace_scores = self.detect_water_ace(hyperspectral_image)
            mf_scores = self.matched_filter_water(hyperspectral_image)
            
            # 4. Identify leak candidates
            leak_candidates = self.identify_leaks(
                ndwi, rx_scores, ace_scores, mf_scores
            )
            
            # 5. Extract spectral signatures for each candidate
            leak_details = self.extract_leak_details(
                hyperspectral_image,
                leak_candidates
            )
            
            # 6. Calculate confidence scores
            confidence_map = self.calculate_confidence_map(
                ndwi, rx_scores, ace_scores, mf_scores
            )
            
            results = {
                'ndwi': ndwi,
                'ndvi': ndvi,
                'rx_scores': rx_scores,
                'ace_scores': ace_scores,
                'mf_scores': mf_scores,
                'leak_candidates': leak_candidates,
                'leak_details': leak_details,
                'confidence_map': confidence_map,
                'num_leaks': len(leak_candidates),
                'analysis_metadata': {
                    'shape': hyperspectral_image.shape,
                    'num_bands': hyperspectral_image.shape[2],
                    'rx_threshold': np.percentile(rx_scores, self.rx_threshold_percentile)
                }
            }
            
            logger.info(f"Analysis complete: {len(leak_candidates)} potential leaks detected")
            
            return results
            
        except Exception as e:
            logger.error(f"Spectral analysis failed: {str(e)}", exc_info=True)
            raise
    
    def calculate_ndwi(self, img: np.ndarray) -> np.ndarray:
        """
        Calculate Normalized Difference Water Index
        NDWI = (Green - NIR) / (Green + NIR)
        High values indicate water presence
        """
        try:
            green_idx = self._find_band_index(img, self.water_bands['green'])
            nir_idx = self._find_band_index(img, self.water_bands['nir'])
            
            green = img[:, :, green_idx].astype(np.float32)
            nir = img[:, :, nir_idx].astype(np.float32)
            
            # Calculate NDWI
            ndwi = (green - nir) / (green + nir + 1e-10)
            
            logger.debug(f"NDWI calculated: min={ndwi.min():.3f}, max={ndwi.max():.3f}")
            
            return ndwi
            
        except Exception as e:
            logger.error(f"NDWI calculation failed: {str(e)}")
            raise
    
    def calculate_ndvi(self, img: np.ndarray) -> np.ndarray:
        """
        Calculate Normalized Difference Vegetation Index
        NDVI = (NIR - Red) / (NIR + Red)
        Used to mask vegetation from analysis
        """
        try:
            red_idx = self._find_band_index(img, 650)
            nir_idx = self._find_band_index(img, self.water_bands['nir'])
            
            red = img[:, :, red_idx].astype(np.float32)
            nir = img[:, :, nir_idx].astype(np.float32)
            
            ndvi = (nir - red) / (nir + red + 1e-10)
            
            return ndvi
            
        except Exception as e:
            logger.error(f"NDVI calculation failed: {str(e)}")
            raise
    
    def detect_anomalies_rx(self, img: np.ndarray) -> np.ndarray:
        """
        RX Anomaly Detector - finds spectrally unusual pixels
        Perfect for detecting leaks that differ from background
        
        Uses SPy's built-in RX detector with local statistics
        """
        try:
            logger.info("Running RX anomaly detection")
            
            # Apply noise reduction first (MNF transform)
            reduced_img = self._reduce_noise_mnf(img)
            
            # Run RX with local window
            rx_scores = spy.rx(
                reduced_img,
                window_inner=3,   # Pixel neighborhood size
                window_outer=25   # Background window size
            )
            
            logger.info(f"RX detection complete: mean={np.mean(rx_scores):.3f}")
            
            return rx_scores
            
        except Exception as e:
            logger.error(f"RX detection failed: {str(e)}")
            # Fallback to simpler method if SPy fails
            return self._simple_anomaly_detection(img)
    
    def detect_water_ace(self, img: np.ndarray) -> np.ndarray:
        """
        Adaptive Coherence/Cosine Estimator (ACE) for water detection
        More robust than matched filter in varying backgrounds
        """
        try:
            logger.info("Running ACE water detection")
            
            water_signature = self.reference_spectra['water_leak']
            
            # Run ACE detector
            ace_scores = spy.ace(img, water_signature)
            
            logger.info(f"ACE detection complete: max={np.max(ace_scores):.3f}")
            
            return ace_scores
            
        except Exception as e:
            logger.error(f"ACE detection failed: {str(e)}")
            # Return zeros if fails
            return np.zeros((img.shape[0], img.shape[1]))
    
    def matched_filter_water(self, img: np.ndarray) -> np.ndarray:
        """
        Matched Filter detector for water targets
        Linear detector using background covariance
        """
        try:
            logger.info("Running Matched Filter water detection")
            
            water_signature = self.reference_spectra['water_leak']
            
            # Run matched filter
            mf_scores = spy.matched_filter(
                img,
                water_signature,
                background=None  # Uses global covariance
            )
            
            logger.info(f"Matched Filter complete: max={np.max(mf_scores):.3f}")
            
            return mf_scores
            
        except Exception as e:
            logger.error(f"Matched Filter failed: {str(e)}")
            return np.zeros((img.shape[0], img.shape[1]))
    
    def identify_leaks(
        self,
        ndwi: np.ndarray,
        rx_scores: np.ndarray,
        ace_scores: np.ndarray,
        mf_scores: np.ndarray
    ) -> List[Dict[str, Any]]:
        """
        Combine all detection methods to identify leak locations
        
        Returns list of leak candidates with coordinates and scores
        """
        try:
            # Create combined detection mask
            height, width = ndwi.shape
            
            # Threshold each method
            rx_threshold = np.percentile(rx_scores, self.rx_threshold_percentile)
            rx_mask = rx_scores > rx_threshold
            
            ndwi_mask = ndwi > self.ndwi_leak_threshold
            ace_mask = ace_scores > self.ace_confidence_threshold
            mf_mask = mf_scores > np.percentile(mf_scores, 95)
            
            # Combine masks (at least 2 methods must agree)
            vote_map = (
                rx_mask.astype(int) +
                ndwi_mask.astype(int) +
                ace_mask.astype(int) +
                mf_mask.astype(int)
            )
            
            leak_mask = vote_map >= 2
            
            # Find connected components (leak regions)
            from scipy import ndimage
            labeled, num_features = ndimage.label(leak_mask)
            
            leak_candidates = []
            
            for i in range(1, num_features + 1):
                # Get region properties
                region_mask = labeled == i
                region_coords = np.where(region_mask)
                
                if len(region_coords[0]) < 5:  # Skip tiny regions
                    continue
                
                # Calculate centroid
                centroid_y = int(np.mean(region_coords[0]))
                centroid_x = int(np.mean(region_coords[1]))
                
                # Calculate bounding box
                min_y, max_y = region_coords[0].min(), region_coords[0].max()
                min_x, max_x = region_coords[1].min(), region_coords[1].max()
                
                # Extract scores for this region
                region_scores = {
                    'rx_mean': float(np.mean(rx_scores[region_mask])),
                    'ndwi_mean': float(np.mean(ndwi[region_mask])),
                    'ace_mean': float(np.mean(ace_scores[region_mask])),
                    'mf_mean': float(np.mean(mf_scores[region_mask]))
                }
                
                # Combined confidence
                combined_confidence = (
                    region_scores['rx_mean'] * 0.25 +
                    region_scores['ace_mean'] * 0.35 +
                    region_scores['mf_mean'] * 0.25 +
                    (region_scores['ndwi_mean'] - self.ndwi_leak_threshold) * 0.15
                )
                
                leak_candidates.append({
                    'id': f"spectral_leak_{i}",
                    'centroid': [centroid_x, centroid_y],
                    'bbox': [min_x, min_y, max_x, max_y],
                    'area_pixels': int(np.sum(region_mask)),
                    'scores': region_scores,
                    'combined_confidence': float(combined_confidence),
                    'detection_methods': int(vote_map[centroid_y, centroid_x])
                })
            
            logger.info(f"Identified {len(leak_candidates)} leak candidates")
            
            return leak_candidates
            
        except Exception as e:
            logger.error(f"Leak identification failed: {str(e)}")
            return []
    
    def extract_leak_details(
        self,
        img: np.ndarray,
        leak_candidates: List[Dict[str, Any]]
    ) -> List[Dict[str, Any]]:
        """
        Extract detailed spectral information for each leak candidate
        """
        detailed_leaks = []
        
        for candidate in leak_candidates:
            try:
                # Extract ROI
                bbox = candidate['bbox']
                roi = img[bbox[1]:bbox[3], bbox[0]:bbox[2], :]
                
                # Calculate mean spectrum
                mean_spectrum = np.mean(roi, axis=(0, 1))
                
                # Classify defect type based on spectral signature
                defect_type = self._classify_defect_type(mean_spectrum)
                
                # Calculate severity
                severity = self._calculate_severity(candidate['scores'])
                
                detailed_leaks.append({
                    **candidate,
                    'mean_spectrum': mean_spectrum.tolist(),
                    'defect_type': defect_type,
                    'severity': severity,
                    'spectral_angle_to_water': float(
                        self._spectral_angle(
                            mean_spectrum,
                            self.reference_spectra['water_leak']
                        )
                    )
                })
                
            except Exception as e:
                logger.warning(f"Failed to extract details for leak {candidate['id']}: {str(e)}")
                detailed_leaks.append(candidate)
        
        return detailed_leaks
    
    def calculate_confidence_map(
        self,
        ndwi: np.ndarray,
        rx_scores: np.ndarray,
        ace_scores: np.ndarray,
        mf_scores: np.ndarray
    ) -> np.ndarray:
        """
        Create a combined confidence map
        """
        # Normalize all scores to 0-1
        rx_norm = (rx_scores - rx_scores.min()) / (rx_scores.max() - rx_scores.min() + 1e-10)
        ndwi_norm = np.clip((ndwi + 1) / 2, 0, 1)  # NDWI is -1 to 1
        ace_norm = np.clip(ace_scores, 0, 1)
        mf_norm = (mf_scores - mf_scores.min()) / (mf_scores.max() - mf_scores.min() + 1e-10)
        
        # Weighted combination
        confidence_map = (
            rx_norm * 0.25 +
            ndwi_norm * 0.15 +
            ace_norm * 0.35 +
            mf_norm * 0.25
        )
        
        return confidence_map
    
    def extract_signatures(
        self,
        image_path: str,
        roi_coords: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Extract spectral signatures from specific ROIs
        Useful for building reference libraries
        """
        try:
            # Load image with SPy
            img = spy.open_image(image_path)
            
            if roi_coords:
                # Parse ROI coordinates
                coords = json.loads(roi_coords)
                x1, y1, x2, y2 = coords['x1'], coords['y1'], coords['x2'], coords['y2']
                roi = img[y1:y2, x1:x2, :]
            else:
                roi = img
            
            # Extract statistics
            mean_spectrum = np.mean(roi, axis=(0, 1))
            std_spectrum = np.std(roi, axis=(0, 1))
            
            return {
                'mean_spectrum': mean_spectrum.tolist(),
                'std_spectrum': std_spectrum.tolist(),
                'shape': roi.shape,
                'wavelengths': self._get_wavelengths(img)
            }
            
        except Exception as e:
            logger.error(f"Signature extraction failed: {str(e)}")
            raise
    
    # Helper methods
    
    def _reduce_noise_mnf(self, img: np.ndarray) -> np.ndarray:
        """Apply Minimum Noise Fraction transform"""
        try:
            # Estimate noise from homogeneous region
            # Use center region as assumed relatively homogeneous
            h, w = img.shape[0], img.shape[1]
            noise_region = img[h//4:3*h//4, w//4:3*w//4, :]
            
            noise = spy.noise_from_diffs(noise_region)
            
            # Apply MNF
            mnf_result = spy.mnf(noise, img)
            
            # Keep top components (preserves signal, removes noise)
            num_components = min(20, img.shape[2])
            transformed = mnf_result.transform(img)
            reduced = transformed[:, :, :num_components]
            
            return reduced
            
        except Exception as e:
            logger.warning(f"MNF failed, using original image: {str(e)}")
            return img
    
    def _find_band_index(self, img: np.ndarray, target_wavelength: float) -> int:
        """Find band index closest to target wavelength"""
        # This is simplified - in production, read from image metadata
        # For now, assume standard band distribution
        num_bands = img.shape[2]
        
        # Typical hyperspectral range: 400-2500 nm
        wavelengths = np.linspace(400, 2500, num_bands)
        idx = np.argmin(np.abs(wavelengths - target_wavelength))
        
        return int(idx)
    
    def _classify_defect_type(self, spectrum: np.ndarray) -> str:
        """Classify defect type based on spectral signature"""
        min_angle = float('inf')
        best_match = 'unknown'
        
        for defect_type, ref_spectrum in self.reference_spectra.items():
            angle = self._spectral_angle(spectrum, ref_spectrum)
            if angle < min_angle:
                min_angle = angle
                best_match = defect_type
        
        # Map to defect categories
        type_mapping = {
            'water_leak': 'leak',
            'corroded_pipe': 'corrosion',
            'healthy_pipe': 'none',
            'vegetation': 'vegetation_interference',
            'concrete': 'surface_material'
        }
        
        return type_mapping.get(best_match, 'unknown')
    
    def _spectral_angle(self, spectrum1: np.ndarray, spectrum2: np.ndarray) -> float:
        """Calculate spectral angle between two spectra"""
        # Ensure same length
        min_len = min(len(spectrum1), len(spectrum2))
        s1 = spectrum1[:min_len]
        s2 = spectrum2[:min_len]
        
        # Spectral Angle Mapper formula
        dot_product = np.dot(s1, s2)
        norm1 = np.linalg.norm(s1)
        norm2 = np.linalg.norm(s2)
        
        cos_angle = dot_product / (norm1 * norm2 + 1e-10)
        angle = np.arccos(np.clip(cos_angle, -1, 1))
        
        return float(angle)
    
    def _calculate_severity(self, scores: Dict[str, float]) -> int:
        """Calculate severity score (1-10)"""
        # Weighted combination of detection scores
        severity = (
            scores['rx_mean'] * 3 +
            scores['ace_mean'] * 4 +
            scores['ndwi_mean'] * 2 +
            scores['mf_mean'] * 1
        ) * 10
        
        return int(np.clip(severity, 1, 10))
    
    def _simple_anomaly_detection(self, img: np.ndarray) -> np.ndarray:
        """Fallback simple anomaly detection"""
        # Use Mahalanobis distance
        img_2d = img.reshape(-1, img.shape[2])
        
        mean = np.mean(img_2d, axis=0)
        cov = np.cov(img_2d.T)
        
        try:
            inv_cov = np.linalg.inv(cov)
            
            distances = np.zeros(img_2d.shape[0])
            for i, pixel in enumerate(img_2d):
                diff = pixel - mean
                distances[i] = np.sqrt(diff @ inv_cov @ diff.T)
            
            return distances.reshape(img.shape[0], img.shape[1])
            
        except:
            return np.zeros((img.shape[0], img.shape[1]))
    
    def _get_wavelengths(self, img) -> List[float]:
        """Get wavelength information from image"""
        if hasattr(img, 'metadata') and 'wavelength' in img.metadata:
            return [float(w) for w in img.metadata['wavelength']]
        else:
            # Return default if not available
            num_bands = img.shape[2] if hasattr(img, 'shape') else 224
            return list(np.linspace(400, 2500, num_bands))
    
    # Reference spectrum creation methods
    
    def _create_water_signature(self) -> np.ndarray:
        """Create reference spectrum for water/moisture"""
        # Simplified water absorption spectrum
        # In production, use measured spectra from spectral library
        wavelengths = np.linspace(400, 2500, 224)
        
        signature = np.ones_like(wavelengths)
        
        # Water absorption peaks
        signature[wavelengths > 1400] *= 0.3  # Strong absorption at 1450nm
        signature[wavelengths > 1900] *= 0.1  # Very strong at 1940nm
        
        return signature
    
    def _create_healthy_pipe_signature(self) -> np.ndarray:
        """Reference spectrum for healthy pipe materials"""
        wavelengths = np.linspace(400, 2500, 224)
        signature = 0.5 + 0.1 * np.sin(wavelengths / 100)
        return signature
    
    def _create_corrosion_signature(self) -> np.ndarray:
        """Reference spectrum for corroded surfaces"""
        wavelengths = np.linspace(400, 2500, 224)
        # Iron oxide has characteristic absorption features
        signature = np.ones_like(wavelengths)
        signature[wavelengths < 600] *= 0.7  # Lower reflectance in blue
        signature[(wavelengths > 800) & (wavelengths < 1000)] *= 0.5
        return signature
    
    def _create_vegetation_signature(self) -> np.ndarray:
        """Reference spectrum for vegetation"""
        wavelengths = np.linspace(400, 2500, 224)
        signature = np.ones_like(wavelengths)
        
        # Vegetation characteristics
        signature[wavelengths < 700] *= 0.3  # Low reflectance in visible
        signature[(wavelengths >= 700) & (wavelengths < 1300)] *= 2.0  # High NIR
        
        return signature
    
    def _create_concrete_signature(self) -> np.ndarray:
        """Reference spectrum for concrete surfaces"""
        wavelengths = np.linspace(400, 2500, 224)
        # Concrete is relatively flat, high reflectance
        signature = 0.7 + 0.05 * np.random.randn(len(wavelengths))
        return np.abs(signature)
