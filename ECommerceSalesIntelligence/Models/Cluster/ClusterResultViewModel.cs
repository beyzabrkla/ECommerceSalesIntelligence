using ECommerceSalesIntelligence.Models.Cluster;

namespace ECommerceSalesIntelligence.Models
{
    public class ClusterResultViewModel
    {
        public uint ClusterId { get; set; }

        public string City { get; set; } = string.Empty;

        // Mevcut satış özellikleri
        public float TotalQuantity { get; set; }
        public float AverageUnitPrice { get; set; }
        public float TotalSalesAmount { get; set; }
        public float AverageOrderAmount { get; set; }
        public float TotalRevenue { get; set; }
        public float CampaignRate { get; set; }

        // Kategori özellikleri
        public int CategoryCount { get; set; }
        public string TopCategory { get; set; } = string.Empty;
        public float TopCategoryRate { get; set; }
        public float CategoryDiversity { get; set; }

        // Şehirdeki kategori dağılımı
        public List<CategoryDistributionSummaryViewModel> CategoryDistribution { get; set; } = new();
    }
}