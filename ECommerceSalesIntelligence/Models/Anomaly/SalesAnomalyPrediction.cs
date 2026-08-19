using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class SalesAnomalyPrediction
    {
        // ML.NET anomali tespiti çıktısı: [anomaliVarMı (0 veya 1), hamSkor, güvenSkoru]
        [ColumnName("Prediction")]
        public double[] Prediction { get; set; }
    }
}
