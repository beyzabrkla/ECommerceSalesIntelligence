using ECommerceSalesIntelligence.Models.Classification;
using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class MulticlassClassificationViewModel
    {
        public MulticlassClassificationMetrics? Metrics { get; set; }
        public List<MulticlassPredictionViewModel> Predictions { get; set; } = new();
    }

    public class MulticlassPredictionViewModel
    {
        public string Sku { get; set; }
        public float PredictedVolume { get; set; }
        public float Confidence { get; set; }
        public string DemandCategory { get; set; }
    }
}