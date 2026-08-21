using ECommerceSalesIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSalesIntelligence.Controllers
{
    public class ClustersController : Controller
    {
        private readonly ClusteringService _clusteringService;

        public ClustersController(
            ClusteringService clusteringService)
        {
            _clusteringService = clusteringService;
        }

        public async Task<IActionResult> Index(int count = 2)
        {
            // Şehirleri davranış özelliklerine göre kümeler
            var model = await _clusteringService.TrainAndClusterAsync(count);
            return View(model);
        }
    }
}