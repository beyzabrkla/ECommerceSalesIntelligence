using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesClassificationPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }

        [ColumnName("Probability")]
        public float Probability { get; set; }

        [ColumnName("Score")]
        public float Score { get; set; }

        public string City { get; set; }
        public float ThreeMonthsAgo { get; set; }
        public float TwoMonthsAgo { get; set; }
        public float LastMonth { get; set; }
        public float ThreeMonthAverage { get; set; }
    }
}