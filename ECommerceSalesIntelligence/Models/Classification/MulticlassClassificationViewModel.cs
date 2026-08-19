using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class MulticlassClassificationViewModel
    {
        public MulticlassClassificationMetrics? Metrics { get; set; }

        public List<MulticlassPredictionViewModel> Predictions { get; set; } = new();

        public double P33 { get; set; }
        public double P66 { get; set; }

        public double LowUpper { get; set; }
        public double MediumUpper { get; set; }

        public int LowCount { get; set; }
        public int MediumCount { get; set; }
        public int HighCount { get; set; }

        public int SampleCount { get; set; }
        public int TrainCount { get; set; }
        public int TestCount { get; set; }

        public string NextMonthLabel { get; set; } = string.Empty;
    }

    public class MulticlassPredictionViewModel
    {
        public string City { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public float PredictedVolume { get; set; }
        public float Average3 { get; set; }

        public float Confidence { get; set; }

        public string PredictedClass { get; set; } = string.Empty;
        public string DemandCategory { get; set; } = string.Empty;
    }
}