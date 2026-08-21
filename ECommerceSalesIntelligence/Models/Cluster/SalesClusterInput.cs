namespace ECommerceSalesIntelligence.Models
{
    public class SalesClusterInput
    {
        public string City { get; set; } = string.Empty;
        public float AverageDailyQuantity { get; set; }
        public float AverageOrderAmount { get; set; }
        public float CampaignRate { get; set; }
        public float AverageDiscountRate { get; set; }
        public float CategoryCount { get; set; }
        public float TopCategoryRate { get; set; }
        public float CategoryDiversity { get; set; }
    }
}