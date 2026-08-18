using AutoMapper;
using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.TimeSeries;
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

        public async Task<SalesPrediction> PredictNext7DaysAsync(string city) // Şehir bazlı satış tahmini
        {
            //Veritabanından seçilen şehrin ham satış kayıtlarını çekiyoruz.
            var rawSales = await _context.SalesRecords
                .Where(s => s.City == city)
                .OrderBy(s => s.OrderDate)
                .ToListAsync();

            if (rawSales == null || rawSales.Count == 0)
            {
                return new SalesPrediction();
            }

            var dailySales = _mapper.Map<List<SalesData>>(rawSales); 

            //Veriyi ML.NET DataView formatına yüklüyoruz
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(dailySales);

            //ML.NET SSA (Singular Spectrum Analysis) Pipeline Yapılandırması
            int horizon = 7; // Önümüzdeki tahmin edilecek gün sayısı
            int seriesLength = dailySales.Count > 365 ? 365 : dailySales.Count;
            int windowSize = 7; // Haftalık mevsimsellik döngüsü

            var pipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(SalesPrediction.ForecastedQuantity),
                inputColumnName: nameof(SalesData.Quantity),
                windowSize: windowSize,
                seriesLength: seriesLength,
                trainSize: dailySales.Count,
                horizon: horizon,
                confidenceLevel: 0.95f, // %95 Güven aralığı
                confidenceLowerBoundColumn: nameof(SalesPrediction.LowerBound),
                confidenceUpperBoundColumn: nameof(SalesPrediction.UpperBound)
            );

            //Modelin eğitilmesi
            var model = pipeline.Fit(dataView);

            //Tahmin motorunun çalıştırılması
            var forecastingEngine = model.CreateTimeSeriesEngine<SalesData, SalesPrediction>(_mlContext);
            var prediction = forecastingEngine.Predict();

            return prediction;
        }
    }
}