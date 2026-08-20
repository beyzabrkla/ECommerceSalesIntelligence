using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace ECommerceSalesIntelligence.Services
{
    public class AnomalyDetectionService
    {
        private readonly AppDbContext _context;
        private readonly MLContext _mlContext;
        private readonly ForecastingService _forecastingService;

        public AnomalyDetectionService(
            AppDbContext context,
            MLContext mlContext,
            ForecastingService forecastingService)
        {
            _context = context;
            _mlContext = mlContext;
            _forecastingService = forecastingService;
        }

        public async Task<List<SalesAnomalyResultViewModel>>
            DetectAnomaliesAsync()
        {
            // ============================================================
            // 1. GÜNLÜK SATIŞLARI AL
            // ============================================================

            var dailySales =
                await _context.SalesRecords
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

            if (!dailySales.Any())
            {
                return new List<SalesAnomalyResultViewModel>();
            }

            // ============================================================
            // 2. ÜLKE + ŞEHİR + ÜRÜN GRUPLARI
            // ============================================================

            var groups =
                dailySales
                    .GroupBy(x => new
                    {
                        x.Country,
                        x.City,
                        x.ProductName
                    })
                    .ToList();

            var results =
                new List<SalesAnomalyResultViewModel>();

            // ============================================================
            // 3. HER SERİYİ ANALİZ ET
            // ============================================================

            foreach (var group in groups)
            {
                var rawSeries =
                    group
                        .OrderBy(x => x.OrderDate)
                        .ToList();

                if (rawSeries.Count < 60)
                {
                    continue;
                }

                // ========================================================
                // TAKVİM GÜNLERİNİ TAMAMLA
                // ========================================================

                DateTime startDate =
                    rawSeries.Min(x => x.OrderDate).Date;

                DateTime endDate =
                    rawSeries.Max(x => x.OrderDate).Date;

                var salesByDate =
                    rawSeries.ToDictionary(
                        x => x.OrderDate.Date,
                        x => x.Quantity);

                var series =
                    new List<DailySalesGroup>();

                for (
                    var date = startDate;
                    date <= endDate;
                    date = date.AddDays(1))
                {
                    salesByDate.TryGetValue(
                        date,
                        out float quantity);

                    series.Add(
                        new DailySalesGroup
                        {
                            Country = group.Key.Country,
                            City = group.Key.City,
                            ProductName = group.Key.ProductName,
                            OrderDate = date,
                            Quantity = quantity
                        });
                }

                // ========================================================
                // EN AZ 60 GÜN
                // ========================================================

                if (series.Count < 60)
                {
                    continue;
                }

                // ========================================================
                // EN AZ 30 SATIŞ GÜNÜ
                // ========================================================

                if (series.Count(x => x.Quantity > 0) < 30)
                {
                    continue;
                }

                // ========================================================
                // ML.NET INPUT
                // ========================================================

                var inputs =
                    series.Select(x =>
                        new SalesAnomalyInput
                        {
                            OrderDate = x.OrderDate,
                            Quantity = x.Quantity
                        })
                    .ToList();

                IDataView dataView =
                    _mlContext.Data
                        .LoadFromEnumerable(inputs);

                // ========================================================
                // SSA
                // ========================================================

                const int trainingWindowSize = 60;
                const int seasonalityWindowSize = 7;
                const double confidence = 95.0;
                const int pvalueHistoryLength = 30;

                ITransformer model;

                try
                {
                    var pipeline =
                        _mlContext.Transforms.DetectSpikeBySsa(
                            outputColumnName: "Prediction",
                            inputColumnName:
                                nameof(
                                    SalesAnomalyInput.Quantity),
                            confidence: confidence,
                            pvalueHistoryLength:
                                pvalueHistoryLength,
                            trainingWindowSize:
                                trainingWindowSize,
                            seasonalityWindowSize:
                                seasonalityWindowSize);

                    model = pipeline.Fit(dataView);
                }
                catch
                {
                    continue;
                }

                // ========================================================
                // TRANSFORM
                // ========================================================

                IDataView transformedData;

                try
                {
                    transformedData =
                        model.Transform(dataView);
                }
                catch
                {
                    continue;
                }

                List<SalesAnomalyPrediction> predictions;

                try
                {
                    predictions =
                        _mlContext.Data
                            .CreateEnumerable<
                                SalesAnomalyPrediction>(
                                transformedData,
                                reuseRowObject: false)
                            .ToList();
                }
                catch
                {
                    continue;
                }

                // ========================================================
                // ANOMALİLER
                // ========================================================

                int count =
                    Math.Min(
                        series.Count,
                        predictions.Count);

                for (int i = 0; i < count; i++)
                {
                    var prediction =
                        predictions[i];

                    if (prediction.Prediction == null ||
                        prediction.Prediction.Length < 3)
                    {
                        continue;
                    }

                    bool isAnomaly =
                        prediction.Prediction[0] == 1;

                    if (!isAnomaly)
                    {
                        continue;
                    }

                    double score =
                        prediction.Prediction[1];

                    double pValue =
                        prediction.Prediction[2];

                    // ====================================================
                    // P VALUE
                    // ====================================================

                    if (pValue > 0.05)
                    {
                        continue;
                    }

                    var current =
                        series[i];

                    // ====================================================
                    // FORECASTING
                    // ====================================================

                    float? expectedSales = null;

                    try
                    {
                        expectedSales =
                            await _forecastingService
                                .GetExpectedSalesAsync(
                                    current.City,
                                    current.ProductName,
                                    current.OrderDate);
                    }
                    catch
                    {
                        // Forecasting başarısızsa
                        // anomaly tamamen kaybolmasın.
                        expectedSales = null;
                    }

                    // ====================================================
                    // FORECAST YOKSA
                    // SON 7 GÜNLÜK GEÇMİŞ ORTALAMA
                    // ====================================================

                    float expected;

                    if (expectedSales.HasValue)
                    {
                        expected =
                            expectedSales.Value;
                    }
                    else
                    {
                        var previousValues =
                            series
                                .Where(x =>
                                    x.OrderDate <
                                    current.OrderDate &&
                                    x.OrderDate >=
                                    current.OrderDate.AddDays(-7))
                                .Select(x => x.Quantity)
                                .ToList();

                        if (!previousValues.Any())
                        {
                            continue;
                        }

                        expected =
                            previousValues.Average();
                    }

                    // ====================================================
                    // GERÇEK SATIŞ
                    // ====================================================

                    float actual =
                        current.Quantity;

                    // ====================================================
                    // FARK
                    // ====================================================

                    float change =
                        actual - expected;

                    // ====================================================
                    // YÜZDE
                    // ====================================================

                    float changePercentage = 0;

                    if (expected > 0)
                    {
                        changePercentage =
                            (change / expected) * 100f;
                    }

                    // ====================================================
                    // DURUM
                    // ====================================================

                    string status =
                        GetAnomalyStatus(
                            actual,
                            expected);

                    // ====================================================
                    // SEVİYE
                    // ====================================================

                    string severity =
                        GetSeverity(
                            Math.Abs(changePercentage));

                    // ====================================================
                    // SONUÇ
                    // ====================================================

                    results.Add(
                        new SalesAnomalyResultViewModel
                        {
                            OrderDate =
                                current.OrderDate,

                            Country =
                                current.Country,

                            City =
                                current.City,

                            ProductName =
                                current.ProductName,

                            Quantity =
                                actual,

                            ExpectedSales =
                                expected,

                            Change =
                                change,

                            ChangePercentage =
                                changePercentage,

                            Score =
                                (float)score,

                            PValue =
                                (float)pValue,

                            IsAnomaly =
                                true,

                            Status =
                                status,

                            Severity =
                                severity
                        });
                }
            }

            // ============================================================
            // 4. SIRALA
            // ============================================================

            return results
                .OrderByDescending(x =>
                    Math.Abs(x.ChangePercentage))
                .ThenByDescending(x =>
                    x.OrderDate)
                .ToList();
        }

        // ================================================================
        // ANOMALİ DURUMU
        // ================================================================

        private static string GetAnomalyStatus(
            float actual,
            float expected)
        {
            if (expected <= 0)
            {
                return actual > 0
                    ? "SIÇRAMA"
                    : "ANOMALİ";
            }

            if (actual > expected)
            {
                return "SIÇRAMA";
            }

            if (actual < expected)
            {
                return "DÜŞÜŞ";
            }

            return "ANOMALİ";
        }

        // ================================================================
        // ŞİDDET
        // ================================================================

        private static string GetSeverity(
            float percentage)
        {
            if (percentage >= 100)
            {
                return "KRİTİK";
            }

            if (percentage >= 50)
            {
                return "YÜKSEK";
            }

            if (percentage >= 25)
            {
                return "ORTA";
            }

            return "DÜŞÜK";
        }

        // ================================================================
        // INTERNAL MODEL
        // ================================================================

        private class DailySalesGroup
        {
            public string Country { get; set; }
                = string.Empty;

            public string City { get; set; }
                = string.Empty;

            public string ProductName { get; set; }
                = string.Empty;

            public DateTime OrderDate { get; set; }

            public float Quantity { get; set; }
        }
    }
}