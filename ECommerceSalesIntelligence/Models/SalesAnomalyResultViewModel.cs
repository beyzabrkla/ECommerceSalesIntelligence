namespace ECommerceSalesIntelligence.Models
{
    public class SalesAnomalyResultViewModel
    {
        public DateTime OrderDate { get; set; }
        public float TotalAmount { get; set; }
        public bool IsAnomaly { get; set; }
        public float Score { get; set; }
    }
}
