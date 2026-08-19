namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesMulticlassInput
    {
        public float UnitPrice { get; set; }
        public float Quantity { get; set; }
        public float DiscountRate { get; set; }
        public bool IsCampaign { get; set; }
        public string Label { get; set; } // "Low", "Medium", "High"
    }
}