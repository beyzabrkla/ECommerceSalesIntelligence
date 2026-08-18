namespace ECommerceSalesIntelligence.Entities
{
    public class SalesRecord
    {
        public long Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } 
        public string Country { get; set; } 
        public string City { get; set; }
        public decimal DiscountRate { get; set; }
        public bool IsCampaign { get; set; }
    }
}
