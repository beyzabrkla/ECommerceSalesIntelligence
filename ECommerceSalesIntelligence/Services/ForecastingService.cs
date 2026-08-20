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

        public ForecastingService(
            AppDbContext context,
            MLContext mlContext)
        {
            _context = context;
            _mlContext = mlContext;
        }

        /// <summary>
        /// Seçilen şehir için geçmiş günlük toplam satışlardan
        /// gelecek günlerin satış miktarını ML.NET SSA ile tahmin eder.
        /// </summary>
        public async Task<SalesPrediction> PredictNextDaysAsync(
            string city,
            int horizon = 7,
            float confidenceLevel = 0.95f)
        {
            // ============================================================
            // 1. PARAMETRE KONTROLLERİ
            // ============================================================

            if (string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException(
                    "Şehir bilgisi boş olamaz.",
                    nameof(city));
            }

            city = city.Trim();

            if (horizon < 1)
            {
                throw new ArgumentException(
                    "Tahmin ufku en az 1 gün olmalıdır.",
                    nameof(horizon));
            }

            if (horizon > 30)
            {
                throw new ArgumentException(
                    "Tahmin ufku en fazla 30 gün olabilir.",
                    nameof(horizon));
            }

            if (confidenceLevel <= 0 || confidenceLevel >= 1)
            {
                throw new ArgumentException(
                    "Güven düzeyi 0 ile 1 arasında olmalıdır.",
                    nameof(confidenceLevel));
            }

            // ============================================================
            // 2. SQL'DEN ŞEHRE AİT GÜNLÜK SATIŞLARI AL
            // ============================================================

            var groupedSales = await _context.SalesRecords
                .AsNoTracking()
                .Where(x =>
                    x.City != null &&
                    x.City == city)
                .GroupBy(x => x.OrderDate.Date)
                .Select(g => new
                {
                    OrderDate = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderBy(x => x.OrderDate)
                .ToListAsync();

            if (groupedSales.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{city} için satış verisi bulunamadı.");
            }

            // ============================================================
            // 3. TARİH ARALIĞINI BELİRLE
            // ============================================================

            DateTime startDate =
                groupedSales.First().OrderDate.Date;

            DateTime endDate =
                groupedSales.Last().OrderDate.Date;

            // ============================================================
            // 4. GÜNLÜK SATIŞ SÖZLÜĞÜ
            // ============================================================

            var salesDictionary = groupedSales
                .ToDictionary(
                    x => x.OrderDate.Date,
                    x => Convert.ToSingle(x.Quantity));

            // ============================================================
            // 5. EKSİK TAKVİM GÜNLERİNİ TAMAMLA
            //
            // Satış olmayan gün = 0
            // ============================================================

            var dailySales = new List<SalesData>();

            for (
                DateTime date = startDate;
                date <= endDate;
                date = date.AddDays(1))
            {
                float quantity = 0f;

                if (salesDictionary.TryGetValue(
                        date,
                        out float existingQuantity))
                {
                    quantity = existingQuantity;
                }

                dailySales.Add(
                    new SalesData
                    {
                        OrderDate = date,
                        Quantity = Math.Max(0f, quantity)
                    });
            }

            // ============================================================
            // 6. VERİ YETERLİLİK KONTROLÜ
            // ============================================================

            int seriesLengthTotal =
                dailySales.Count;

            if (seriesLengthTotal < 30)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA tahmini yapmak üzere yeterli günlük veri yok. " +
                    $"En az 30 takvim günü gerekiyor. " +
                    $"Mevcut gün sayısı: {seriesLengthTotal}");
            }

            int nonZeroDays =
                dailySales.Count(x => x.Quantity > 0);

            if (nonZeroDays < 10)
            {
                throw new InvalidOperationException(
                    $"{city} için yeterli gerçek satış günü yok. " +
                    $"En az 10 satış yapılan gün gerekiyor. " +
                    $"Mevcut satış günü: {nonZeroDays}");
            }

            // ============================================================
            // 7. SSA PARAMETRELERİ
            //
            // BURASI ÖNEMLİ
            //
            // seriesLength bütün veriyi zorunlu olarak almak yerine
            // modelin kullanacağı makul geçmiş pencereyi temsil eder.
            //
            // trainSize ise gerçek eğitim veri sayısıdır.
            //
            // Önceki hatalı kullanım:
            //
            // trainSize = seriesLength
            //
            // yerine:
            //
            // trainSize = dailySales.Count
            //
            // kullanıyoruz.
            // ============================================================

            int seriesLength =
                Math.Min(90, dailySales.Count);

            // seriesLength > windowSize olmalı.
            int windowSize =
                Math.Min(14, seriesLength / 2);

            windowSize =
                Math.Max(2, windowSize);

            // Güvenli kontrol
            if (windowSize >= seriesLength)
            {
                windowSize =
                    Math.Max(2, seriesLength - 1);
            }

            // Gerçek eğitim veri uzunluğu
            int trainSize =
                dailySales.Count;

            // ============================================================
            // 8. ML.NET DATA VIEW
            // ============================================================

            IDataView dataView =
                _mlContext.Data.LoadFromEnumerable(dailySales);

            // ============================================================
            // 9. SSA PIPELINE
            // ============================================================

            IEstimator<ITransformer> forecastingPipeline;

            try
            {
                forecastingPipeline =
                    _mlContext.Forecasting.ForecastBySsa(
                        outputColumnName:
                            nameof(SalesPrediction.ForecastedQuantity),

                        inputColumnName:
                            nameof(SalesData.Quantity),

                        windowSize:
                            windowSize,

                        seriesLength:
                            seriesLength,

                        trainSize:
                            trainSize,

                        horizon:
                            horizon,

                        confidenceLevel:
                            confidenceLevel,

                        confidenceLowerBoundColumn:
                            nameof(SalesPrediction.LowerBound),

                        confidenceUpperBoundColumn:
                            nameof(SalesPrediction.UpperBound));
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

            // ============================================================
            // 10. MODEL EĞİTİMİ
            // ============================================================

            ITransformer forecastModel;

            try
            {
                forecastModel =
                    forecastingPipeline.Fit(dataView);
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

            // ============================================================
            // 11. FORECAST ENGINE
            // ============================================================

            TimeSeriesPredictionEngine<
                SalesData,
                SalesPrediction> forecastingEngine;

            try
            {
                forecastingEngine =
                    forecastModel.CreateTimeSeriesEngine<
                        SalesData,
                        SalesPrediction>(
                        _mlContext);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA forecasting engine oluşturulamadı.",
                    ex);
            }

            // ============================================================
            // 12. TAHMİN
            // ============================================================

            SalesPrediction prediction;

            try
            {
                prediction =
                    forecastingEngine.Predict();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA tahmini oluşturulamadı.",
                    ex);
            }

            if (prediction == null)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA modeli boş sonuç döndürdü.");
            }

            // ============================================================
            // 13. TAHMİN ARRAY KONTROLLERİ
            // ============================================================

            if (prediction.ForecastedQuantity == null ||
                prediction.ForecastedQuantity.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{city} için SSA tahmin sonucu boş döndü.");
            }

            // ============================================================
            // 14. MODEL PARAMETRELERİ
            // ============================================================

            prediction.City =
                city;

            prediction.WindowSize =
                windowSize;

            prediction.SeriesLength =
                seriesLength;

            prediction.TrainSize =
                trainSize;

            prediction.Horizon =
                horizon;

            prediction.ConfidenceLevel =
                confidenceLevel;

            // ============================================================
            // 15. SON 30 GERÇEK GÜN
            // ============================================================

            prediction.HistoricalDetails =
                dailySales
                    .TakeLast(30)
                    .Select(x =>
                        new HistoricalDetailItem
                        {
                            Date =
                                x.OrderDate
                                    .ToString("yyyy-MM-dd"),

                            ActualSales =
                                x.Quantity
                        })
                    .ToList();

            // ============================================================
            // 16. SON GERÇEK TARİH
            // ============================================================

            DateTime lastHistoricalDate =
                dailySales.Last().OrderDate.Date;

            // ============================================================
            // 17. TAHMİN DETAYLARI
            // ============================================================

            prediction.Details =
                new List<ForecastDetailItem>();

            for (int i = 0; i < horizon; i++)
            {
                float rawForecast =
                    prediction.ForecastedQuantity.Length > i
                        ? prediction.ForecastedQuantity[i]
                        : 0f;

                float rawLower =
                    prediction.LowerBound != null &&
                    prediction.LowerBound.Length > i
                        ? prediction.LowerBound[i]
                        : rawForecast;

                float rawUpper =
                    prediction.UpperBound != null &&
                    prediction.UpperBound.Length > i
                        ? prediction.UpperBound[i]
                        : rawForecast;

                // NaN / Infinity koruması
                if (float.IsNaN(rawForecast) ||
                    float.IsInfinity(rawForecast))
                {
                    rawForecast = 0f;
                }

                if (float.IsNaN(rawLower) ||
                    float.IsInfinity(rawLower))
                {
                    rawLower = 0f;
                }

                if (float.IsNaN(rawUpper) ||
                    float.IsInfinity(rawUpper))
                {
                    rawUpper = rawForecast;
                }

                float forecastedSales =
                    Math.Max(
                        0f,
                        (float)Math.Round(
                            rawForecast,
                            2));

                float lowerBound =
                    Math.Max(
                        0f,
                        (float)Math.Round(
                            rawLower,
                            2));

                float upperBound =
                    Math.Max(
                        forecastedSales,
                        (float)Math.Round(
                            rawUpper,
                            2));

                prediction.Details.Add(
                    new ForecastDetailItem
                    {
                        Date =
                            lastHistoricalDate
                                .AddDays(i + 1)
                                .ToString("yyyy-MM-dd"),

                        PredictedSales =
                            forecastedSales,

                        LowerBound =
                            lowerBound,

                        UpperBound =
                            upperBound
                    });
            }

            return prediction;
        }

        // ============================================================
        // EXPECTED SALES
        // ============================================================
        // Classification / Anomaly gibi servislerde kullanılmak üzere
        // belirli şehir + tarih için beklenen günlük satış miktarını döndürür.
        //
        // NOT:
        // Mevcut Forecasting modeli şehir bazlı günlük toplam satış
        // kullandığı için ProductName burada filtre olarak kullanılmaz.
        // ============================================================

        public async Task<float?> GetExpectedSalesAsync(
            string city,
            string? productName,
            DateTime targetDate)
        {
            if (string.IsNullOrWhiteSpace(city))
                return null;

            if (targetDate == default)
                return null;

            try
            {
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

                var startDate = groupedSales.First().OrderDate.Date;
                var endDate = targetDate.Date;

                var salesDictionary = groupedSales
                    .ToDictionary(
                        x => x.OrderDate.Date,
                        x => x.Quantity);

                var dailySales = new List<SalesData>();

                for (
                    DateTime date = startDate;
                    date <= endDate;
                    date = date.AddDays(1))
                {
                    salesDictionary.TryGetValue(
                        date,
                        out int quantity);

                    dailySales.Add(
                        new SalesData
                        {
                            OrderDate = date,
                            Quantity = quantity
                        });
                }

                if (dailySales.Count < 30)
                    return null;

                int nonZeroDays =
                    dailySales.Count(x => x.Quantity > 0);

                if (nonZeroDays < 10)
                    return null;

                int seriesLength = dailySales.Count;

                int windowSize =
                    Math.Min(
                        14,
                        seriesLength / 2);

                windowSize =
                    Math.Max(
                        2,
                        windowSize);

                int trainSize = seriesLength;

                IDataView dataView =
                    _mlContext.Data.LoadFromEnumerable(
                        dailySales);

                var forecastingPipeline =
                    _mlContext.Forecasting.ForecastBySsa(
                        outputColumnName:
                            nameof(SalesPrediction.ForecastedQuantity),

                        inputColumnName:
                            nameof(SalesData.Quantity),

                        windowSize:
                            windowSize,

                        seriesLength:
                            seriesLength,

                        trainSize:
                            trainSize,

                        horizon:
                            1,

                        confidenceLevel:
                            0.95f,

                        confidenceLowerBoundColumn:
                            nameof(SalesPrediction.LowerBound),

                        confidenceUpperBoundColumn:
                            nameof(SalesPrediction.UpperBound));

                ITransformer model =
                    forecastingPipeline.Fit(dataView);

                var engine =
                    model.CreateTimeSeriesEngine<
                        SalesData,
                        SalesPrediction>(
                        _mlContext);

                var prediction =
                    engine.Predict();

                if (prediction?.ForecastedQuantity == null ||
                    prediction.ForecastedQuantity.Length == 0)
                {
                    return null;
                }

                float expectedSales =
                    prediction.ForecastedQuantity[0];

                if (float.IsNaN(expectedSales) ||
                    float.IsInfinity(expectedSales))
                {
                    return null;
                }

                return Math.Max(
                    0,
                    (float)Math.Round(
                        expectedSales,
                        2));
            }
            catch
            {
                return null;
            }
        }
    }
}