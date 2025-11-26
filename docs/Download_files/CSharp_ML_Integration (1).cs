// Infrastructure/ML/MLServiceClient.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BahyWay.WPDD.Infrastructure.ML
{
    /// <summary>
    /// Client for interacting with Python ML Service (SPy + YOLOv8 + TinkerPop)
    /// </summary>
    public class MLServiceClient : IMLServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MLServiceClient> _logger;
        private readonly string _baseUrl;

        public MLServiceClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<MLServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["ML_SERVICE_URL"] ?? "http://localhost:8000";
            
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // ML operations can take time
        }

        /// <summary>
        /// Detect defects using multi-modal approach (RGB + Hyperspectral)
        /// </summary>
        public async Task<List<DetectionResult>> DetectMultiModalAsync(
            string rgbImagePath,
            string hyperspectralImagePath,
            string metadata = null)
        {
            try
            {
                _logger.LogInformation("Starting multi-modal detection");

                using var form = new MultipartFormDataContent();
                
                // Add RGB image
                var rgbContent = new ByteArrayContent(await File.ReadAllBytesAsync(rgbImagePath));
                rgbContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/tiff");
                form.Add(rgbContent, "rgb_image", Path.GetFileName(rgbImagePath));

                // Add hyperspectral image
                var hyperContent = new ByteArrayContent(await File.ReadAllBytesAsync(hyperspectralImagePath));
                hyperContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                form.Add(hyperContent, "hyperspectral_image", Path.GetFileName(hyperspectralImagePath));

                // Add metadata if provided
                if (!string.IsNullOrEmpty(metadata))
                {
                    form.Add(new StringContent(metadata), "metadata");
                }

                var response = await _httpClient.PostAsync("/api/detect/multi-modal", form);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var detections = JsonSerializer.Deserialize<List<DetectionResult>>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation($"Multi-modal detection complete: {detections?.Count ?? 0} detections");

                return detections ?? new List<DetectionResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Multi-modal detection failed");
                throw;
            }
        }

        /// <summary>
        /// Detect defects using satellite imagery only
        /// </summary>
        public async Task<List<DetectionResult>> DetectSatelliteOnlyAsync(
            string satelliteImagePath,
            string areaBounds = null)
        {
            try
            {
                _logger.LogInformation("Starting satellite-only detection");

                using var form = new MultipartFormDataContent();
                
                var imageContent = new ByteArrayContent(await File.ReadAllBytesAsync(satelliteImagePath));
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/tiff");
                form.Add(imageContent, "satellite_image", Path.GetFileName(satelliteImagePath));

                if (!string.IsNullOrEmpty(areaBounds))
                {
                    form.Add(new StringContent(areaBounds), "area_bounds");
                }

                var response = await _httpClient.PostAsync("/api/detect/satellite-only", form);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var detections = JsonSerializer.Deserialize<List<DetectionResult>>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation($"Satellite detection complete: {detections?.Count ?? 0} detections");

                return detections ?? new List<DetectionResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satellite detection failed");
                throw;
            }
        }

        /// <summary>
        /// Perform war zone damage assessment (before/after comparison)
        /// </summary>
        public async Task<DamageAssessmentResult> AssessWarZoneDamageAsync(
            string areaId,
            string beforeImagePath,
            string afterImagePath,
            string hyperspectralAfterPath = null)
        {
            try
            {
                _logger.LogInformation($"Starting war zone assessment for area: {areaId}");

                using var form = new MultipartFormDataContent();
                
                // Add area ID
                form.Add(new StringContent(areaId), "area_id");

                // Add before image
                var beforeContent = new ByteArrayContent(await File.ReadAllBytesAsync(beforeImagePath));
                beforeContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/tiff");
                form.Add(beforeContent, "before_image", Path.GetFileName(beforeImagePath));

                // Add after image
                var afterContent = new ByteArrayContent(await File.ReadAllBytesAsync(afterImagePath));
                afterContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/tiff");
                form.Add(afterContent, "after_image", Path.GetFileName(afterImagePath));

                // Add hyperspectral if available
                if (!string.IsNullOrEmpty(hyperspectralAfterPath))
                {
                    var hyperContent = new ByteArrayContent(await File.ReadAllBytesAsync(hyperspectralAfterPath));
                    hyperContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    form.Add(hyperContent, "hyperspectral_after", Path.GetFileName(hyperspectralAfterPath));
                }

                var response = await _httpClient.PostAsync("/api/warzone/damage-assessment", form);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var assessment = JsonSerializer.Deserialize<DamageAssessmentResult>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation($"Damage assessment complete: {assessment.TotalChanges} changes detected");

                return assessment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "War zone damage assessment failed");
                throw;
            }
        }

        /// <summary>
        /// Query graph database for critical defects
        /// </summary>
        public async Task<GraphQueryResult> QueryGraphAsync(string queryType, Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation($"Executing graph query: {queryType}");

                var request = new
                {
                    query_type = queryType,
                    parameters = parameters
                };

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/graph/query", content);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GraphQueryResult>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Graph query failed: {queryType}");
                throw;
            }
        }

        /// <summary>
        /// Get network analysis and health metrics
        /// </summary>
        public async Task<NetworkAnalysis> GetNetworkAnalysisAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/graph/network-analysis");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var analysis = JsonSerializer.Deserialize<NetworkAnalysis>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Network analysis failed");
                throw;
            }
        }

        /// <summary>
        /// Generate and download interactive network map
        /// </summary>
        public async Task<byte[]> GenerateNetworkMapAsync(string areaId = null, bool includeDefects = true)
        {
            try
            {
                var url = $"/api/visualize/network-map?include_defects={includeDefects}";
                if (!string.IsNullOrEmpty(areaId))
                {
                    url += $"&area_id={areaId}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Network map generation failed");
                throw;
            }
        }

        /// <summary>
        /// Generate network topology visualization
        /// </summary>
        public async Task<byte[]> GenerateTopologyVisualizationAsync(string areaId = null, string layout = "spring")
        {
            try
            {
                var url = $"/api/visualize/network-topology?layout={layout}";
                if (!string.IsNullOrEmpty(areaId))
                {
                    url += $"&area_id={areaId}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Topology visualization failed");
                throw;
            }
        }

        /// <summary>
        /// Check ML service health
        /// </summary>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    // DTOs for ML Service Communication

    public class DetectionResult
    {
        public string DetectionId { get; set; }
        public List<double> Bbox { get; set; }
        public GeoCoordinate GeoCoordinates { get; set; }
        public string DefectType { get; set; }
        public double VisualConfidence { get; set; }
        public double SpectralConfidence { get; set; }
        public double CombinedConfidence { get; set; }
        public int Severity { get; set; }
        public List<double> SpectralSignature { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class GeoCoordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string CoordinateSystem { get; set; }
    }

    public class DamageAssessmentResult
    {
        public string AreaId { get; set; }
        public int TotalChanges { get; set; }
        public int NewDefects { get; set; }
        public int AffectedPopulation { get; set; }
        public int CriticalInfrastructureAtRisk { get; set; }
        public List<RepairPriority> PriorityRepairs { get; set; }
        public List<ChangeDetection> Changes { get; set; }
    }

    public class ChangeDetection
    {
        public string ChangeId { get; set; }
        public string ChangeType { get; set; }
        public DetectionResult Before { get; set; }
        public DetectionResult After { get; set; }
        public int SeverityChange { get; set; }
        public string Timestamp { get; set; }
    }

    public class GraphQueryResult
    {
        public Dictionary<string, object> Data { get; set; }
        public int ResultCount { get; set; }
    }

    public class NetworkAnalysis
    {
        public int TotalSegments { get; set; }
        public int TotalDefects { get; set; }
        public int CriticalDefects { get; set; }
        public int AffectedPopulation { get; set; }
        public double NetworkHealthScore { get; set; }
        public List<RepairPriority> PriorityRepairs { get; set; }
    }

    public class RepairPriority
    {
        public int PriorityRank { get; set; }
        public string DefectId { get; set; }
        public int Severity { get; set; }
        public double Confidence { get; set; }
        public int EstimatedImpact { get; set; }
    }

    // Interface
    public interface IMLServiceClient
    {
        Task<List<DetectionResult>> DetectMultiModalAsync(
            string rgbImagePath,
            string hyperspectralImagePath,
            string metadata = null);
        
        Task<List<DetectionResult>> DetectSatelliteOnlyAsync(
            string satelliteImagePath,
            string areaBounds = null);
        
        Task<DamageAssessmentResult> AssessWarZoneDamageAsync(
            string areaId,
            string beforeImagePath,
            string afterImagePath,
            string hyperspectralAfterPath = null);
        
        Task<GraphQueryResult> QueryGraphAsync(
            string queryType,
            Dictionary<string, object> parameters);
        
        Task<NetworkAnalysis> GetNetworkAnalysisAsync();
        
        Task<byte[]> GenerateNetworkMapAsync(
            string areaId = null,
            bool includeDefects = true);
        
        Task<byte[]> GenerateTopologyVisualizationAsync(
            string areaId = null,
            string layout = "spring");
        
        Task<bool> CheckHealthAsync();
    }
}

