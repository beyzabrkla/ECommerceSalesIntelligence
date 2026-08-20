namespace ECommerceSalesIntelligence.Models
{
    public class SalesAnomalyPrediction
    {
        // [Anomaly, Score, PValue].
        public double[] Prediction { get; set; } = Array.Empty<double>();
    }
}