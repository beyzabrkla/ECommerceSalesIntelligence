using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace ECommerceSalesIntelligence.Services
{
    /// Günlük satış serilerindeki olağan dışı davranışları ML.NET SSA Spike Detection algoritması ile tespit eder
    public class AnomalyDetectionService
    {
        private readonly AppDbContext _context;
        private readonly MLContext _mlContext;

        // SSA için kullanılan temel parametreler
        private const int TrainingWindowSize = 60; //
        private const int SeasonalityWindowSize = 7;
        private const double Confidence = 95.0;
        private const int PValueHistoryLength = 30;

        // Bir serinin analiz edilebilmesi için minimum veri şartları
        private const int MinimumSeriesLength = 60;
        private const int MinimumSalesDays = 30;

        public AnomalyDetectionService(AppDbContext context, MLContext mlContext)
        {
            _context = context;
            _mlContext = mlContext;
        }

        /// Tüm ülke + şehir + ürün serilerini analiz eder
        /// ve tespit edilen anomalileri döndürür
        public async Task<List<SalesAnomalyResultViewModel>> DetectAnomaliesAsync()
        {
            // Veritabanından günlük satış toplamlarını oluşturur
            var dailySales = await GetDailySalesAsync();

            if (dailySales.Count == 0)
                return new List<SalesAnomalyResultViewModel>();

            var results = new List<SalesAnomalyResultViewModel>();

            // Her ülke + şehir + ürün kombinasyonu ayrı analiz edilir
            var groups = dailySales
                .GroupBy(x => new
                {
                    x.Country,
                    x.City,
                    x.ProductName
                })
                .ToList();

            foreach (var group in groups)
            {
                var series = CreateCompleteSeries(group);

                // Çok kısa seriler SSA için yeterli değildir
                if (series.Count < MinimumSeriesLength)
                    continue;

                // Gerçek satış günü sayısı yeterli değilse seri analiz edilmez
                if (series.Count(x => x.Quantity > 0) < MinimumSalesDays)
                    continue;

                var anomalies = DetectSeriesAnomalies(series);

                if (anomalies.Count == 0)
                    continue;

                foreach (var anomaly in anomalies)
                {
                    var current = series[anomaly.Index];

                    // Beklenen satış sadece geçmiş gerçek satışlardan hesaplanır
                    var expected = CalculateExpectedSales(series, anomaly.Index);

                    if (!expected.HasValue)
                        continue;

                    var actual = current.Quantity;
                    var change = actual - expected.Value;

                    var changePercentage = CalculateChangePercentage( actual, expected.Value);

                    var status = GetAnomalyStatus( actual, expected.Value);

                    var severity = GetSeverity(Math.Abs(changePercentage));

                    results.Add(new SalesAnomalyResultViewModel
                    {
                        OrderDate = current.OrderDate,
                        Country = current.Country,
                        City = current.City,
                        ProductName = current.ProductName,
                        Quantity = actual,
                        ExpectedSales = expected.Value,
                        Change = change,
                        ChangePercentage = changePercentage,
                        Score = anomaly.Score,
                        PValue = anomaly.PValue,
                        IsAnomaly = true,
                        Status = status,
                        Severity = severity
                    });
                }
            }

            // En büyük sapmalar önce gösterilir
            return results .OrderByDescending(x => Math.Abs(x.ChangePercentage)).ThenByDescending(x => x.OrderDate).ToList();
        }

        /// Veritabanındaki satış kayıtlarını günlük toplam satışlara dönüştürür
        private async Task<List<DailySalesGroup>> GetDailySalesAsync()
        {
            return await _context.SalesRecords
                .AsNoTracking()
                .GroupBy(x => new
                {
                    x.Country,
                    x.City,
                    x.ProductName,
                    SaleDate = x.OrderDate.Date
                })
                .Select(g => new DailySalesGroup
                {
                    Country = g.Key.Country,
                    City = g.Key.City,
                    ProductName = g.Key.ProductName,
                    OrderDate = g.Key.SaleDate,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderBy(x => x.Country)
                .ThenBy(x => x.City)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.OrderDate)
                .ToListAsync();
        }

        /// Eksik takvim günlerini oluşturur
        /// Eksik günler SSA için 0 satış olarak kabul edilir
        /// Ancak beklenen satış hesabında bu sıfırlar kullanılmaz
        private static List<DailySalesGroup> CreateCompleteSeries(IEnumerable<DailySalesGroup> group)
        {
            var rawSeries = group.OrderBy(x => x.OrderDate).ToList();

            if (rawSeries.Count == 0)
                return new List<DailySalesGroup>();

            var startDate = rawSeries.First().OrderDate.Date;

            var endDate = rawSeries .Last().OrderDate.Date;

            // Gerçek satış bulunan tarihleri hızlı erişim için dictionary'ye alır
            var salesByDate = rawSeries.ToDictionary(x => x.OrderDate.Date,x => x.Quantity);

            var series = new List<DailySalesGroup>();

            // Başlangıç ve bitiş arasındaki bütün takvim günlerini oluşturur
            for (var date = startDate;
                 date <= endDate;
                 date = date.AddDays(1))
            {
                salesByDate.TryGetValue(date, out var quantity);

                series.Add(new DailySalesGroup
                {
                    Country = rawSeries[0].Country,
                    City = rawSeries[0].City,
                    ProductName = rawSeries[0].ProductName,
                    OrderDate = date,
                    Quantity = quantity
                });
            }

            return series;
        }

        /// Tek bir satış serisini ML.NET SSA ile analiz eder
        private List<AnomalyPoint> DetectSeriesAnomalies(List<DailySalesGroup> series)
        {
            // ML.NET'in kullanacağı input nesnelerini oluşturur
            var inputs = series.Select(x => new SalesAnomalyInput
                {
                    OrderDate = x.OrderDate,
                    Quantity = x.Quantity
                })
                .ToList();

            var dataView = _mlContext.Data.LoadFromEnumerable(inputs);

            ITransformer model;

            try
            {
                // SSA Spike Detection modeli oluşturulur
                var pipeline = _mlContext.Transforms.DetectSpikeBySsa(
                    outputColumnName: "Prediction",
                    inputColumnName: nameof(SalesAnomalyInput.Quantity),
                    confidence: Confidence,
                    pvalueHistoryLength: PValueHistoryLength,
                    trainingWindowSize: TrainingWindowSize,
                    seasonalityWindowSize: SeasonalityWindowSize);

                model = pipeline.Fit(dataView);
            }
            catch
            {
                // Model oluşturulamazsa bu seri atlanır
                return new List<AnomalyPoint>();
            }

            IDataView transformedData;

            try
            {
                // Eğitilen model satış serisini analiz eder
                transformedData = model.Transform(dataView);
            }
            catch
            {
                return new List<AnomalyPoint>();
            }

            List<SalesAnomalyPrediction> predictions;

            try
            {
                // ML.NET çıktısını model sınıfına dönüştürür
                predictions = _mlContext.Data.CreateEnumerable<SalesAnomalyPrediction>(transformedData,reuseRowObject: false).ToList();
            }
            catch
            {
                return new List<AnomalyPoint>();
            }

            var anomalies = new List<AnomalyPoint>();

            // Tahmin ve gerçek seri aynı indeks üzerinden eşleştirilir
            var count = Math.Min(series.Count, predictions.Count);

            for (var i = 0; i < count; i++)
            {
                var prediction = predictions[i];

                // ML.NET'in beklenen çıktı formatı kontrol edilir
                if (prediction.Prediction == null ||
                    prediction.Prediction.Length < 3)
                {
                    continue;
                }

                var isAnomaly = prediction.Prediction[0] == 1;

                // Normal günler sonuç listesine eklenmez
                if (!isAnomaly)
                    continue;

                var score = prediction.Prediction[1];

                var pValue = prediction.Prediction[2];

                // İstatistiksel olarak yeterince anlamlı olmayan anomaliler sonuçtan çıkarılır
                if (pValue > 0.05)
                    continue;

                anomalies.Add(new AnomalyPoint
                {
                    Index = i,
                    Score = (float)score,
                    PValue = (float)pValue
                });
            }

            return anomalies;
        }

        /// Anomali günü için beklenen satışı hesaplar
        /// Sıfır satış günleri ortalamaya dahil edilmez
        /// Önce aynı haftanın günleri, sonra daha geniş geçmiş kullanılır
        private static float? CalculateExpectedSales( List<DailySalesGroup> series, int currentIndex)
        {
            if (currentIndex <= 0)
                return null;

            var currentDate =
                series[currentIndex].OrderDate;

            // Öncelikle önceki aynı haftanın günlerindeki satışlar alınır
            var sameWeekdaySales = series
                .Take(currentIndex)
                .Where(x =>
                    x.Quantity > 0 &&
                    x.OrderDate.DayOfWeek ==
                    currentDate.DayOfWeek)
                .OrderByDescending(x => x.OrderDate)
                .Take(8)
                .Select(x => x.Quantity)
                .ToList();

            if (sameWeekdaySales.Count >= 2)
                return CalculateMedian(sameWeekdaySales);

            // Aynı haftanın günü yeterli değilse son 14 gün kullanılır
            var last14Days = series.Take(currentIndex)
                .Where(x =>
                    x.Quantity > 0 &&
                    x.OrderDate >= currentDate.AddDays(-14))
                .Select(x => x.Quantity)
                .ToList();

            if (last14Days.Count >= 3)
                return CalculateMedian(last14Days);

            // Son 30 gün daha geniş bir fallback olarak kullanılır
            var last30Days = series.Take(currentIndex).Where(x =>
                    x.Quantity > 0 &&
                    x.OrderDate >= currentDate.AddDays(-30))
                .Select(x => x.Quantity)
                .ToList();

            if (last30Days.Count >= 3)
                return CalculateMedian(last30Days);

            return null;
        }

        /// Verilen satış değerlerinin median değerini hesaplar.
        /// Median, aşırı yüksek satışların beklenen değeri bozmasını engeller.
        private static float CalculateMedian(
            List<float> values)
        {
            if (values.Count == 0)
                return 0;

            var orderedValues = values.OrderBy(x => x).ToList();

            var middle = orderedValues.Count / 2;

            if (orderedValues.Count % 2 == 1)
                return orderedValues[middle];

            return (orderedValues[middle - 1] + orderedValues[middle]) / 2f;
        }

        /// Gerçek satış ile beklenen satış arasındaki yüzde farkı hesaplar
        private static float CalculateChangePercentage(float actual, float expected)
        {
            if (expected <= 0)
                return actual > 0 ? 100f : 0f;

            return ((actual - expected) / expected) * 100f;
        }

        /// Anomalinin satış sıçraması mı yoksa düşüş mü olduğunu belirler
        private static string GetAnomalyStatus(float actual, float expected)
        {
            if (expected <= 0)
            {
                return actual > 0 ? "SIÇRAMA" : "ANOMALİ";
            }

            if (actual > expected) return "SIÇRAMA";
            if (actual < expected) return "DÜŞÜŞ";
            return "ANOMALİ";
        }

        /// Anomalinin şiddetini yüzde sapmaya göre belirler
        private static string GetSeverity(
            float percentage)
        {
            if (percentage >= 100) return "KRİTİK";
            if (percentage >= 50) return "YÜKSEK";
            if (percentage >= 25) return "ORTA";

            return "DÜŞÜK";
        }

        /// Günlük satışları temsil eden dahili modeldir
        private class DailySalesGroup
        {
            public string Country { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public DateTime OrderDate { get; set; }
            public float Quantity { get; set; }
        }

        /// ML.NET tarafından tespit edilen tek bir anomaly noktasını tutar
        private class AnomalyPoint
        {
            public int Index { get; set; }
            public float Score { get; set; }
            public float PValue { get; set; }
        }
    }
}
