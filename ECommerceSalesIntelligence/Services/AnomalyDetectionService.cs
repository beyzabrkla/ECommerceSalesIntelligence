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

        public AnomalyDetectionService(AppDbContext context, MLContext mlContext)
        {
            _context = context;
            _mlContext = mlContext;
        }

        public List<SalesAnomalyResultViewModel> DetectAnomalies()
        {
            // Günlük bazda toplam satışları gruplayarak çekiyoruz
            var dailySales = _context.SalesRecords
                .GroupBy(s => s.OrderDate.Date)
                .Select(g => new SalesAnomalyInput
                {
                    OrderDate = g.Key,
                    TotalAmount = (float)g.Sum(s => s.TotalAmount)
                })
                .OrderBy(s => s.OrderDate)
                .ToList();

            IDataView dataView = _mlContext.Data.LoadFromEnumerable(dailySales);

            // Anomali Tespiti Pipeline Yapısı (IidSpikeEstimator - Ani Sıçrama Tespiti)
            // pValue: Güven eşik değeri, confidence: Güven aralığı
            var pipeline = _mlContext.Transforms.DetectIidSpike(
                outputColumnName: "Prediction",
                inputColumnName: nameof(SalesAnomalyInput.TotalAmount),
                confidence: 95.0,
                pvalueHistoryLength: 30);

            var model = pipeline.Fit(dataView);
            var transformedData = model.Transform(dataView);

            // Sonuçları listeye aktarıyoruz
            var predictions = _mlContext.Data.CreateEnumerable<SalesAnomalyPrediction>(transformedData, reuseRowObject: false).ToList();
            var inputList = dailySales;

            var results = new List<SalesAnomalyResultViewModel>();

            for (int i = 0; i < predictions.Count; i++)
            {
                // Prediction[0] -> 1 ise anomali var (ani patlama/çöküş), 0 ise normal
                if (predictions[i].Prediction[0] == 1)
                {
                    results.Add(new SalesAnomalyResultViewModel
                    {
                        OrderDate = inputList[i].OrderDate,
                        TotalAmount = inputList[i].TotalAmount,
                        IsAnomaly = true,
                        Score = (float)predictions[i].Prediction[1]
                    });
                }
            }

            return results;
        }
    }
}