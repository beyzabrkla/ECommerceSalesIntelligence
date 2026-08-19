using Microsoft.AspNetCore.Mvc;
using ECommerceSalesIntelligence.Services;

namespace ECommerceSalesIntelligence.Controllers
{
    public class ClustersController : Controller
    {
        private readonly ClusteringService _clusteringService;

        public ClustersController(ClusteringService clusteringService)
        {
            _clusteringService = clusteringService;
        }

        public IActionResult Index(int count = 3)
        {
            var model = _clusteringService.TrainAndCluster(count);
            return View(model);
        }
    }
}