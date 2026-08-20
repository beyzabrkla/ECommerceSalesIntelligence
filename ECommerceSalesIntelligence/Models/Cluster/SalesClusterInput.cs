namespace ECommerceSalesIntelligence.Models
{
    // K-Means algoritmasına gönderilecek şehir özellikleri.
    public class SalesClusterInput
    {
        // Şehir adı.
        public string City { get; set; } = string.Empty;

        // Log dönüşümü uygulanmış toplam satış miktarı.
        public float TotalQuantity { get; set; }

        // Log dönüşümü uygulanmış ortalama birim fiyat.
        public float AverageUnitPrice { get; set; }

        // Log dönüşümü uygulanmış ortalama satış tutarı.
        public float AverageOrderAmount { get; set; }

        // Log dönüşümü uygulanmış toplam ciro.
        public float TotalRevenue { get; set; }

        // Ortalama indirim oranı.
        public float AverageDiscountRate { get; set; }

        // Kampanyalı satış oranı.
        public float CampaignRate { get; set; }

        // Ciro / satış miktarı oranı.
        public float RevenuePerQuantity { get; set; }

        // Şehirde kullanılan farklı kategori sayısı.
        public float CategoryCount { get; set; }

        // En baskın kategorinin satış payı.
        public float TopCategoryRate { get; set; }

        // Kategori çeşitliliği.
        public float CategoryDiversity { get; set; }
    }
}