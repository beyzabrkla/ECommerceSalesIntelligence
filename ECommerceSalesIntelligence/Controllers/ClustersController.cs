using ECommerceSalesIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSalesIntelligence.Controllers
{
    public class ClustersController : Controller
    {
        private readonly ClusteringService _clusteringService;

        public ClustersController(ClusteringService clusteringService)
        {
            _clusteringService = clusteringService;
        }

        // Clustering analizini çalıştırır ve sonuçları View'a gönderir.
        public async Task<IActionResult> Index(int count = 4)
        {
            // Şehirleri K-Means algoritmasıyla kümelendirir.
            var model = await _clusteringService.TrainAndClusterAsync(count);

            // Kümeleme sonuçlarını sayfaya gönderir.
            return View(model);
        }
    }
}