using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class SalesClassificationPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; } //tahmin edilen sınıf (satın alma veya satın almama)
        public float Probability { get; set; } //satın alma olasılığı
        public float Score { get; set; } //tahminin güven skoru
    }
}
