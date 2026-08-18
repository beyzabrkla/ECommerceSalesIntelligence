using AutoMapper;
using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace ECommerceSalesIntelligence.Services
{
    public class MulticlassClassificationService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly MLContext _mlContext;

        public MulticlassClassificationService(AppDbContext context, IMapper mapper, MLContext mlContext)
        {
            _context = context;
            _mapper = mapper;
            _mlContext = mlContext;
        }

        public SalesMulticlassPrediction TrainAndEvaluate()
        {
            IEnumerable<SalesMulticlassInput> GetStreamingData()
            {
                foreach (var record in _context.SalesRecords.AsNoTracking().AsEnumerable())
                {
                    // Satış miktarına göre Low, Medium, High sınırlarını belirliyoruz
                    string performanceLabel = record.Quantity switch
                    {
                        < 5 => "Low",
                        < 20 => "Medium",
                        _ => "High"
                    };

                    var input = _mapper.Map<SalesMulticlassInput>(record);
                    input.Label = performanceLabel;
                    yield return input;
                }
            }

            IDataView dataView = _mlContext.Data.LoadFromEnumerable<SalesMulticlassInput>(GetStreamingData());

            // Train / Test Ayrımı (%80 Eğitim, %20 Test)
            var splitData = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            // Pipeline: Özellikleri birleştir ve Multiclass eğitmeni seç
            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(SalesMulticlassInput.UnitPrice),
                    nameof(SalesMulticlassInput.Quantity),
                    nameof(SalesMulticlassInput.DiscountRate))
                // Metin etiketlerini sayısal anahtarlara dönüştürüyoruz
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label"))
                // Çoklu sınıflandırma için SdcaMaximumEntropy algoritması
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                // Tahmin edilen sayısal anahtarı tekrar orijinal metne ("Low", "Medium", "High") çeviriyoruz
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var model = pipeline.Fit(splitData.TrainSet);

            // Değerlendirme Metrikleri
            var transformedTestSet = model.Transform(splitData.TestSet);
            var metrics = _mlContext.MulticlassClassification.Evaluate(transformedTestSet);

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesMulticlassInput, SalesMulticlassPrediction>(model);

            var sample = new SalesMulticlassInput
            {
                UnitPrice = 1200f,
                Quantity = 15f,
                DiscountRate = 0.15f,
                IsCampaign = true
            };

            return predictionEngine.Predict(sample);
        }
    }
}