namespace ECommerceSalesIntelligence.Models
{
    public class SalesAnomalyResultViewModel
    {
        public DateTime OrderDate { get; set; }
        public string Country { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public float Quantity { get; set; } //gerçek satış
        public float ExpectedSales { get; set; } //beklenen satış
        public float Change { get; set; } //değişim miktarı (Gerçek - Beklenen)
        public float ChangePercentage { get; set; } //sapma yüzdesi (Değişim / Beklenen * 100)
        public float Score { get; set; } //anomaly score (0-1 arası değer, 1'e yakınsa anomali olma olasılığı yüksek)
        public float PValue { get; set; } //p-value (istatistiksel anlamlılık testi için kullanılır, 0.05'ten küçükse anomali olma olasılığı yüksek)
        public bool IsAnomaly { get; set; } //anomaly olup olmadığını belirten boolean değer
        public string Status { get; set; } = string.Empty; //anomaly yönü (sıçrama, düşüş, anomali)
        public string Severity { get; set; } = string.Empty; //anomaly şiddeti (düşük, orta, yüksek, kritik)
    }
}