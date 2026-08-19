using ECommerceSalesIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSalesIntelligence.Controllers
{
    public class BinaryClassificationController : Controller
    {
        private readonly BinaryClassificationService _binaryService;

        public BinaryClassificationController(BinaryClassificationService binaryService)
        {
            _binaryService = binaryService;
        }

        public IActionResult Index()
        {
            var viewModel = _binaryService.GetBinaryDashboardData();
            return View(viewModel);
        }
    }
}