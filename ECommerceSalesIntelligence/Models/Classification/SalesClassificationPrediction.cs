using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models.Classification
{
    public class SalesClassificationPrediction
    {
        public string City { get; set; } = "";
        public string ProductName { get; set; } = "";
        public float LastThreeMonthsSales { get; set; }
        public float LastMonthSales { get; set; }
        public float ThreeMonthAverage { get; set; }

        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
        public string TargetMonth { get; set; } = "";
    }
}