// Application/Commands/ProcessSatelliteImage/ProcessSatelliteImageCommand.cs
using MediatR;
using System.Collections.Generic;

namespace BahyWay.WPDD.Application.Commands.ProcessSatelliteImage
{
    public class ProcessSatelliteImageCommand : IRequest<ProcessSatelliteImageResult>
    {
        public string RgbImagePath { get; set; }
        public string HyperspectralImagePath { get; set; }
        public string AreaId { get; set; }
        public string Metadata { get; set; }
    }

    public class ProcessSatelliteImageResult
    {
        public List<DetectionResult> Detections { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}

// Application/Commands/ProcessSatelliteImage/ProcessSatelliteImageHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using BahyWay.WPDD.Infrastructure.ML;

namespace BahyWay.WPDD.Application.Commands.ProcessSatelliteImage
{
    public class ProcessSatelliteImageHandler 
        : IRequestHandler<ProcessSatelliteImageCommand, ProcessSatelliteImageResult>
    {
        private readonly IMLServiceClient _mlClient;
        private readonly ILogger<ProcessSatelliteImageHandler> _logger;

        public ProcessSatelliteImageHandler(
            IMLServiceClient mlClient,
            ILogger<ProcessSatelliteImageHandler> logger)
        {
            _mlClient = mlClient;
            _logger = logger;
        }

        public async Task<ProcessSatelliteImageResult> Handle(
            ProcessSatelliteImageCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Processing satellite imagery for area: {request.AreaId}");

                // Call ML service
                var detections = await _mlClient.DetectMultiModalAsync(
                    request.RgbImagePath,
                    request.HyperspectralImagePath,
                    request.Metadata
                );

                _logger.LogInformation($"Detection complete: {detections.Count} defects found");

                return new ProcessSatelliteImageResult
                {
                    Detections = detections,
                    Success = true,
                    Message = $"Successfully detected {detections.Count} defects"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satellite image processing failed");
                
                return new ProcessSatelliteImageResult
                {
                    Success = false,
                    Message = $"Processing failed: {ex.Message}"
                };
            }
        }
    }
}

// API/Controllers/DetectionController.cs
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Threading.Tasks;
using BahyWay.WPDD.Application.Commands.ProcessSatelliteImage;

namespace BahyWay.WPDD.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DetectionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMLServiceClient _mlClient;

