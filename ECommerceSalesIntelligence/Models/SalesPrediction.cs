using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class SalesPrediction
    {
        [ColumnName("ForecastedQuantity")]
        public float[] ForecastedQuantity { get; set; } = Array.Empty<float>(); // bu tahmin edilen satış miktarlarını temsil eder ve float dizisi olarak saklanır

        [ColumnName("LowerBound")]
        public float[] LowerBound { get; set; } = Array.Empty<float>(); // bu tahmin edilen satış miktarlarının alt sınırlarını temsil eder ve float dizisi olarak saklanır

        [ColumnName("UpperBound")]
        public float[] UpperBound { get; set; } = Array.Empty<float>(); // bu tahmin edilen satış miktarlarının üst sınırlarını temsil eder ve float dizisi olarak saklanır

        [NoColumn]
        public string City { get; set; } = string.Empty;

        [NoColumn]
        public int WindowSize { get; set; }

        [NoColumn]
        public int SeriesLength { get; set; }

        [NoColumn]
        public int TrainSize { get; set; }

        [NoColumn]
        public int Horizon { get; set; }

        [NoColumn]
        public float ConfidenceLevel { get; set; }

        [NoColumn]
        public List<ForecastDetailItem> Details { get; set; } = new(); 

        [NoColumn]
        public List<HistoricalDetailItem> HistoricalDetails { get; set; } = new();
    }

    public class ForecastDetailItem
    {
        public string Date { get; set; } = string.Empty;
        public float PredictedSales { get; set; }
        public float LowerBound { get; set; }
        public float UpperBound { get; set; }
    }

    public class HistoricalDetailItem
    {
        public string Date { get; set; } = string.Empty;
        public float ActualSales { get; set; }
    }
}