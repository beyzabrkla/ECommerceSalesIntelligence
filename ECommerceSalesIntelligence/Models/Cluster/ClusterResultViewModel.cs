using ECommerceSalesIntelligence.Models.Cluster;

namespace ECommerceSalesIntelligence.Models
{
    // Bir şehrin küme sonucunu temsil eder
    public class ClusterResultViewModel
    {
        public uint ClusterId { get; set; }
        public string City { get; set; } = string.Empty;
        public float TotalQuantity { get; set; }
        public float AverageUnitPrice { get; set; }
        public float TotalSalesAmount { get; set; }
        public float AverageOrderAmount { get; set; }
        public float TotalRevenue { get; set; }
        public float CampaignRate { get; set; }
        public int CategoryCount { get; set; }
        public string TopCategory { get; set; } = string.Empty;
        public float TopCategoryRate { get; set; }
        public float CategoryDiversity { get; set; }
        public List<CategoryDistributionSummaryViewModel> CategoryDistribution { get; set; } = new(); // Şehirdeki kategori dağılımını gösteriyoryuz peki neden new () kullanıyoruz çünkü bu property bir liste ve null olmasını istemiyoruz
                                                                                                      // bu yüzden boş bir liste ile başlatıyoruz
    }
}