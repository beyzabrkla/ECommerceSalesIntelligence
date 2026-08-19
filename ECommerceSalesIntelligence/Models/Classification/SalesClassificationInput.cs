using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesClassificationInput
    {
        [LoadColumn(0)]
        public string City { get; set; }

        [LoadColumn(1)]
        public string ProductName { get; set; }

        [LoadColumn(2)]
        public float ThreeMonthsAgo { get; set; }

        [LoadColumn(3)]
        public float TwoMonthsAgo { get; set; }

        [LoadColumn(4)]
        public float LastMonth { get; set; }

        [LoadColumn(5)]
        public float ThreeMonthAverage { get; set; }

        [LoadColumn(6)]
        public string TargetMonth { get; set; }

        [LoadColumn(7)]
        public bool Label { get; set; }

        // Gerçek gelecek ay satış miktarı.
        // SADECE Label oluşturmak için kullanılır.
        // ML.NET Features içine kesinlikle eklenmez.
        [LoadColumn(8)]
        public float TargetQuantity { get; set; }
    }

    public class BinaryDashboardViewModel
    {
        public BinaryClassificationMetrics Metrics { get; set; }

        public List<SalesClassificationPrediction> CityPredictions { get; set; } = new();
    }
}