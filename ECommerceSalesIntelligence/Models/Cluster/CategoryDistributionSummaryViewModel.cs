namespace ECommerceSalesIntelligence.Models.Cluster
{
    public class CategoryDistributionSummaryViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public float Revenue { get; set; }
        public float Percentage { get; set; }
    }
}