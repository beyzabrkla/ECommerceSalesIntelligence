using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Models.Classification;
using ECommerceSalesIntelligence.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSalesIntelligence.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesIntelligenceController : ControllerBase
    {
        private readonly ForecastingService _forecastingService;
        private readonly BinaryClassificationService _binaryClassificationService;
        private readonly MulticlassClassificationService _multiclassService;
        private readonly AnomalyDetectionService _anomalyService;
        private readonly ClusteringService _clusteringService;
        private readonly AppDbContext _context;

        public SalesIntelligenceController(
            ForecastingService forecastingService,
            BinaryClassificationService binaryClassificationService,
            MulticlassClassificationService multiclassService,
            AnomalyDetectionService anomalyService,
            AppDbContext context,
            ClusteringService clusteringService)
        {
            _forecastingService = forecastingService;
            _binaryClassificationService = binaryClassificationService;
            _multiclassService = multiclassService;
            _anomalyService = anomalyService;
            _context = context;
            _clusteringService = clusteringService;
        }

        // Belirli bir şehrin gelecek 7 günlük satış tahminini alır
        [HttpGet("forecast")]
        public async Task<ActionResult<SalesPrediction>> GetSalesForecast([FromQuery] string city, [FromQuery] int horizon = 7, [FromQuery] float confidenceLevel = 0.95f)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { message = "Şehir adı boş olamaz!" });
                }

                var prediction = await _forecastingService.PredictNextDaysAsync(city, horizon, confidenceLevel);
                return Ok(prediction);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FORECAST HATASI:");
                Console.WriteLine(ex.ToString());

                return StatusCode(500, new
                {
                    message = "Tahmin oluşturulurken hata oluştu.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("cities")]
        public async Task<ActionResult<List<string>>> GetCities()
        {
            var count = await _context.SalesRecords.CountAsync();
            Console.WriteLine($"Veritabanındaki toplam kayıt sayısı: {count}");

            var cities = await _context.SalesRecords
                .AsNoTracking()
                .Select(s => s.City)
                .Distinct()
                .ToListAsync();

            return Ok(cities);
        }

        // --- BİNARY CLASSIFICATION (İkili Sınıflandırma ve Şehir Listesi Dashboard Verisi) ---
        [HttpGet("binary-dashboard")]
        public ActionResult<BinaryDashboardViewModel> GetBinaryDashboard()
        {
            try
            {
                var dashboardData = _binaryClassificationService.GetBinaryDashboardData();
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Binary dashboard verileri yüklenirken hata oluştu.", error = ex.Message });
            }
        }

        // --- MULTICLASS CLASSIFICATION (Çok Sınıflı Kategorizasyon) ---
        [HttpGet("multiclass-dashboard")]
        public ActionResult RunMulticlassClassification()
        {
            try
            {
                var predictions = _multiclassService.GetMulticlassDashboardData();
                return Ok(predictions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Multiclass classification sırasında hata oluştu.", error = ex.Message });
            }
        }

        // Anomaly Detection (Sıradışı satış günleri) tespiti yapar
        [HttpGet("anomalies")]
        public ActionResult<List<SalesAnomalyResultViewModel>> GetAnomalies()
        {
            var anomalies = _anomalyService.DetectAnomalies();
            return Ok(anomalies);
        }

        // Clustering (Kümeleme) modelini çalıştırır
        [HttpGet("clusters")]
        public ActionResult<List<ClusterResultViewModel>> GetClusters([FromQuery] int count = 3)
        {
            try
            {
                var clusters = _clusteringService.TrainAndCluster(count);
                return Ok(clusters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kümeleme yapılırken hata oluştu.", error = ex.Message });
            }
        }
    }
}