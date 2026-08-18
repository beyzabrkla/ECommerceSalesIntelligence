using AutoMapper;
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
        private readonly IMapper _mapper;
        private readonly MLContext _mlContext;

        public ForecastingService(AppDbContext context, IMapper mapper, MLContext mlContext)
        {
            _context = context;
            _mapper = mapper;
            _mlContext = mlContext;
        }

        public async Task<SalesPrediction> PredictNextDaysAsync(string city, int horizon = 7, float confidenceLevel = 0.95f)
        {
            // Şehre ait satış kayıtlarını getir
            var rawSales = await _context.SalesRecords
                .Where(s => s.City == city)
                .OrderBy(s => s.OrderDate)
                .ToListAsync();

            if (rawSales == null || rawSales.Count == 0)
            {
                throw new Exception($"{city} için satış verisi bulunamadı.");
            }

            // Satışları gün bazında topla
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
                    $"En az 14 farklı gün veri gerekiyor. Mevcut gün sayısı: {dailySales.Count}"
                );
            }

            IDataView dataView = _mlContext.Data.LoadFromEnumerable(dailySales);

            int seriesLength = dailySales.Count;

            // Pencere Boyutunu (Window Size) 14 gün olarak belirliyoruz (Veri kümesi küçükse serinin yarısını geçmeyecek şekilde)
            int windowSize = Math.Min(14, seriesLength / 2);
            if (windowSize < 2) windowSize = 2;

            int trainSize = seriesLength;

            // SSA Model Tanımı
            var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(SalesPrediction.ForecastedQuantity),
                inputColumnName: nameof(SalesData.Quantity),
                windowSize: windowSize,
                seriesLength: seriesLength,
                trainSize: trainSize,
                horizon: horizon,
                confidenceLevel: confidenceLevel,
                confidenceLowerBoundColumn: nameof(SalesPrediction.LowerBound),
                confidenceUpperBoundColumn: nameof(SalesPrediction.UpperBound));

            SsaForecastingTransformer forecastModel = forecastingPipeline.Fit(dataView);
            var forecastingEngine = forecastModel.CreateTimeSeriesEngine<SalesData, SalesPrediction>(_mlContext);

            SalesPrediction prediction = forecastingEngine.Predict();

            prediction.WindowSize = windowSize;
            prediction.SeriesLength = seriesLength;
            prediction.TrainSize = trainSize;
            prediction.Horizon = horizon;
            prediction.ConfidenceLevel = confidenceLevel;

            // 1. SON 14 GÜNÜN GEÇMİŞ SATIŞLARINI DOLDUR
            var last14Days = dailySales.TakeLast(14).ToList();
            prediction.HistoricalDetails = last14Days.Select(h => new HistoricalDetailItem
            {
                Date = h.OrderDate.ToString("yyyy-MM-dd"),
                ActualSales = (float)Math.Round(h.Quantity, 2)
            }).ToList();

            // 2. GELECEK 7 GÜNÜN TAHMİNLERİNİ DOLDUR
            DateTime lastHistoricalDate = dailySales.Last().OrderDate;
            prediction.Details = new List<ForecastDetailItem>();

            for (int i = 0; i < horizon; i++)
            {
                float rawLower = prediction.LowerBound.Length > i ? prediction.LowerBound[i] : 0;
                float rawUpper = prediction.UpperBound.Length > i ? prediction.UpperBound[i] : 0;
                float rawForecast = prediction.ForecastedQuantity.Length > i ? prediction.ForecastedQuantity[i] : 0;

                // Mantıksız eksi değerleri engellemek için Alt Sınır ve Tahmin en az 0'a sabitlenir
                float lowerBound = Math.Max(0, (float)Math.Round(rawLower, 2));
                float forecastedSales = Math.Max(0, (float)Math.Round(rawForecast, 2));
                float upperBound = Math.Max(forecastedSales, (float)Math.Round(rawUpper, 2));

                prediction.Details.Add(new ForecastDetailItem
                {
                    Date = lastHistoricalDate.AddDays(i + 1).ToString("yyyy-MM-dd"),
                    PredictedSales = forecastedSales,
                    LowerBound = lowerBound,
                    UpperBound = upperBound
                });
            }

            return prediction;
        }
    }
}