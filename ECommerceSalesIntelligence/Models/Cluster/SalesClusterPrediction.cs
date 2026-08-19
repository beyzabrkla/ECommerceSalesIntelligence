using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class SalesClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }

        [ColumnName("Score")]
        public float[] Distances { get; set; }
    }
}
