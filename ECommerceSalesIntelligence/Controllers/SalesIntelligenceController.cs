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
        private readonly ClassificationService _classificationService;
        private readonly MulticlassClassificationService _multiclassService;
        private readonly AnomalyDetectionService _anomalyService;
        private readonly AppDbContext _context;

        public SalesIntelligenceController(
            ForecastingService forecastingService,
            ClassificationService classificationService,
            MulticlassClassificationService multiclassService,
            AnomalyDetectionService anomalyService,
            AppDbContext context)
        {
            _forecastingService = forecastingService;
            _classificationService = classificationService;
            _multiclassService = multiclassService;
            _anomalyService = anomalyService;
            _context = context;
        }

        //Belirli bir şehrin gelecek 7 günlük satış tahminini alır
        [HttpGet("forecast")]
        public async Task<ActionResult<SalesPrediction>> GetSalesForecast([FromQuery] string city, [FromQuery] int horizon = 7, [FromQuery] float confidenceLevel = 0.95f)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new
                    {
                        message = "Şehir adı boş olamaz!"
                    });
                }

                var prediction =
                    await _forecastingService
                        .PredictNextDaysAsync(
                            city,
                            7,
                            0.95f);

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
                .Select(s => s.City)
                .Distinct()
                .ToListAsync();

            return Ok(cities);
        }
        //Binary Classification (Başarılı/Başarısız) modelini çalıştırır
        [HttpPost("classify")]
        public ActionResult<SalesClassificationPrediction> RunClassification()
        {
            var prediction = _classificationService.TrainAndEvaluateAsync();
            return Ok(prediction);
        }

        //Multiclass Classification (Low/Medium/High) modelini çalıştırır
        [HttpPost("multiclass")]
        public ActionResult<SalesMulticlassPrediction> RunMulticlassClassification()
        {
            var prediction = _multiclassService.TrainAndEvaluate();
            return Ok(prediction);
        }

        //Anomaly Detection (Sıradışı satış günleri) tespiti yapar
        [HttpGet("anomalies")]
        public ActionResult<List<SalesAnomalyResultViewModel>> GetAnomalies()
        {
            var anomalies = _anomalyService.DetectAnomalies();
            return Ok(anomalies);
        }
    }
}