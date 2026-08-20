using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesMulticlassPrediction
    {
        // ML.NET tarafından tahmin edilen sınıfı temsil eder.
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;

        // Her sınıf için model skorlarını içerir.
        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }
}