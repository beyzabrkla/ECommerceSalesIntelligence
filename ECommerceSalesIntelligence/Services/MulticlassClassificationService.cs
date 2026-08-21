using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Models.Classification;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

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

        /// Şehir ve ürün bazında gelecek ay satış performansını Low, Medium veya High sınıfına ayırır
        public MulticlassClassificationViewModel GetMulticlassDashboardData()
        {
            // SQL verileri şehir, ürün ve ay bazında aylık satışlara dönüştürülür
            var monthlySales = _context.SalesRecords
                .AsNoTracking()
                .Select(x => new
                {
                    City = string.IsNullOrWhiteSpace(x.City) ? "Bilinmeyen Şehir" : x.City.Trim(),
                    ProductName = string.IsNullOrWhiteSpace(x.ProductName) ? "Bilinmeyen Ürün" : x.ProductName.Trim(),
                    x.OrderDate,
                    x.Quantity
                })
                .ToList()
                .GroupBy(x => new
                {
                    x.City,
                    x.ProductName,
                    Year = x.OrderDate.Year,
                    Month = x.OrderDate.Month
                })
                .Select(g => new
                {
                    g.Key.City,
                    g.Key.ProductName,
                    g.Key.Year,
                    g.Key.Month,
                    TotalQuantity = g.Sum(x => (float)x.Quantity)
                })
                .OrderBy(x => x.City)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            if (!monthlySales.Any())
                return new MulticlassClassificationViewModel();

            // En az dört aylık geçmişi bulunan şehir-ürün grupları kullanılır
            var groups = monthlySales
                .GroupBy(x => new { x.City, x.ProductName })
                .Where(g => g.Count() >= 4)
                .ToList();

            if (!groups.Any())
                return new MulticlassClassificationViewModel();

            // Modelin eğitiminde kullanılacak geçmiş örnekler hazırlanır
            var trainingData = new List<SalesMulticlassInput>();

            foreach (var group in groups)
            {
                var months = group
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                // En az üç geçmiş ay kullanılarak sonraki ay hedef oluşturulur
                for (int i = 3; i < months.Count; i++)
                {
                    var threeMonthsAgo = months[i - 3];
                    var twoMonthsAgo = months[i - 2];
                    var lastMonth = months[i - 1];
                    var targetMonth = months[i];

                    var expectedTargetDate = new DateTime(
                        lastMonth.Year,
                        lastMonth.Month,
                        1).AddMonths(1);

                    var actualTargetDate = new DateTime(
                        targetMonth.Year,
                        targetMonth.Month,
                        1);

                    // Arada eksik ay varsa bu örnek eğitimden çıkarılır
                    if (actualTargetDate != expectedTargetDate)
                        continue;

                    // Son üç ayın ortalama satış değeri hesaplanır
                    float average = (
                        threeMonthsAgo.TotalQuantity +
                        twoMonthsAgo.TotalQuantity +
                        lastMonth.TotalQuantity) / 3f;

                    if (average <= 0)
                        continue;

                    // Geçmiş satış davranışını açıklayan özellikler hesaplanır
                    float lastMonthGrowthRate = SafeRate(lastMonth.TotalQuantity, twoMonthsAgo.TotalQuantity); // Son ayın bir önceki aya göre büyüme oranı

                    float twoMonthGrowthRate = SafeRate(twoMonthsAgo.TotalQuantity, threeMonthsAgo.TotalQuantity); // İki ay önceki ayın bir önceki aya göre büyüme oranı

                    float lastMonthVsAverageRate = SafeRate(lastMonth.TotalQuantity, average); // Son ayın ortalama satışa göre büyüme oranı

                    float trendSlope = CalculateTrendSlope(threeMonthsAgo.TotalQuantity, twoMonthsAgo.TotalQuantity, lastMonth.TotalQuantity); // Trend eğimi

                    // Gerçek hedef satış yalnızca sınıf etiketi oluşturmak için kullanılır
                    float targetPerformanceRatio = targetMonth.TotalQuantity / average;

                    trainingData.Add(new SalesMulticlassInput
                    {
                        City = group.Key.City,
                        ProductName = group.Key.ProductName,
                        ThreeMonthsAgo = threeMonthsAgo.TotalQuantity,
                        TwoMonthsAgo = twoMonthsAgo.TotalQuantity,
                        LastMonth = lastMonth.TotalQuantity,
                        ThreeMonthAverage = average,
                        LastMonthGrowthRate = lastMonthGrowthRate,
                        TwoMonthGrowthRate = twoMonthGrowthRate,
                        LastMonthVsAverageRate = lastMonthVsAverageRate,
                        TrendSlope = trendSlope,
                        TargetMonthNumber = actualTargetDate.Month,
                        TargetMonth = actualTargetDate.ToString("yyyy-MM"),
                        TargetPerformanceRatio = targetPerformanceRatio,
                        TargetQuantity = targetMonth.TotalQuantity,
                        Label = string.Empty
                    });
                }
            }

            if (!trainingData.Any())
                return new MulticlassClassificationViewModel();

            // Performans oranları sıralanarak veri dağılımı elde edilir
            var performanceValues = trainingData.Select(x => x.TargetPerformanceRatio).OrderBy(x => x).ToList();

            // P33 ve P66 değerleri, performans oranlarının sıralı dağılımında %33 ve %66 noktalarını temsil eder
            // Bu noktalar, veriyi üç eşit parçaya böler ve her parçaya bir sınıf atanır: Low, Medium ve High
            double p33 = GetPercentile(performanceValues, 0.3333);
            double p66 = GetPercentile(performanceValues, 0.6667);

            // Her eğitim örneği Low, Medium veya High sınıfına atanır
            foreach (var item in trainingData)
            {
                item.Label = item.TargetPerformanceRatio < p33 ? "Low" : item.TargetPerformanceRatio < p66 ? "Medium" : "High";
            }

            // Her sınıftaki örnek sayısı hesaplanır
            int lowCount = trainingData.Count(x => x.Label == "Low");
            int mediumCount = trainingData.Count(x => x.Label == "Medium");
            int highCount = trainingData.Count(x => x.Label == "High");

            // Üç sınıfın da oluşması modelin eğitimi için kontrol edilir
            if (lowCount == 0 || mediumCount == 0 || highCount == 0)
            {
                throw new InvalidOperationException("Low, Medium ve High sınıflarının üçü de oluşmadı. " + $"Low sınırı: {p33:N2}, High sınırı: {p66:N2}");
            }

            // Eğitim listesi ML.NET IDataView yapısına dönüştürülür
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Veriler eğitim ve test olarak ikiye ayrılır
            var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.20, seed: 42); // testFraction, verinin %20'sinin test seti olarak ayrılacağını belirtir
                                                                                                //seed, rastgele bölme işleminin tekrarlanabilir olmasını sağlar
                                                                                                //42 olmasının sebebi, 42 popüler bir "evrensel cevap" olarak kabul edilir ve rastgele sayı üretiminde sıkça kullanılır

            // Eğitim ve test kayıtlarının sayısı belirlenir
            int trainCount = _mlContext.Data.CreateEnumerable<SalesMulticlassInput>(split.TrainSet, reuseRowObject: false).Count(); 
            int testCount = _mlContext.Data.CreateEnumerable<SalesMulticlassInput>(split.TestSet, reuseRowObject: false).Count(); 

            // Kategorik şehir ve ürün bilgileri sayısal özelliklere dönüştürülür
            var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "CityEncoded",
                    inputColumnName: nameof(SalesMulticlassInput.City))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "ProductEncoded",
                    inputColumnName: nameof(SalesMulticlassInput.ProductName)))
                .Append(_mlContext.Transforms.Concatenate(
                    "NumericFeatures",
                    nameof(SalesMulticlassInput.ThreeMonthsAgo),
                    nameof(SalesMulticlassInput.TwoMonthsAgo),
                    nameof(SalesMulticlassInput.LastMonth),
                    nameof(SalesMulticlassInput.ThreeMonthAverage),
                    nameof(SalesMulticlassInput.LastMonthGrowthRate),
                    nameof(SalesMulticlassInput.TwoMonthGrowthRate),
                    nameof(SalesMulticlassInput.LastMonthVsAverageRate),
                    nameof(SalesMulticlassInput.TrendSlope),
                    nameof(SalesMulticlassInput.TargetMonthNumber)))
                .Append(_mlContext.Transforms.NormalizeMinMax(
                    "NumericFeatures"))
                .Append(_mlContext.Transforms.Concatenate(
                    "Features",
                    "CityEncoded",
                    "ProductEncoded",
                    "NumericFeatures"))
                // Low, Medium ve High metinleri modelin anlayacağı key değerlerine çevrilir
                .Append(_mlContext.Transforms.Conversion.MapValueToKey(
                    outputColumnName: "LabelKey",
                    inputColumnName: nameof(SalesMulticlassInput.Label)))
                // Üç sınıflı tahmin için Maximum Entropy modeli kullanılır
                .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(
                    labelColumnName: "LabelKey",
                    featureColumnName: "Features"))
                // Tahmin edilen key tekrar Low/Medium/High değerine çevrilir
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                    outputColumnName: "PredictedLabel",
                    inputColumnName: "PredictedLabel"));

            // Model yalnızca eğitim seti ile eğitilir
            var model = pipeline.Fit(split.TrainSet);

            // Test seti modelden geçirilerek performans ölçülür
            var transformedTest = model.Transform(split.TestSet);

            var metrics = _mlContext.MulticlassClassification.Evaluate(
                transformedTest,
                labelColumnName: "LabelKey",
                predictedLabelColumnName: "PredictedLabel");

            // Tek tek şehir-ürün tahminleri oluşturmak için prediction engine hazırlanır
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<
                SalesMulticlassInput,
                SalesMulticlassPrediction>(model);

            // Her şehir-ürünün son üç aylık bilgisi alınarak gelecek ay girdisi oluşturulur
            var latestInputs = new List<SalesMulticlassInput>();

            foreach (var group in groups)
            {
                var months = group
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                if (months.Count < 3)
                    continue;

                var threeMonthsAgo = months[^3];
                var twoMonthsAgo = months[^2];
                var lastMonth = months[^1];

                var secondExpected = new DateTime(
                    threeMonthsAgo.Year,
                    threeMonthsAgo.Month,
                    1).AddMonths(1);

                var thirdExpected = new DateTime(
                    twoMonthsAgo.Year,
                    twoMonthsAgo.Month,
                    1).AddMonths(1);

                var secondActual = new DateTime(
                    twoMonthsAgo.Year,
                    twoMonthsAgo.Month,
                    1);

                var thirdActual = new DateTime(
                    lastMonth.Year,
                    lastMonth.Month,
                    1);

                // Son üç ay ardışık değilse gelecek ay tahmini oluşturulmaz
                if (secondActual != secondExpected || thirdActual != thirdExpected)
                    continue;

                float average = (
                    threeMonthsAgo.TotalQuantity +
                    twoMonthsAgo.TotalQuantity +
                    lastMonth.TotalQuantity) / 3f;

                if (average <= 0)
                    continue;

                var nextMonth = thirdActual.AddMonths(1);

                latestInputs.Add(new SalesMulticlassInput
                {
                    City = group.Key.City,
                    ProductName = group.Key.ProductName,
                    ThreeMonthsAgo = threeMonthsAgo.TotalQuantity,
                    TwoMonthsAgo = twoMonthsAgo.TotalQuantity,
                    LastMonth = lastMonth.TotalQuantity,
                    ThreeMonthAverage = average,
                    LastMonthGrowthRate = SafeRate(
                        lastMonth.TotalQuantity,
                        twoMonthsAgo.TotalQuantity),
                    TwoMonthGrowthRate = SafeRate(
                        twoMonthsAgo.TotalQuantity,
                        threeMonthsAgo.TotalQuantity),
                    LastMonthVsAverageRate = SafeRate(
                        lastMonth.TotalQuantity,
                        average),
                    TrendSlope = CalculateTrendSlope(
                        threeMonthsAgo.TotalQuantity,
                        twoMonthsAgo.TotalQuantity,
                        lastMonth.TotalQuantity),
                    TargetMonthNumber = nextMonth.Month,
                    TargetMonth = nextMonth.ToString("yyyy-MM"),
                    // Gerçek gelecek ay satışı tahmin aşamasında bilinmez
                    TargetQuantity = 0,
                    TargetPerformanceRatio = 0,
                    Label = string.Empty
                });
            }

            // Gelecek ay için Low/Medium/High tahminleri oluşturulur
            var predictions = new List<MulticlassPredictionViewModel>();

            foreach (var item in latestInputs)
            {
                var prediction = predictionEngine.Predict(item);
                float confidence = GetConfidence(prediction.Score);

                string category = prediction.PredictedLabel switch
                {
                    "High" => "YÜKSEK TALEP",
                    "Medium" => "ORTA TALEP",
                    _ => "DÜŞÜK TALEP"
                };

                predictions.Add(new MulticlassPredictionViewModel
                {
                    City = item.City,
                    ProductName = item.ProductName,
                    LastMonthSales = item.LastMonth,
                    Average3 = item.ThreeMonthAverage,
                    Confidence = confidence,
                    PredictedClass = prediction.PredictedLabel,
                    DemandCategory = category
                });
            }

            // Tahminlerin hangi gelecek ay için yapıldığı belirlenir
            string nextMonthLabel = latestInputs
                .Select(x => x.TargetMonth)
                .OrderByDescending(x => x)
                .FirstOrDefault() ?? string.Empty;

            return new MulticlassClassificationViewModel
            {
                Metrics = metrics,
                Predictions = predictions
                    .OrderBy(x => x.City)
                    .ThenBy(x => x.ProductName)
                    .ToList(),
                P33 = p33,
                P66 = p66,
                LowUpper = p33,
                MediumUpper = p66,
                LowCount = lowCount,
                MediumCount = mediumCount,
                HighCount = highCount,
                SampleCount = trainingData.Count,
                TrainCount = trainCount,
                TestCount = testCount,
                NextMonthLabel = nextMonthLabel
            };
        }

        // İki değer arasındaki büyüme oranını hesaplar
        private static float SafeRate(float current, float previous)
        {
            if (Math.Abs(previous) < 0.0001f)
                return 0f;

            return (current - previous) / previous;
        }

        // Üç aylık satış değerlerinden basit trend eğimi hesaplar
        private static float CalculateTrendSlope(float first, float second, float third)
        {
            return ((second - first) + (third - second)) / 2f;
        }

        // Verilen yüzdelik değerin karşılığını sıralı veri üzerinden hesaplar
        private static double GetPercentile(List<float> values, double percentile)
        {
            if (values.Count == 0)
                return 0;

            if (values.Count == 1)
                return values[0];

            double position = (values.Count - 1) * percentile;
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);

            if (lower == upper)
                return values[lower];

            double fraction = position - lower;

            return values[lower] + (values[upper] - values[lower]) * fraction;
        }

        // ML.NET skorlarını Softmax mantığıyla yaklaşık güven değerine dönüştürür
        private static float GetConfidence(float[] scores)
        {
            if (scores == null || scores.Length == 0)
                return 0;

            double maxScore = scores.Max();

            var exponentials = scores
                .Select(x => Math.Exp(x - maxScore))
                .ToArray();

            double sum = exponentials.Sum();

            if (sum <= 0)
                return 0;

            return (float)Math.Clamp(exponentials.Max() / sum, 0, 1);
        }
    }
}