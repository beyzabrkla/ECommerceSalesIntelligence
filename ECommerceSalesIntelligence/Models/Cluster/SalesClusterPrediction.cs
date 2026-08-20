using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    // K-Means modelinin ürettiği küme sonucunu tutar.
    public class SalesClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }

        [ColumnName("Score")]
        public float[] Distances { get; set; } = Array.Empty<float>();
    }
}