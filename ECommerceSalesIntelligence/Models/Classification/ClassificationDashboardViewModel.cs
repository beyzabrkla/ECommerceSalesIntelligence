using ECommerceSalesIntelligence.Models.Classification;
using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    public class ClassificationDashboardViewModel
    {
        public CalibratedBinaryClassificationMetrics? Metrics { get; set; } //bu metrikler, modelin performansını değerlendirmek için kullanılır. Örneğin, Accuracy, Precision, Recall gibi metrikler içerir
                                                                            //bunlar da modelin doğruluğunu ve güvenilirliğini ölçmek için kullanılır
        public List<SalesClassificationPrediction> Predictions { get; set; }= new(); //bu, modelin tahmin ettiği sonuçları içerir
                                                                                     //Örneğin bir satışın başarılı olup olmayacağını tahmin eden bir model için bu liste her satış için tahmin edilen sonucu içerir
        public float Threshold { get; set; } //bu, modelin tahminlerini sınıflandırmak için kullanılan eşik değerini temsil eder
                                             //Örneğin, bir satışın başarılı olup olmayacağını tahmin eden bir model için, bu eşik değeri belirli bir olasılık değerinin üzerinde olan tahminleri başarılı olarak sınıflandırmak için kullanılır
        public string? Message { get; set; }
    }
}