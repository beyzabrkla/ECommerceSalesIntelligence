using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class SalesClassificationInput
    {
        public float UnitPrice { get; set; }
        public float Quantity { get; set; }
        public float DiscountRate { get; set; }
        public bool IsCampaign { get; set; }

        [LoadColumn(4)]
        public bool Label { get; set; } // Hedef değişken: Eşik aşıldı mı? (True/False)
    }
}
