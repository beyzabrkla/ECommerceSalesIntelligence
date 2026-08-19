using ECommerceSalesIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSalesIntelligence.Controllers
{
    public class MulticlassClassificationController : Controller
    {
        private readonly MulticlassClassificationService _multiclassService;

        public MulticlassClassificationController(
            MulticlassClassificationService multiclassService)
        {
            _multiclassService = multiclassService;
        }

        public IActionResult Index()
        {
            var viewModel =
                _multiclassService.GetMulticlassDashboardData();

            return View(viewModel);
        }
    }
}