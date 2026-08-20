namespace ECommerceSalesIntelligence.Models
{
    public class SalesAnomalyInput
    {
        public DateTime OrderDate { get; set; }

        public string Country { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public float Quantity { get; set; }
    }
}