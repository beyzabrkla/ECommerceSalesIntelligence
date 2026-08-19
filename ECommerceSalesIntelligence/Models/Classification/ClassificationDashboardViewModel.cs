using ECommerceSalesIntelligence.Models.Classification;
using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class ClassificationDashboardViewModel
    {
        // Binary (İkili) Sınıflandırma Metrikleri ve Örnek Tahmin
        public BinaryClassificationMetrics Metrics { get; set; }
        public SalesClassificationPrediction SamplePrediction { get; set; }

        // Razor sayfasındaki tablo için aranan Predictions listesi:
        public List<SalesClassificationPrediction> Predictions { get; set; } = new();
        public float Threshold { get; set; } // Eşik değerini ekranda göstermek için
    }
}