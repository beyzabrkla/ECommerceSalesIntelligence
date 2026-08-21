namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesMulticlassInput
    {
        // Kategorik model özellikleri
        public string City { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        // Son üç aylık geçmiş satış değerleri
        public float ThreeMonthsAgo { get; set; }
        public float TwoMonthsAgo { get; set; }
        public float LastMonth { get; set; }
        public float ThreeMonthAverage { get; set; }

        // Satış davranışını açıklayan trend özellikleri
        public float LastMonthGrowthRate { get; set; }
        public float TwoMonthGrowthRate { get; set; }
        public float LastMonthVsAverageRate { get; set; }
        public float TrendSlope { get; set; }

        // Gelecek ayın takvim ayını temsil eder
        public float TargetMonthNumber { get; set; }

        // Raporlama amacıyla hedef ay bilgisi tutulur
        public string TargetMonth { get; set; } = string.Empty;

        // Gerçek gelecek ay satışının geçmiş ortalamaya oranıdır
        // Sadece Low/Medium/High label üretmek için kullanılır
        public float TargetPerformanceRatio { get; set; }

        // Gerçek gelecek ay satışıdır
        // Model feature'ı olarak kesinlikle kullanılmaz
        public float TargetQuantity { get; set; }

        // Modelin öğrenmeye çalıştığı hedef sınıftır
        public string Label { get; set; } = string.Empty;
    }
}