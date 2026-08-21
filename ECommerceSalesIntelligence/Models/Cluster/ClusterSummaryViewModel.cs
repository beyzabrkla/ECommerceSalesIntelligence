namespace ECommerceSalesIntelligence.Models.Cluster
{
    // Kümenin genel özet bilgisini temsil eder
    public class ClusterSummaryViewModel
    {
        public uint ClusterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; //neden string empty kullanıyoruz çünkü null olmasını istemiyoruz eğer empty yazmazsak null olabilir ve null reference hatası alabiliriz
        public int CityCount { get; set; }
        public float AverageUnitPrice { get; set; }
        public float AverageQuantity { get; set; }
        public float AverageOrderAmount { get; set; }
        public float TotalRevenue { get; set; }
    }
}