using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
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

        // Belirtilen şehir için gelecek günlerin satış tahminini oluşturur
        [HttpGet("forecast")]
        public async Task<ActionResult<SalesPrediction>> GetSalesForecast([FromQuery] string city,[FromQuery] int horizon = 7, [FromQuery] float confidenceLevel = 0.95f)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new
                    {
                        message = "Şehir adı boş olamaz."
                    });
                }

                city = city.Trim();

                if (horizon < 1 || horizon > 30)
                {
                    return BadRequest(new
                    {
                        message = "Tahmin günü 1 ile 30 arasında olmalıdır."
                    });
                }

                if (confidenceLevel <= 0 || confidenceLevel >= 1)
                {
                    return BadRequest(new
                    {
                        message = "Confidence level 0 ile 1 arasında olmalıdır."
                    });
                }

                var prediction = await _forecastingService
                    .PredictNextDaysAsync(city, horizon, confidenceLevel);

                return Ok(prediction);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new
                {
                    message = "Tahmin modeli çalıştırılırken hata oluştu.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Tahmin oluşturulurken beklenmeyen bir hata oluştu.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // Veritabanındaki benzersiz şehirleri alfabetik olarak getirir
        [HttpGet("cities")]
        public async Task<ActionResult<List<string>>> GetCities()
        {
            try
            {
                var cities = await _context.SalesRecords
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrWhiteSpace(x.City))
                    .Select(x => x.City)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Şehirler alınırken hata oluştu.",
                    error = ex.Message
                });
            }
        }

        // İsteğe bağlı şehir filtresiyle benzersiz ürünleri getirir
        [HttpGet("products")]
        public async Task<ActionResult<List<string>>> GetProducts([FromQuery] string? city = null)
        {
            try
            {
                var query = _context.SalesRecords
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrWhiteSpace(x.ProductName));

                if (!string.IsNullOrWhiteSpace(city))
                {
                    city = city.Trim();
                    query = query.Where(x => x.City == city);
                }

                var products = await query
                    .Select(x => x.ProductName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ürünler alınırken hata oluştu.",
                    error = ex.Message
                });
            }
        }

        // Binary classification modelinden dashboard verilerini getirir
        [HttpGet("binary-dashboard")]
        public ActionResult<ClassificationDashboardViewModel> GetBinaryDashboard()
        {
            try
            {
                var dashboardData = _binaryClassificationService
                    .GetBinaryDashboardData();

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Binary dashboard verileri yüklenirken hata oluştu.",
                    error = ex.Message
                });
            }
        }

        // Satış davranışlarını multiclass classification ile sınıflandırır
        [HttpGet("multiclass-dashboard")]
        public ActionResult RunMulticlassClassification()
        {
            try
            {
                var predictions = _multiclassService
                    .GetMulticlassDashboardData();

                return Ok(predictions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Multiclass classification sırasında hata oluştu.",
                    error = ex.Message
                });
            }
        }

        // Normal davranıştan ciddi şekilde sapan satış günlerini tespit eder
        [HttpGet("anomalies")]
        public async Task<ActionResult<List<SalesAnomalyResultViewModel>>> GetAnomalies()
        {
            try
            {
                var anomalies = await _anomalyService
                    .DetectAnomaliesAsync();

                return Ok(anomalies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Anomali tespiti sırasında hata oluştu.",
                    error = ex.Message
                });
            }
        }

        // Şehirleri satış özelliklerine göre K-Means ile kümelendirir
        [HttpGet("clusters")]
        public async Task<ActionResult<List<ClusterResultViewModel>>> GetClusters(
            [FromQuery] int count = 3)
        {
            try
            {
                if (count < 2)
                {
                    return BadRequest(new
                    {
                        message = "Küme sayısı en az 2 olmalıdır."
                    });
                }

                if (count > 20)
                {
                    return BadRequest(new
                    {
                        message = "Küme sayısı en fazla 20 olabilir."
                    });
                }

                var clusters = await _clusteringService.TrainAndClusterAsync(count);

                return Ok(clusters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Kümeleme yapılırken hata oluştu.",
                    error = ex.Message
                });
            }
        }
    }
}