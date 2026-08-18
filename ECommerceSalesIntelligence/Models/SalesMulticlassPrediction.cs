using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class SalesMulticlassPrediction
    {
        [ColumnName("PredictedLabel")]
        public string Prediction { get; set; }

        public float[] Score { get; set; }
    }
}
