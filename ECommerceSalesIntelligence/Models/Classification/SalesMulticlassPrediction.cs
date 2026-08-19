using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesMulticlassPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; }

        [ColumnName("Score")]
        public float[] Score { get; set; }
    }
}