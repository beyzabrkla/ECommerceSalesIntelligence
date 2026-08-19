namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesMulticlassInput
    {
        public string City { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public float ThreeMonthsAgo { get; set; }
        public float TwoMonthsAgo { get; set; }
        public float LastMonth { get; set; }
        public float ThreeMonthAverage { get; set; }

        // Geçmiş davranış özellikleri
        public float LastMonthGrowthRate { get; set; }
        public float TwoMonthGrowthRate { get; set; }
        public float LastMonthVsAverageRate { get; set; }
        public float TrendSlope { get; set; }

        // Gelecek ayın takvim ayı
        public float TargetMonthNumber { get; set; }

        public string TargetMonth { get; set; } = string.Empty;

        // Gelecek ayın satışının kendi geçmişine oranı.
        // SADECE Label oluşturmak için kullanılır.
        public float TargetPerformanceRatio { get; set; }

        // Gerçek gelecek ay satış miktarı.
        // Model feature'ı değildir.
        public float TargetQuantity { get; set; }

        // Low / Medium / High
        public string Label { get; set; } = string.Empty;
    }
}