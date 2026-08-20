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

        // Cache süresi ve eşik değişikliklerini ayırt etmek için versiyonlu key.
        private const string CacheKey = "BinaryClassificationDashboardCache_v12_650";

        // 650 ve üzeri AŞTI, 650 altı ALTINDA olarak etiketlenir.
        private const float ClassificationThreshold = 650f;

        public BinaryClassificationService(
            AppDbContext context,
            MLContext mlContext,
            IMemoryCache memoryCache)
        {
            _context = context;
            _mlContext = mlContext;
            _memoryCache = memoryCache;
        }

        // Dashboard için eğitim, test ve gelecek ay sınıflandırma sonuçlarını oluşturur.
        public ClassificationDashboardViewModel GetBinaryDashboardData()
        {
            // Cache varsa modeli tekrar eğitmeden mevcut sonucu kullanır.
            if (_memoryCache.TryGetValue(
                CacheKey,
                out ClassificationDashboardViewModel? cachedModel) &&
                cachedModel != null)
            {
                return cachedModel;
            }

            // Gerekli satış alanlarını veritabanından alır.
            var salesRecords = _context.SalesRecords
                .AsNoTracking()
                .Select(x => new
                {
                    x.City,
                    x.ProductName,
                    x.OrderDate,
                    x.Quantity
                })
                .ToList();

            if (!salesRecords.Any())
            {
                return EmptyResult("Veritabanında satış verisi bulunamadı.");
            }

            // Satışları şehir, ürün ve ay bazında toplar.
            var rawMonthlySales = salesRecords
                .GroupBy(x => new
                {
                    City = string.IsNullOrWhiteSpace(x.City)
                        ? "Bilinmeyen Şehir"
                        : x.City.Trim(),
                    ProductName = string.IsNullOrWhiteSpace(x.ProductName)
                        ? "Bilinmeyen Ürün"
                        : x.ProductName.Trim(),
                    Year = x.OrderDate.Year,
                    Month = x.OrderDate.Month
                })
                .Select(g => new MonthlySale
                {
                    City = g.Key.City,
                    ProductName = g.Key.ProductName,
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    TotalQuantity = g.Sum(x => Convert.ToSingle(x.Quantity))
                })
                .OrderBy(x => x.City)
                .ThenBy(x => x.ProductName)
                .ThenBy(x => x.Date)
                .ToList();

            if (!rawMonthlySales.Any())
            {
                return EmptyResult("Aylık satış verisi oluşturulamadı.");
            }

            // En az dört aylık geçmişi olan şehir-ürün gruplarını seçer.
            var productGroups = rawMonthlySales
                .GroupBy(x => new { x.City, x.ProductName })
                .Where(g => g.Count() >= 4)
                .ToList();

            if (!productGroups.Any())
            {
                return EmptyResult(
                    "En az 4 aylık satış geçmişi bulunan şehir-ürün grubu bulunamadı.");
            }

            // Geçmiş satışlardan modelin öğreneceği eğitim kayıtlarını oluşturur.
            var trainingData = new List<SalesClassificationInput>();

            foreach (var group in productGroups)
            {
                var monthlyData = group
                    .OrderBy(x => x.Date)
                    .ToDictionary(x => x.Date, x => x.TotalQuantity);

                var firstDate = monthlyData.Keys.Min();
                var lastDate = monthlyData.Keys.Max();

                int monthDifference =
                    (lastDate.Year - firstDate.Year) * 12 +
                    lastDate.Month - firstDate.Month;

                if (monthDifference < 3)
                {
                    continue;
                }

                var currentDate = firstDate.AddMonths(3);

                while (currentDate <= lastDate)
                {
                    var month1 = currentDate.AddMonths(-3);
                    var month2 = currentDate.AddMonths(-2);
                    var month3 = currentDate.AddMonths(-1);

                    float sales1 = monthlyData.TryGetValue(month1, out var value1) ? value1 : 0f;

                    float sales2 = monthlyData.TryGetValue(month2, out var value2) ? value2 : 0f;

                    float sales3 = monthlyData.TryGetValue(month3, out var value3) ? value3 : 0f;

                    // Hedef ayın gerçek satışı yoksa eğitim kaydı oluşturulmaz.
                    if (!monthlyData.TryGetValue(currentDate, out var targetQuantity))
                    {
                        currentDate = currentDate.AddMonths(1);
                        continue;
                    }

                    float lastThreeMonthsSales = sales1 + sales2 + sales3;
                    float threeMonthAverage = lastThreeMonthsSales / 3f;

                    // Gerçek hedef satış 650 veya üzerindeyse pozitif sınıf oluşturulur.
                    bool label = targetQuantity >= ClassificationThreshold;

                    trainingData.Add(new SalesClassificationInput
                    {
                        City = group.Key.City,
                        ProductName = group.Key.ProductName,
                        LastThreeMonthsSales = lastThreeMonthsSales,
                        LastMonthSales = sales3,
                        ThreeMonthAverage = threeMonthAverage,
                        TargetMonth = currentDate.ToString("yyyy-MM"),
                        TargetQuantity = targetQuantity,
                        Label = label
                    });

                    currentDate = currentDate.AddMonths(1);
                }
            }

            // Eğitim verisindeki iki sınıfın dağılımını hesaplar.
            int positiveCount = trainingData.Count(x => x.Label);
            int negativeCount = trainingData.Count(x => !x.Label);

            if (trainingData.Count < 4)
            {
                return EmptyResult(
                    $"Model eğitimi için yeterli kayıt oluşturulamadı. " +
                    $"Oluşturulan eğitim kaydı: {trainingData.Count}");
            }

            if (positiveCount == 0)
            {
                return EmptyResult(
                    $"Eğitim verilerinde {ClassificationThreshold:N0} ve üzeri satış bulunan kayıt yok. " +
                    $"Toplam: {trainingData.Count}, EVET: {positiveCount}, HAYIR: {negativeCount}");
            }

            if (negativeCount == 0)
            {
                return EmptyResult(
                    $"Eğitim verilerinde {ClassificationThreshold:N0} altı satış bulunan kayıt yok. " +
                    $"Toplam: {trainingData.Count}, EVET: {positiveCount}, HAYIR: {negativeCount}");
            }

            // Eğitim listesini ML.NET'in IDataView formatına çevirir.
            IDataView data = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Veriyi %80 eğitim, %20 test olacak şekilde ayırır.
            var split = _mlContext.Data.TrainTestSplit(
                data,
                testFraction: 0.20,
                seed: 42);

            // Şehir, ürün ve geçmiş satış değerlerini modele özellik olarak verir.
            var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "CityEncoded",
                    inputColumnName: nameof(SalesClassificationInput.City))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "ProductEncoded",
                    inputColumnName: nameof(SalesClassificationInput.ProductName)))
                .Append(_mlContext.Transforms.Concatenate(
                    "Features",
                    "CityEncoded",
                    "ProductEncoded",
                    nameof(SalesClassificationInput.LastThreeMonthsSales),
                    nameof(SalesClassificationInput.LastMonthSales),
                    nameof(SalesClassificationInput.ThreeMonthAverage)))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: "Label",
                    featureColumnName: "Features"));

            // Logistic Regression modelini eğitim verisiyle eğitir.
            ITransformer model = pipeline.Fit(split.TrainSet);

            // Modelin test verisindeki başarısını ölçer.
            IDataView testResult = model.Transform(split.TestSet);

            var metrics = _mlContext.BinaryClassification.Evaluate(
                testResult,
                labelColumnName: "Label");

            // Eğitilmiş modelden tek tek sınıflandırma sonucu üretmek için engine oluşturur.
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<
                SalesClassificationInput,
                SalesClassificationPrediction>(model);

            // Gelecek ay için mevcut son üç aylık satış özelliklerini hazırlar.
            var futureInputs = new List<SalesClassificationInput>();

            foreach (var group in productGroups)
            {
                var monthlyData = group .OrderBy(x => x.Date).ToDictionary(x => x.Date, x => x.TotalQuantity);

                if (monthlyData.Count < 3)
                {
                    continue;
                }

                var lastDate = monthlyData.Keys.Max();

                var month1 = lastDate.AddMonths(-2);
                var month2 = lastDate.AddMonths(-1);
                var month3 = lastDate;

                float sales1 = monthlyData.TryGetValue(month1, out var value1) ? value1 : 0f;

                float sales2 = monthlyData.TryGetValue(month2, out var value2) ? value2 : 0f;

                float sales3 = monthlyData.TryGetValue(month3, out var value3) ? value3 : 0f;

                float lastThreeMonthsSales = sales1 + sales2 + sales3;
                float threeMonthAverage = lastThreeMonthsSales / 3f;

                futureInputs.Add(new SalesClassificationInput
                {
                    City = group.Key.City,
                    ProductName = group.Key.ProductName,
                    LastThreeMonthsSales = lastThreeMonthsSales,
                    LastMonthSales = sales3,
                    ThreeMonthAverage = threeMonthAverage,
                    TargetMonth = lastDate.AddMonths(1).ToString("yyyy-MM"),
                    TargetQuantity = 0,
                    Label = false
                });
            }

            if (!futureInputs.Any())
            {
                return EmptyResult(
                    "Gelecek ay için tahmin oluşturulabilecek şehir-ürün verisi bulunamadı.");
            }

            // Logistic Regression'ın gelecek ay için ürettiği sınıflandırma sonuçlarını toplar.
            var predictions = new List<SalesClassificationPrediction>();

            foreach (var input in futureInputs)
            {
                var prediction = predictionEngine.Predict(input);

                predictions.Add(new SalesClassificationPrediction
                {
                    City = input.City,
                    ProductName = input.ProductName,
                    LastThreeMonthsSales = input.LastThreeMonthsSales,
                    LastMonthSales = input.LastMonthSales,
                    ThreeMonthAverage = input.ThreeMonthAverage,
                    TargetMonth = input.TargetMonth,
                    PredictedLabel = prediction.PredictedLabel,
                    Probability = prediction.Probability,
                    Score = prediction.Score
                });
            }

            // En yüksek pozitif sınıf olasılığından başlayarak sıralar.
            predictions = predictions
                .OrderByDescending(x => x.Probability)
                .ToList();

            // Dashboard'da gösterilecek ana modeli oluşturur.
            var viewModel = new ClassificationDashboardViewModel
            {
                Metrics = metrics,
                Predictions = predictions,
                Threshold = ClassificationThreshold,
                Message =
                    $"Model {trainingData.Count:N0} eğitim kaydı ile eğitildi. " +
                    $"EVET: {positiveCount:N0}, HAYIR: {negativeCount:N0}. " +
                    $"Classification eşiği: {ClassificationThreshold:N0}"
            };

            // Oluşturulan sonucu bir saat boyunca cache'de tutar.
            _memoryCache.Set(
                CacheKey,
                viewModel,
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1)));

            return viewModel;
        }

        // Hata veya veri yetersizliği durumunda boş dashboard modeli döndürür.
        private ClassificationDashboardViewModel EmptyResult(string message)
        {
            return new ClassificationDashboardViewModel
            {
                Metrics = null,
                Predictions = new List<SalesClassificationPrediction>(),
                Threshold = ClassificationThreshold,
                Message = message
            };
        }

        // SQL'den oluşturulan aylık şehir-ürün satış modelidir.
        private class MonthlySale
        {
            public string City { get; set; } = "";
            public string ProductName { get; set; } = "";
            public DateTime Date { get; set; }
            public float TotalQuantity { get; set; }
        }
    }
}