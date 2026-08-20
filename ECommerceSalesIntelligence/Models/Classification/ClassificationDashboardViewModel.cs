using ECommerceSalesIntelligence.Models.Classification;

namespace ECommerceSalesIntelligence.Models
{
    public class ClassificationDashboardViewModel
    {
        public Microsoft.ML.Data.CalibratedBinaryClassificationMetrics? Metrics { get; set; }

        public List<SalesClassificationPrediction> Predictions { get; set; }
            = new();

        public float Threshold { get; set; }

        public string? Message { get; set; }
    }
}