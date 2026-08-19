using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSalesIntelligence.Controllers
{
    public class MulticlassClassificationController : Controller
    {
        private readonly MulticlassClassificationService _multiclassService;

        public MulticlassClassificationController(MulticlassClassificationService multiclassService)
        {
            _multiclassService = multiclassService;
        }

        public IActionResult Index()
        {
            var (metrics, predictions) = _multiclassService.GetMulticlassDashboardData();

            var viewModel = new MulticlassClassificationViewModel
            {
                Metrics = metrics,
                Predictions = predictions
            };

            return View(viewModel);
        }
    }
}