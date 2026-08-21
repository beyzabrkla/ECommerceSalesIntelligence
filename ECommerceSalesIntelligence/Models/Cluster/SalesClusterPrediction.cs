using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Models
{
    // K-Means tahmin sonucunu tutar
    public class SalesClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; } // Tahmin edilen küme ID'si

        [ColumnName("Score")]
        public float[] Distances { get; set; } = Array.Empty<float>(); // Küme merkezlerine olan uzaklıklar neden array empty float array olarak tanımlandı
                                                                       // Çünkü K-Means algoritması her veri noktasının tüm küme merkezlerine olan uzaklıklarını hesaplar ve bu uzaklıkları bir dizi olarak döndürür
                                                                       // Bu sayede her veri noktasının hangi kümeye daha yakın olduğunu ve hangi kümelerden ne kadar uzak olduğunu görebiliriz
    }
}