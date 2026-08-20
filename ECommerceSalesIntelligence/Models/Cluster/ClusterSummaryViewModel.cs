namespace ECommerceSalesIntelligence.Models.Cluster
{
    // View'daki küme özet kartını temsil eder.
    public class ClusterSummaryViewModel
    {
        public uint ClusterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CityCount { get; set; }
        public float AverageUnitPrice { get; set; }
        public float AverageQuantity { get; set; }
        public float AverageOrderAmount { get; set; }
        public float TotalRevenue { get; set; }
    }
}