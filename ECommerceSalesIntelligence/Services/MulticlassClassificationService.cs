using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Models.Classification;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Services
{
    public class MulticlassClassificationService
    {
        private readonly AppDbContext _context;
        private readonly MLContext _mlContext;

        public MulticlassClassificationService(AppDbContext context, MLContext mlContext)
        {
            _context = context;
            _mlContext = mlContext;
        }

        public (MulticlassClassificationMetrics Metrics, List<MulticlassPredictionViewModel> Predictions) GetMulticlassDashboardData()
        {
            // Taraflı (biased) seçim yerine rastgele + yeterli büyüklükte örneklem
            var dbRecords = _context.SalesRecords
                .AsNoTracking()
                .OrderBy(x => Guid.NewGuid())
                .Take(3000)
                .ToList();

            var multiclassInputData = new List<SalesMulticlassInput>();

            foreach (var record in dbRecords)
            {
                string performanceLabel = record.Quantity switch
                {
                    < 4000 => "Low",
                    < 8000 => "Medium",
                    _ => "High"
                };

                multiclassInputData.Add(new SalesMulticlassInput
                {
                    UnitPrice = (float)record.UnitPrice,
                    Quantity = (float)record.Quantity,
                    DiscountRate = (float)record.DiscountRate,
                    IsCampaign = record.IsCampaign,
                    Label = performanceLabel
                });
            }

            if (!multiclassInputData.Any())
                return (null, new List<MulticlassPredictionViewModel>());

            // Güvenlik kontrolü: en az 2 farklı sınıf yoksa eğitim patlar, anlamlı hata fırlat
            var distinctLabels = multiclassInputData.Select(x => x.Label).Distinct().Count();
            if (distinctLabels < 2)
                throw new InvalidOperationException(
                    $"Eğitim verisinde yalnızca {distinctLabels} farklı sınıf var. Örneklem büyütülmeli veya sınıf sınırları veri dağılımına göre gözden geçirilmeli.");

            var dataView = _mlContext.Data.LoadFromEnumerable(multiclassInputData);
            var splitData = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(SalesMulticlassInput.UnitPrice),
                    nameof(SalesMulticlassInput.Quantity),
                    nameof(SalesMulticlassInput.DiscountRate))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var model = pipeline.Fit(splitData.TrainSet);

            var transformedTestSet = model.Transform(splitData.TestSet);
            var metrics = _mlContext.MulticlassClassification.Evaluate(transformedTestSet, labelColumnName: "Label", predictedLabelColumnName: "PredictedLabel");

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesMulticlassInput, SalesMulticlassPrediction>(model);

            // Dashboard tablosu için makul sayıda örnek göster (ör. ilk 100)
            var resultList = new List<MulticlassPredictionViewModel>();
            int index = 1;

            foreach (var item in multiclassInputData.Take(100))
            {
                var pred = predictionEngine.Predict(item);

                float confidence = 0.85f;
                if (pred.Score != null && pred.Score.Length > 0)
                {
                    float maxScore = pred.Score.Max();
                    confidence = 1.0f / (1.0f + (float)Math.Exp(-maxScore));
                }

                resultList.Add(new MulticlassPredictionViewModel
                {
                    Sku = $"SKU-{index++:000}",
                    PredictedVolume = item.Quantity,
                    Confidence = Math.Clamp(confidence, 0.5f, 0.99f),
                    DemandCategory = pred.PredictedLabel == "High" ? "YÜKSEK TALEP" : (pred.PredictedLabel == "Medium" ? "ORTA TALEP" : "DÜŞÜK TALEP")
                });
            }

            return (metrics, resultList);
        }
    }
}