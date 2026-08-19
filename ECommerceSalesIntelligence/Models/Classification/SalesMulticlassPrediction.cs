using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesMulticlassPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }
}