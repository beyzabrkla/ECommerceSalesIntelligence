using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Entities;
using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Models.Cluster;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace ECommerceSalesIntelligence.Services
{
    public class ClusteringService
    {
        private readonly AppDbContext _context;
        private readonly MLContext _mlContext;

        public ClusteringService(AppDbContext context)
        {
            _context = context;
            _mlContext = new MLContext(seed: 42); // Tekrarlanabilir ML sonuçları için seed
        }

        public async Task<List<ClusterResultViewModel>> TrainAndClusterAsync(int count = 2)
        {
            count = 2;
            var sales = await _context.SalesRecords
                .AsNoTracking() // Sadece okuma yapılacağı için tracking kapatılır
                .Where(x => !string.IsNullOrWhiteSpace(x.City) &&
                            !string.IsNullOrWhiteSpace(x.CategoryName))
                .ToListAsync();

            if (!sales.Any()) throw new InvalidOperationException("Kümeleme için kullanılabilecek satış verisi bulunamadı.");

            // Satışları şehirlere göre grupla
            var cityGroups = sales
                .GroupBy(x => x.City.Trim())
                .OrderBy(x => x.Key)
                .ToList();

            if (cityGroups.Count < count) throw new InvalidOperationException( $"Kümeleme için en az {count} farklı şehir gereklidir.");

            var inputs = cityGroups.Select(BuildCityInput).ToList();

            // Şehir özelliklerini ML.NET veri formatına dönüştür
            var dataView = _mlContext.Data.LoadFromEnumerable(inputs);

            // KMeans için kullanılacak özellikleri birleştir
            var pipeline = _mlContext.Transforms
                .Concatenate(
                    "Features",
                    nameof(SalesClusterInput.AverageDailyQuantity),
                    nameof(SalesClusterInput.AverageOrderAmount),
                    nameof(SalesClusterInput.CampaignRate),
                    nameof(SalesClusterInput.AverageDiscountRate),
                    nameof(SalesClusterInput.CategoryCount),
                    nameof(SalesClusterInput.TopCategoryRate),
                    nameof(SalesClusterInput.CategoryDiversity))

                // Özellikleri 0-1 aralığına getir
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"));

            // Pipelineı eğit ve verileri dönüştür
            var transformedData = pipeline
                .Fit(dataView)
                .Transform(dataView);

            // KMeans kümeleme algoritmasını oluştur
            var kMeans = _mlContext.Clustering.Trainers.KMeans(featureColumnName: "Features",numberOfClusters: count);

            // KMeans modelini eğit
            var model = kMeans.Fit(transformedData);

            // Şehirlerin hangi kümeye ait olduğunu tahmin et
            var predictions = model.Transform(transformedData);

            // Tahmin sonuçlarını C# listesine dönüştür
            var predictionRows = _mlContext.Data.CreateEnumerable<SalesClusterPrediction>(predictions,reuseRowObject: false).ToList();

            // Ham küme ID'lerini ve şehir indekslerini tut
            var rawClusters = new Dictionary<uint, List<int>>();

            // Her şehrin hangi kümeye atandığını işle
            for (int i = 0; i < predictionRows.Count; i++)
            {
                uint rawId = predictionRows[i].PredictedClusterId;

                // Küme daha önce oluşturulmadıysa oluştur
                if (!rawClusters.ContainsKey(rawId))
                    rawClusters[rawId] = new List<int>();

                // Şehrin indeksini kümeye ekle
                rawClusters[rawId].Add(i);
            }

            // Kümeleri belirli kriterlere göre sırala
            var orderedClusters = rawClusters
                .Select(x => new
                {
                    RawId = x.Key,
                    Indexes = x.Value,

                    // Kümenin ortalama kampanya oranı
                    CampaignRate = x.Value.Average(
                        i => inputs[i].CampaignRate),

                    // Kümenin ortalama günlük satış adedi
                    AverageDailyQuantity = x.Value.Average(
                        i => inputs[i].AverageDailyQuantity),

                    // Kümenin ortalama sipariş tutarı
                    AverageOrderAmount = x.Value.Average(
                        i => inputs[i].AverageOrderAmount)
                })

                // Önce kampanya oranına göre sırala
                .OrderBy(x => x.CampaignRate)

                // Sonra satış miktarına göre büyükten küçüğe sırala
                .ThenByDescending(x => x.AverageDailyQuantity)

                // Son olarak sipariş tutarına göre sırala
                .ThenByDescending(x => x.AverageOrderAmount)

                .ToList();

            // ML.NET'in ham küme ID'lerini 1,2 gibi anlaşılır ID'lere dönüştür
            var clusterIdMap = new Dictionary<uint, int>();

            for (int i = 0; i < orderedClusters.Count; i++)
                clusterIdMap[orderedClusters[i].RawId] = i + 1;

            // Sonuçların tutulacağı liste
            var results = new List<ClusterResultViewModel>();

            // Her şehir için sonuç oluştur
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];

                // Şehrin ML tarafından verilen ham küme ID'si
                uint rawClusterId = predictionRows[i].PredictedClusterId;

                // Şehre ait tüm satış kayıtlarını al
                var cityRecords = cityGroups[i].ToList();

                // Şehrin toplam satış miktarını hesapla
                int cityTotalQuantity = cityRecords.Sum(x => x.Quantity);

                // Şehirdeki kategorilerin dağılımını hesapla
                var categoryDistribution = cityRecords
                    .GroupBy(x => x.CategoryName.Trim())
                    .Select(g => new CategoryDistributionSummaryViewModel
                    {
                        CategoryName = g.Key, // Kategori adı

                        Quantity = g.Sum(x => x.Quantity), // Kategori satış adedi

                        Revenue = (float)g.Sum(
                            x => (double)x.TotalAmount), // Kategori cirosu

                        // Kategorinin toplam satış içindeki oranı
                        Percentage = cityTotalQuantity > 0
                            ? (float)g.Sum(x => x.Quantity)
                              / cityTotalQuantity * 100f
                            : 0f
                    })
                    .OrderByDescending(x => x.Quantity) // En çok satılan kategori üstte
                    .ToList();

                var topCategory = categoryDistribution.FirstOrDefault();

                results.Add(new ClusterResultViewModel
                {
                    ClusterId = (uint)clusterIdMap[rawClusterId],
                    City = input.City,
                    TotalQuantity = cityTotalQuantity,

                    AverageUnitPrice = cityRecords.Count > 0? (float)cityRecords.Average( x => (double)x.UnitPrice) : 0f,

                    TotalSalesAmount = (float)cityRecords.Sum(x => (double)x.TotalAmount),

                    AverageOrderAmount = cityRecords.Count > 0 ? (float)cityRecords.Average(x => (double)x.TotalAmount) : 0f,

                    TotalRevenue = (float)cityRecords.Sum(x => (double)x.TotalAmount),

                    CampaignRate = cityRecords.Count > 0? (float)cityRecords.Count(x => x.IsCampaign) / cityRecords.Count: 0f,

                    CategoryCount = categoryDistribution.Count,

                    TopCategory = topCategory?.CategoryName ?? "-",

                    TopCategoryRate = topCategory != null && cityTotalQuantity > 0 ? (float)topCategory.Quantity / cityTotalQuantity : 0f,

                    CategoryDiversity = input.CategoryDiversity,
                    CategoryDistribution = categoryDistribution
                });
            }
            return results;
        }

        private SalesClusterInput BuildCityInput(IGrouping<string, SalesRecord> city)
        {
            var records = city.ToList();
            int totalQuantity = records.Sum(x => x.Quantity);

            int activeDays = records .Select(x => x.OrderDate.Date).Distinct().Count();

            float averageDailyQuantity = activeDays > 0 ? (float)totalQuantity / activeDays : 0f;

            double averageOrderAmount = records.Count > 0 ? records.Average(x => (double)x.TotalAmount) : 0d;

            float averageDiscountRate = records.Count > 0 ? (float)records.Average( x => (double)x.DiscountRate) : 0f;

            float campaignRate = records.Count > 0 ? (float)records.Count(x => x.IsCampaign) / records.Count : 0f;

            // Şehirdeki kategorileri satış miktarına göre grupla
            var categoryGroups = records
                .GroupBy(x => x.CategoryName.Trim())
                .Select(g => new
                {
                    Category = g.Key,
                    Quantity = g.Sum(x => x.Quantity) 
                })
                .OrderByDescending(x => x.Quantity)
                .ToList();

            float categoryCount = categoryGroups.Count;

            int topCategoryQuantity = categoryGroups.Count > 0 ? categoryGroups[0].Quantity : 0;

            float topCategoryRate = totalQuantity > 0  ? (float)topCategoryQuantity / totalQuantity : 0f;

            double categoryDiversity = CalculateEntropy(categoryGroups.Select(x => x.Quantity), totalQuantity);

            // ML.NET için şehir özelliklerini hazırla
            return new SalesClusterInput
            {
                City = city.Key,

                // Büyük değerlerin etkisini azaltmak için log dönüşümü
                AverageDailyQuantity = (float)Math.Log( 1d + Math.Max(0d, averageDailyQuantity)),

                // Sipariş tutarında log dönüşümü
                AverageOrderAmount = (float)Math.Log( 1d + Math.Max(0d, averageOrderAmount)),

                CampaignRate = campaignRate,
                AverageDiscountRate = averageDiscountRate,
                CategoryCount = categoryCount,
                TopCategoryRate = topCategoryRate,
                CategoryDiversity = (float)categoryDiversity
            };
        }

        private static double CalculateEntropy(IEnumerable<int> quantities, double totalQuantity)
        {
            // Toplam satış yoksa entropy hesaplanamaz
            if (totalQuantity <= 0)
                return 0d;

            double entropy = 0d;

            // Her kategorinin satış oranını hesapla
            foreach (int quantity in quantities)
            {
                double probability =
                    (double)quantity / totalQuantity;

                // Olasılık sıfırdan büyükse entropy'ye ekle
                if (probability > 0d)
                    entropy -= probability * Math.Log(probability);
            }
            return entropy;
        }
    }
}