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

        public async Task<SalesPrediction> PredictNextDaysAsync(
            string city,
            int horizon = 7,
            float confidenceLevel = 0.95f)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException(
                    "Şehir bilgisi boş olamaz.",
                    nameof(city));

            if (horizon < 1)
                throw new ArgumentException(
                    "Tahmin ufku en az 1 gün olmalıdır.",
                    nameof(horizon));

            if (confidenceLevel <= 0 || confidenceLevel >= 1)
                throw new ArgumentException(
                    "Güven düzeyi 0 ile 1 arasında olmalıdır.",
                    nameof(confidenceLevel));

            var rawSales = await _context.SalesRecords
                .AsNoTracking()
                .Where(x => x.City == city)
                .OrderBy(x => x.OrderDate)
                .ToListAsync();

            if (rawSales.Count == 0)
            {
                throw new Exception(
                    $"{city} için satış verisi bulunamadı.");
            }

            var dailySales = rawSales
                .GroupBy(x => x.OrderDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new SalesData
                {
                    OrderDate = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            if (dailySales.Count < 14)
            {
                throw new Exception(
                    $"{city} için SSA tahmini yapmak üzere yeterli günlük veri yok. " +
                    $"En az 14 farklı gün gerekiyor. " +
                    $"Mevcut gün sayısı: {dailySales.Count}");
            }

            var dataView =
                _mlContext.Data.LoadFromEnumerable(
                    dailySales);

            int seriesLength = dailySales.Count;

            int windowSize = Math.Min(
                14,
                seriesLength / 2);

            if (windowSize < 2)
                windowSize = 2;

            int trainSize = seriesLength;

            var forecastingPipeline =
                _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName:
                        nameof(
                            SalesPrediction.ForecastedQuantity),

                    inputColumnName:
                        nameof(
                            SalesData.Quantity),

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
                        nameof(
                            SalesPrediction.LowerBound),

                    confidenceUpperBoundColumn:
                        nameof(
                            SalesPrediction.UpperBound));

            var forecastModel =
                forecastingPipeline.Fit(dataView);

            var forecastingEngine =
                forecastModel.CreateTimeSeriesEngine<
                    SalesData,
                    SalesPrediction>(
                    _mlContext);

            var prediction =
                forecastingEngine.Predict();

            prediction.WindowSize = windowSize;
            prediction.SeriesLength = seriesLength;
            prediction.TrainSize = trainSize;
            prediction.Horizon = horizon;
            prediction.ConfidenceLevel = confidenceLevel;

            var last14Days = dailySales
                .TakeLast(14)
                .ToList();

            prediction.HistoricalDetails =
                last14Days
                    .Select(x => new HistoricalDetailItem
                    {
                        Date = x.OrderDate
                            .ToString("yyyy-MM-dd"),

                        ActualSales =
                            (float)Math.Round(
                                x.Quantity,
                                2)
                    })
                    .ToList();

            var lastHistoricalDate =
                dailySales.Last().OrderDate;

            prediction.Details =
                new List<ForecastDetailItem>();

            for (int i = 0; i < horizon; i++)
            {
                float rawLower =
                    prediction.LowerBound.Length > i
                        ? prediction.LowerBound[i]
                        : 0;

                float rawUpper =
                    prediction.UpperBound.Length > i
                        ? prediction.UpperBound[i]
                        : 0;

                float rawForecast =
                    prediction.ForecastedQuantity.Length > i
                        ? prediction.ForecastedQuantity[i]
                        : 0;

                float lowerBound =
                    Math.Max(
                        0,
                        (float)Math.Round(
                            rawLower,
                            2));

                float forecastedSales =
                    Math.Max(
                        0,
                        (float)Math.Round(
                            rawForecast,
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
                        Date = lastHistoricalDate
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
    }
}