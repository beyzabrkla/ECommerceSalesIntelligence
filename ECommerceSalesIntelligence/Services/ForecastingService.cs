using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace ECommerceSalesIntelligence.Services
{
    public class ForecastingService
    {
        private readonly AppDbContext _context;
        private readonly MLContext _mlContext;

        public ForecastingService(AppDbContext context, MLContext mlContext)
        {
            _context = context;
            _mlContext = mlContext;
        }

        // Seçilen şehir için geçmiş günlük satışlardan gelecek günleri tahmin eder
        public async Task<SalesPrediction> PredictNextDaysAsync( string city, int horizon = 7, float confidenceLevel = 0.95f)
        {
            // Parametrelerin geçerli olup olmadığını kontrol eder
            if (string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException(
                    "Şehir bilgisi boş olamaz.",
                    nameof(city));
            }

            city = city.Trim(); // Şehir adının başındaki ve sonundaki boşlukları kaldırır

            // Şehirdeki satışları günlük toplam satışa dönüştürür
            var groupedSales = await _context.SalesRecords
                .AsNoTracking() // Veritabanından satış kayıtlarını getirir ve değişiklik takibi yapmaz
                .Where(x => x.City != null && x.City == city) 
                .GroupBy(x => x.OrderDate.Date)
                .Select(g => new
                {
                    OrderDate = g.Key, 
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderBy(x => x.OrderDate)
                .ToListAsync();

            // Modelin kullanacağı tarih aralığını belirler
            DateTime startDate = groupedSales.First().OrderDate.Date;
            DateTime endDate = groupedSales.Last().OrderDate.Date;

            // Günlük satışlara hızlı erişim için dictionary oluşturur. Yani her günün satış miktarını hızlıca bulmak için bir sözlük oluşturur
            var salesDictionary = groupedSales.ToDictionary(
                x => x.OrderDate.Date,
                x => Convert.ToSingle(x.Quantity));

            // Satış olmayan günleri 0 ile doldurarak kesintisiz zaman serisi oluşturur
            var dailySales = new List<SalesData>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                float quantity = salesDictionary.TryGetValue(date, out float existingQuantity) ? existingQuantity : 0f;
                dailySales.Add(new SalesData
                {
                    OrderDate = date, // Günlük satış verisinin tarihini ayarlar
                    Quantity = Math.Max(0f, quantity) // Satış miktarını 0'ın altına düşürmez
                });
            }

            // SSA modeli için minimum veri kontrolü yapar
            if (dailySales.Count < 30)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA tahmini yapmak üzere yeterli günlük veri yok. " +
                    $"En az 30 takvim günü gerekiyor. " +
                    $"Mevcut gün sayısı: {dailySales.Count}");
            }

            int nonZeroDays = dailySales.Count(x => x.Quantity > 0);

            if (nonZeroDays < 10)
            {
                throw new InvalidOperationException(
                    $"{city} için yeterli gerçek satış günü yok. " +
                    $"En az 10 satış yapılan gün gerekiyor. " +
                    $"Mevcut satış günü: {nonZeroDays}");
            }

            // SSA'nın geçmiş pencere ve eğitim veri uzunluğunu belirler
            int seriesLength = Math.Min(90, dailySales.Count);
            int windowSize = Math.Max(2, Math.Min(30, seriesLength / 2)); // 30 günlük pencere boyutu, ancak veri uzunluğunun yarısından fazla olamaz

            if (windowSize >= seriesLength)
            {
                windowSize = Math.Max(2, seriesLength - 1);
            }

            int trainSize = dailySales.Count;

            // Günlük satış listesini ML.NET veri yapısına dönüştürür
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(dailySales);

            // SSA forecasting pipeline'ını oluşturur
            IEstimator<ITransformer> forecastingPipeline;

            try
            {
                forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(SalesPrediction.ForecastedQuantity), 
                    inputColumnName: nameof(SalesData.Quantity), 
                    windowSize: windowSize,
                    seriesLength: seriesLength,
                    trainSize: trainSize,
                    horizon: horizon,
                    confidenceLevel: confidenceLevel,
                    confidenceLowerBoundColumn: nameof(SalesPrediction.LowerBound),
                    confidenceUpperBoundColumn: nameof(SalesPrediction.UpperBound));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA pipeline oluşturulamadı. " +
                    $"SeriesLength={seriesLength}, " +
                    $"WindowSize={windowSize}, " +
                    $"TrainSize={trainSize}, " +
                    $"Horizon={horizon}",
                    ex);
            }
            ITransformer forecastModel; // bu değişken, eğitilmiş SSA modelini tutar

            try
            {
                forecastModel = forecastingPipeline.Fit(dataView);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA modeli eğitilemedi. " +
                    $"Günlük veri sayısı={dailySales.Count}, " +
                    $"SeriesLength={seriesLength}, " +
                    $"WindowSize={windowSize}, " +
                    $"TrainSize={trainSize}",
                    ex);
            }

            // Eğitilmiş model üzerinden zaman serisi tahmin motoru oluşturur
            TimeSeriesPredictionEngine<SalesData, SalesPrediction> forecastingEngine;

            try
            {
                forecastingEngine = forecastModel.CreateTimeSeriesEngine<
                    SalesData,
                    SalesPrediction>(_mlContext);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA forecasting engine oluşturulamadı.",
                    ex);
            }

            // Gelecek günlerin satış tahminini üretir
            SalesPrediction prediction;

            try
            {
                prediction = forecastingEngine.Predict();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA tahmini oluşturulamadı.",
                    ex);
            }

            // Tahmin modelinin teknik bilgilerini sonuç nesnesine ekler
            prediction.City = city;
            prediction.WindowSize = windowSize;
            prediction.SeriesLength = seriesLength;
            prediction.TrainSize = trainSize;
            prediction.Horizon = horizon;
            prediction.ConfidenceLevel = confidenceLevel;

            // Son 30 gerçek günü dashboard grafiği için hazırlar
            prediction.HistoricalDetails = dailySales
                .TakeLast(30)
                .Select(x => new HistoricalDetailItem
                {
                    Date = x.OrderDate.ToString("yyyy-MM-dd"),
                    ActualSales = x.Quantity
                })
                .ToList();

            // Tahmin başlangıç tarihini belirler
            DateTime lastHistoricalDate = dailySales.Last().OrderDate.Date;

            // Her tahmin günü için tarih, tahmin ve güven aralığını oluşturur
            prediction.Details = new List<ForecastDetailItem>();

            for (int i = 0; i < horizon; i++)
            {
                float rawForecast = prediction.ForecastedQuantity.Length > i ? prediction.ForecastedQuantity[i] : 0f; // tahmin edileni al veya 0 olarak ayarla
                 
                float rawLower = prediction.LowerBound != null && prediction.LowerBound.Length > i ? prediction.LowerBound[i] : rawForecast; // alt sınırı al veya tahmin edilen değeri kullan

                float rawUpper = prediction.UpperBound != null && prediction.UpperBound.Length > i ? prediction.UpperBound[i] : rawForecast; // üst sınırı al veya tahmin edilen değeri kullan

                // Geçersiz matematiksel değerleri güvenli değerlere çevirir
                if (float.IsNaN(rawForecast) || float.IsInfinity(rawForecast)) rawForecast = 0f;

                if (float.IsNaN(rawLower) || float.IsInfinity(rawLower)) rawLower = 0f;

                if (float.IsNaN(rawUpper) || float.IsInfinity(rawUpper)) rawUpper = rawForecast;

                float forecastedSales = Math.Max(0f, (float)Math.Round(rawForecast, 2));

                float lowerBound = Math.Max( 0f, (float)Math.Round(rawLower, 2));

                float upperBound = Math.Max( forecastedSales, (float)Math.Round(rawUpper, 2));

                prediction.Details.Add(new ForecastDetailItem
                {
                    Date = lastHistoricalDate
                        .AddDays(i + 1)
                        .ToString("yyyy-MM-dd"),

                    PredictedSales = forecastedSales,
                    LowerBound = lowerBound,
                    UpperBound = upperBound
                });
            }

            return prediction;
        }

        // Belirli şehir ve tarih için beklenen günlük satış miktarını hesaplar
        public async Task<float?> GetExpectedSalesAsync(string city, string? productName, DateTime targetDate)
        {
            try
            {
                // Hedef tarihe kadar olan şehir satışlarını günlük olarak toplar
                var groupedSales = await _context.SalesRecords
                    .AsNoTracking()
                    .Where(x =>
                        x.City == city &&
                        x.OrderDate.Date <= targetDate.Date)
                    .GroupBy(x => x.OrderDate.Date)
                    .Select(g => new
                    {
                        OrderDate = g.Key,
                        Quantity = g.Sum(x => x.Quantity)
                    })
                    .OrderBy(x => x.OrderDate)
                    .ToListAsync();

                if (groupedSales.Count < 30)
                    return null;

                // Eksik günleri 0 ile tamamlayarak zaman serisini oluşturur
                var startDate = groupedSales.First().OrderDate.Date;
                var endDate = targetDate.Date;

                var salesDictionary = groupedSales.ToDictionary(
                    x => x.OrderDate.Date,
                    x => x.Quantity);

                var dailySales = new List<SalesData>();

                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    salesDictionary.TryGetValue(date, out int quantity);

                    dailySales.Add(new SalesData
                    {
                        OrderDate = date,
                        Quantity = quantity
                    });
                }

                if (dailySales.Count < 30)
                    return null;

                // Çok az gerçek satış bulunan serilerde tahmin yapmaz
                int nonZeroDays = dailySales.Count(x => x.Quantity > 0);

                if (nonZeroDays < 10)
                    return null;

                // Tek günlük beklenen satış için SSA parametrelerini belirler
                int seriesLength = dailySales.Count;
                int windowSize = Math.Max(2, Math.Min(30, seriesLength / 2)); // 30 günlük pencere boyutu, ancak veri uzunluğunun yarısından fazla olamaz
                int trainSize = seriesLength;

                IDataView dataView = _mlContext.Data.LoadFromEnumerable(dailySales); // Günlük satış listesini ML.NET veri yapısına dönüştürür

                // Bir sonraki gün için SSA modeli oluşturur
                var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(SalesPrediction.ForecastedQuantity),
                    inputColumnName: nameof(SalesData.Quantity),
                    windowSize: windowSize,
                    seriesLength: seriesLength,
                    trainSize: trainSize,
                    horizon: 1,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: nameof(SalesPrediction.LowerBound),
                    confidenceUpperBoundColumn: nameof(SalesPrediction.UpperBound));

                // Modeli eğitir
                ITransformer model = forecastingPipeline.Fit(dataView);

                // Tek günlük tahmin üretir
                var engine = model.CreateTimeSeriesEngine< SalesData,SalesPrediction>(_mlContext);

                var prediction = engine.Predict();

                if (prediction?.ForecastedQuantity == null ||
                    prediction.ForecastedQuantity.Length == 0)
                {
                    return null;
                }

                float expectedSales = prediction.ForecastedQuantity[0];

                if (float.IsNaN(expectedSales) ||
                    float.IsInfinity(expectedSales))
                {
                    return null;
                }

                return Math.Max(0f, (float)Math.Round(expectedSales, 2));
            }
            catch
            {
                // Beklenen satış hesaplanamazsa ana akışı bozmaz
                return null;
            }
        }
    }
}