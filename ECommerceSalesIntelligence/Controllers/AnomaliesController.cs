using ECommerceSalesIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSalesIntelligence.Controllers
{
    public class AnomaliesController : Controller
    {
        private readonly AnomalyDetectionService _service;

        public AnomaliesController(
            AnomalyDetectionService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var results =
                    await _service.DetectAnomaliesAsync();

                return View(results);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Anomali analizi sırasında hata oluştu.";

                ViewBag.ErrorDetail = ex.Message;

                return View(
                    new List<
                        ECommerceSalesIntelligence.Models
                        .SalesAnomalyResultViewModel>());
            }
        }
    }
}