        public DetectionController(IMediator mediator, IMLServiceClient mlClient)
        {
            _mediator = mediator;
            _mlClient = mlClient;
        }

        [HttpPost("process-satellite")]
        public async Task<IActionResult> ProcessSatellite([FromBody] ProcessSatelliteImageCommand command)
        {
            var result = await _mediator.Send(command);
            
            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }

        [HttpGet("network-analysis")]
        public async Task<IActionResult> GetNetworkAnalysis()
        {
            var analysis = await _mlClient.GetNetworkAnalysisAsync();
            return Ok(analysis);
        }

        [HttpGet("network-map")]
        public async Task<IActionResult> GetNetworkMap([FromQuery] string areaId = null)
        {
            var mapHtml = await _mlClient.GenerateNetworkMapAsync(areaId);
            return File(mapHtml, "text/html", "network_map.html");
        }

        [HttpPost("war-zone-assessment")]
        public async Task<IActionResult> AssessWarZone(
            [FromQuery] string areaId,
            [FromForm] IFormFile beforeImage,
            [FromForm] IFormFile afterImage,
            [FromForm] IFormFile hyperspectralAfter = null)
        {
            // Save uploaded files temporarily
            var beforePath = Path.Combine(Path.GetTempPath(), beforeImage.FileName);
            var afterPath = Path.Combine(Path.GetTempPath(), afterImage.FileName);
            string hyperPath = null;

            await using (var stream = System.IO.File.Create(beforePath))
                await beforeImage.CopyToAsync(stream);
            
            await using (var stream = System.IO.File.Create(afterPath))
                await afterImage.CopyToAsync(stream);
            
            if (hyperspectralAfter != null)
            {
                hyperPath = Path.Combine(Path.GetTempPath(), hyperspectralAfter.FileName);
                await using var stream = System.IO.File.Create(hyperPath);
                await hyperspectralAfter.CopyToAsync(stream);
            }

            // Call ML service
            var assessment = await _mlClient.AssessWarZoneDamageAsync(
                areaId,
                beforePath,
                afterPath,
                hyperPath
            );

            // Cleanup temp files
            System.IO.File.Delete(beforePath);
            System.IO.File.Delete(afterPath);
            if (hyperPath != null) System.IO.File.Delete(hyperPath);

            return Ok(assessment);
        }
    }
}

// Program.cs - Dependency Injection Setup
services.AddHttpClient<IMLServiceClient, MLServiceClient>();
services.AddScoped<IMLServiceClient, MLServiceClient>();
