namespace ECommerceSalesIntelligence.Models
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; } // toplam geliri 
        public long TotalQuantity { get; set; } // toplam satılan ürün miktarı
        public long TotalSalesRecords { get; set; } // toplam satış kayıt sayısı
        public decimal AverageSaleAmount { get; set; } //  ortalama satış tutarı
        public decimal AverageUnitPrice { get; set; } // ortalama birim fiyatı
        public double CampaignRate { get; set; } // kampanya satış oranı
        public DateTime? StartDate { get; set; } // satış verilerinin başlangıç tarihi
        public DateTime? EndDate { get; set; } // satış verilerinin bitiş tarihi
        public List<DailySalesItem> DailySales { get; set; } = new(); //günlük satış verileri
        public List<CategorySalesItem> CategorySales { get; set; } = new(); // kategori bazında satış verileri
        public List<CitySalesItem> CitySales { get; set; } = new(); // şehir bazında satış verileri
        public List<PaymentMethodSalesItem> PaymentMethods { get; set; } = new(); //  ödeme yöntemleri
        public CampaignSalesSummary CampaignSummary { get; set; } = new(); 
        public List<ProductSalesItem> TopProducts { get; set; } = new();
    }

    public class DailySalesItem
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public long Quantity { get; set; }
    }

    public class CategorySalesItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public long Quantity { get; set; }
        public double RevenuePercentage { get; set; }
    }

    public class CitySalesItem
    {
        public string City { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public long Quantity { get; set; }
        public decimal AverageSaleAmount { get; set; }
    }

    public class PaymentMethodSalesItem
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public long Quantity { get; set; }
        public double Percentage { get; set; }
    }

    public class CampaignSalesSummary
    {
        public decimal CampaignRevenue { get; set; }
        public decimal NonCampaignRevenue { get; set; }
        public long CampaignQuantity { get; set; }
        public long NonCampaignQuantity { get; set; }
        public long CampaignRecordCount { get; set; }
        public long NonCampaignRecordCount { get; set; }
        public double CampaignRate { get; set; }
        public double NonCampaignRate { get; set; }
    }

    public class ProductSalesItem
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public long Quantity { get; set; }
    }
}