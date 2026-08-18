namespace ECommerceSalesIntelligence.Models
{
    public class SalesPrediction
    {
        public float[] ForecastedQuantity { get; set; } = Array.Empty<float>(); //bu tun tahmin edilen satış miktarlarını tutar
        public float[] LowerBound { get; set; } = Array.Empty<float>(); //bu tun tahmin edilen satış miktarlarının alt sınırlarını tutar
        public float[] UpperBound { get; set; } = Array.Empty<float>(); //bu tun tahmin edilen satış miktarlarının üst sınırlarını tutar
    }
}
