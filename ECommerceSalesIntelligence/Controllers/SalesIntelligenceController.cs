using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

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

        public SalesIntelligenceController(
            ForecastingService forecastingService,
            ClassificationService classificationService,
            MulticlassClassificationService multiclassService,
            AnomalyDetectionService anomalyService)
        {
            _forecastingService = forecastingService;
            _classificationService = classificationService;
            _multiclassService = multiclassService;
            _anomalyService = anomalyService;
        }

        //Belirli bir şehrin gelecek 7 günlük satış tahminini alır
        [HttpGet("forecast")]
        public async Task<ActionResult<SalesPrediction>> GetSalesForecast([FromQuery] string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return BadRequest(new { message = "Şehir adı boş olamaz!" });
            }

            var prediction = await _forecastingService.PredictNext7DaysAsync(city);
            return Ok(prediction);
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