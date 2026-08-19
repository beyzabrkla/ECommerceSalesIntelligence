using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Models.Classification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.ML;

namespace ECommerceSalesIntelligence.Services
{
    public class BinaryClassificationService
    {
        private readonly AppDbContext _context;
        private readonly MLContext _mlContext;
        private readonly IMemoryCache _memoryCache;

        private const string CacheKey = "BinaryClassificationDashboardCache_v3";
        private const float ClassificationThreshold = 7000f;

        public BinaryClassificationService(
            AppDbContext context,
            MLContext mlContext,
            IMemoryCache memoryCache)
        {
            _context = context;
            _mlContext = mlContext;
            _memoryCache = memoryCache;
        }

        public ClassificationDashboardViewModel GetBinaryDashboardData()
        {
            if (_memoryCache.TryGetValue(
                CacheKey,
                out ClassificationDashboardViewModel cachedModel))
            {
                return cachedModel;
            }

            var dbMonthlySales = _context.SalesRecords
                .AsNoTracking()
                .GroupBy(x => new
                {
                    City = x.City ?? "İstanbul",
                    Year = x.OrderDate.Year,
                    Month = x.OrderDate.Month
                })
                .Select(g => new
                {
                    g.Key.City,
                    g.Key.Year,
                    g.Key.Month,
                    TotalQuantity = g.Sum(x => (float)x.Quantity)
                })
                .OrderBy(x => x.City)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            var cityGroups = dbMonthlySales
                .GroupBy(x => x.City)
                .Where(g => g.Count() >= 4)
                .ToList();

            var classificationInputData = new List<SalesClassificationInput>();

            foreach (var cityGroup in cityGroups)
            {
                var sortedMonths = cityGroup
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                for (int i = 3; i < sortedMonths.Count; i++)
                {
                    var threeMonthsAgo = sortedMonths[i - 3];
                    var twoMonthsAgo = sortedMonths[i - 2];
                    var lastMonth = sortedMonths[i - 1];
                    var targetMonth = sortedMonths[i];

                    var expectedTargetDate = new DateTime(
                        lastMonth.Year,
                        lastMonth.Month,
                        1).AddMonths(1);

                    var actualTargetDate = new DateTime(
                        targetMonth.Year,
                        targetMonth.Month,
                        1);

                    if (actualTargetDate != expectedTargetDate)
                        continue;

                    float threeMonthAverage =
                        (
                            threeMonthsAgo.TotalQuantity +
                            twoMonthsAgo.TotalQuantity +
                            lastMonth.TotalQuantity
                        ) / 3f;

                    float targetQuantity = targetMonth.TotalQuantity;
                    bool label = targetQuantity >= ClassificationThreshold;

                    classificationInputData.Add(new SalesClassificationInput
                    {
                        City = cityGroup.Key,
                        ProductName = "Genel Ürün",
                        ThreeMonthsAgo = threeMonthsAgo.TotalQuantity,
                        TwoMonthsAgo = twoMonthsAgo.TotalQuantity,
                        LastMonth = lastMonth.TotalQuantity,
                        ThreeMonthAverage = (float)Math.Round(threeMonthAverage, 1),
                        TargetMonth = actualTargetDate.ToString("yyyy-MM"),
                        TargetQuantity = targetQuantity,
                        Label = label
                    });
                }
            }

            if (!classificationInputData.Any())
            {
                return new ClassificationDashboardViewModel
                {
                    Metrics = null,
                    Predictions = new List<SalesClassificationPrediction>(),
                    Threshold = ClassificationThreshold
                };
            }

            int trueCount = classificationInputData.Count(x => x.Label);
            int falseCount = classificationInputData.Count(x => !x.Label);

            if (trueCount == 0 || falseCount == 0)
            {
                return new ClassificationDashboardViewModel
                {
                    Metrics = null,
                    Predictions = new List<SalesClassificationPrediction>(),
                    Threshold = ClassificationThreshold
                };
            }

            var dataView = _mlContext.Data.LoadFromEnumerable(classificationInputData);

            var splitData = _mlContext.Data.TrainTestSplit(
                dataView,
                testFraction: 0.20,
                seed: 42);

            var pipeline = _mlContext.Transforms
                .Concatenate(
                    "Features",
                    nameof(SalesClassificationInput.ThreeMonthsAgo),
                    nameof(SalesClassificationInput.TwoMonthsAgo),
                    nameof(SalesClassificationInput.LastMonth),
                    nameof(SalesClassificationInput.ThreeMonthAverage))
                .Append(
                    _mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(
                    _mlContext.BinaryClassification.Trainers
                        .SdcaLogisticRegression(
                            labelColumnName: "Label",
                            featureColumnName: "Features"));

            var model = pipeline.Fit(splitData.TrainSet);

            var transformedTestSet = model.Transform(splitData.TestSet);

            var metrics = _mlContext.BinaryClassification.Evaluate(
                transformedTestSet,
                labelColumnName: "Label");

            var predictionEngine =
                _mlContext.Model.CreatePredictionEngine<
                    SalesClassificationInput,
                    SalesClassificationPrediction>(model);

            var latestInputs = classificationInputData
                .GroupBy(x => x.City)
                .Select(g => g
                    .OrderByDescending(x => x.TargetMonth)
                    .First())
                .ToList();

            var predictionList = latestInputs
                .Select(input =>
                {
                    var prediction = predictionEngine.Predict(input);

                    return new SalesClassificationPrediction
                    {
                        City = input.City,
                        ThreeMonthsAgo = input.ThreeMonthsAgo,
                        TwoMonthsAgo = input.TwoMonthsAgo,
                        LastMonth = input.LastMonth,
                        ThreeMonthAverage = input.ThreeMonthAverage,
                        PredictedLabel = prediction.PredictedLabel,
                        Probability = prediction.Probability,
                        Score = prediction.Score
                    };
                })
                .OrderByDescending(x => x.Probability)
                .ToList();

            var viewModel = new ClassificationDashboardViewModel
            {
                Metrics = metrics,
                Predictions = predictionList,
                Threshold = ClassificationThreshold
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _memoryCache.Set(CacheKey, viewModel, cacheOptions);

            return viewModel;
        }
    }
}