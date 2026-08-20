using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Entities;
using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Models.Cluster;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ECommerceSalesIntelligence.Services
{
    public class ClusteringService
    {
        private readonly AppDbContext _context;
        private readonly MLContext _mlContext;

        public ClusteringService(AppDbContext context)
        {
            _context = context;
            _mlContext = new MLContext(seed: 42);
        }

        // Şehirleri satış davranışlarına göre K-Means ile kümeler.
        public async Task<List<ClusterResultViewModel>> TrainAndClusterAsync(int count = 4)
        {
            // Geçerli satış kayıtlarını getir.
            var sales = await _context.SalesRecords
                .AsNoTracking()
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.City) &&
                    !string.IsNullOrWhiteSpace(x.CategoryName))
                .ToListAsync();

            if (!sales.Any())
                throw new InvalidOperationException(
                    "Kümeleme için kullanılabilecek satış verisi bulunamadı.");

            // Satışları şehir bazında grupla.
            var cityGroups = sales
                .GroupBy(x => x.City.Trim())
                .OrderBy(x => x.Key)
                .ToList();

            if (cityGroups.Count < count)
                throw new InvalidOperationException(
                    $"Kümeleme için en az {count} farklı şehir gereklidir.");

            // Her şehir için K-Means özelliklerini oluştur.
            var inputs = cityGroups
                .Select(BuildCityInput)
                .ToList();

            var dataView = _mlContext.Data
                .LoadFromEnumerable(inputs);

            // K-Means'te kullanılacak özellikleri birleştir.
            var pipeline = _mlContext.Transforms
                .Concatenate(
                    "Features",
                    nameof(SalesClusterInput.TotalQuantity),
                    nameof(SalesClusterInput.AverageUnitPrice),
                    nameof(SalesClusterInput.AverageOrderAmount),
                    nameof(SalesClusterInput.TotalRevenue),
                    nameof(SalesClusterInput.AverageDiscountRate),
                    nameof(SalesClusterInput.CampaignRate),
                    nameof(SalesClusterInput.RevenuePerQuantity),
                    nameof(SalesClusterInput.CategoryCount),
                    nameof(SalesClusterInput.TopCategoryRate),
                    nameof(SalesClusterInput.CategoryDiversity))
                .Append(
                    _mlContext.Transforms.NormalizeMinMax(
                        "Features"));

            // Özellikleri normalize et.
            var transformedData = pipeline
                .Fit(dataView)
                .Transform(dataView);

            // K-Means modelini oluştur.
            var kMeans = _mlContext.Clustering.Trainers.KMeans(
                featureColumnName: "Features",
                numberOfClusters: count);

            // Modeli eğit.
            var model = kMeans.Fit(transformedData);

            // Şehirlerin kümesini tahmin et.
            var predictions = model.Transform(transformedData);

            var predictionRows = _mlContext.Data
                .CreateEnumerable<ClusterPrediction>(
                    predictions,
                    reuseRowObject: false)
                .ToList();

            // Ham K-Means küme sonuçlarını şehir indexleriyle eşleştir.
            var rawClusters = new Dictionary<uint, List<int>>();

            for (int i = 0; i < predictionRows.Count; i++)
            {
                uint rawId =
                    predictionRows[i].PredictedClusterId;

                if (!rawClusters.ContainsKey(rawId))
                    rawClusters[rawId] = new List<int>();

                rawClusters[rawId].Add(i);
            }

            // Kümeleri satış hacmi ve ekonomik değerlerine göre sırala.
            var orderedClusters = rawClusters
                .Select(x => new
                {
                    RawId = x.Key,
                    Indexes = x.Value,

                    Quantity = x.Value.Average(
                        i => inputs[i].TotalQuantity),

                    Revenue = x.Value.Average(
                        i => inputs[i].TotalRevenue),

                    OrderAmount = x.Value.Average(
                        i => inputs[i].AverageOrderAmount),

                    Campaign = x.Value.Average(
                        i => inputs[i].CampaignRate)
                })
                .OrderByDescending(x => x.Quantity)
                .ThenByDescending(x => x.Revenue)
                .ThenByDescending(x => x.OrderAmount)
                .ThenByDescending(x => x.Campaign)
                .ToList();

            // Ham K-Means ID'sini UI için 1-2-3-4 şeklinde düzenle.
            var clusterIdMap = new Dictionary<uint, int>();

            for (int i = 0; i < orderedClusters.Count; i++)
            {
                clusterIdMap[
                    orderedClusters[i].RawId] = i + 1;
            }

            // UI sonuçlarını oluştur.
            var results = new List<ClusterResultViewModel>();

            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];

                var rawClusterId =
                    predictionRows[i].PredictedClusterId;

                var cityRecords =
                    cityGroups[i].ToList();

                // Şehrin toplam satış miktarı.
                int cityTotalQuantity =
                    cityRecords.Sum(x => x.Quantity);

                // Şehrin kategori dağılımını oluştur.
                var categoryDistribution = cityRecords
                    .GroupBy(x => x.CategoryName.Trim())
                    .Select(g => new CategoryDistributionSummaryViewModel
                    {
                        CategoryName = g.Key,

                        Quantity = g.Sum(x => x.Quantity),

                        Revenue = (float)g.Sum(
                            x => (double)x.TotalAmount),

                        Percentage = cityTotalQuantity > 0
                            ? (float)g.Sum(x => x.Quantity)
                              / cityTotalQuantity
                              * 100f
                            : 0f
                    })
                    .OrderByDescending(x => x.Quantity)
                    .ToList();

                // En baskın kategoriyi bul.
                var topCategory =
                    categoryDistribution.FirstOrDefault();

                // Gerçek şehir sonucunu oluştur.
                results.Add(new ClusterResultViewModel
                {
                    // Düzenlenmiş küme numarası.
                    ClusterId =
                        (uint)clusterIdMap[rawClusterId],

                    // Şehir.
                    City = input.City,

                    // Gerçek satış miktarı.
                    TotalQuantity =
                        cityTotalQuantity,

                    // Gerçek ortalama birim fiyat.
                    AverageUnitPrice =
                        cityRecords.Count > 0
                            ? (float)cityRecords.Average(
                                x => (double)x.UnitPrice)
                            : 0f,

                    // Toplam satış tutarı.
                    TotalSalesAmount =
                        (float)cityRecords.Sum(
                            x => (double)x.TotalAmount),

                    // Ortalama satış tutarı.
                    AverageOrderAmount =
                        cityRecords.Count > 0
                            ? (float)cityRecords.Average(
                                x => (double)x.TotalAmount)
                            : 0f,

                    // Toplam şehir cirosu.
                    TotalRevenue =
                        (float)cityRecords.Sum(
                            x => (double)x.TotalAmount),

                    // Kampanya oranı.
                    CampaignRate =
                        cityRecords.Count > 0
                            ? (float)cityRecords.Count(
                                x => x.IsCampaign)
                              / cityRecords.Count
                            : 0f,

                    // Kategori sayısı.
                    CategoryCount =
                        categoryDistribution.Count,

                    // En baskın kategori.
                    TopCategory =
                        topCategory?.CategoryName ?? "-",

                    // Baskın kategori satış oranı.
                    TopCategoryRate =
                        topCategory != null &&
                        cityTotalQuantity > 0
                            ? (float)topCategory.Quantity
                              / cityTotalQuantity
                            : 0f,

                    // Kategori çeşitliliği.
                    CategoryDiversity =
                        input.CategoryDiversity,

                    // Kategori detayları.
                    CategoryDistribution =
                        categoryDistribution
                });
            }

            return results;
        }

        // Şehir için K-Means özelliklerini hesaplar.
        private SalesClusterInput BuildCityInput(
            IGrouping<string, SalesRecord> city)
        {
            var records = city.ToList();

            // Toplam satış miktarı.
            int totalQuantity =
                records.Sum(x => x.Quantity);

            // Toplam ciro.
            double totalRevenue =
                records.Sum(x => (double)x.TotalAmount);

            // Ortalama birim fiyat.
            double averageUnitPrice =
                records.Count > 0
                    ? records.Average(
                        x => (double)x.UnitPrice)
                    : 0d;

            // Ortalama satış tutarı.
            double averageOrderAmount =
                records.Count > 0
                    ? records.Average(
                        x => (double)x.TotalAmount)
                    : 0d;

            // Ortalama indirim oranı.
            float averageDiscountRate =
                records.Count > 0
                    ? (float)records.Average(
                        x => (double)x.DiscountRate)
                    : 0f;

            // Kampanyalı satış oranı.
            float campaignRate =
                records.Count > 0
                    ? (float)records.Count(
                        x => x.IsCampaign)
                      / records.Count
                    : 0f;

            // Satış başına üretilen ciro.
            float revenuePerQuantity =
                totalQuantity > 0
                    ? (float)(totalRevenue / totalQuantity)
                    : 0f;

            // Kategorileri satış miktarına göre grupla.
            var categoryGroups = records
                .GroupBy(x => x.CategoryName.Trim())
                .Select(g => new
                {
                    Category = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ToList();

            // Farklı kategori sayısı.
            float categoryCount =
                categoryGroups.Count;

            // En çok satan kategorinin miktarı.
            int topCategoryQuantity =
                categoryGroups.Count > 0
                    ? categoryGroups[0].Quantity
                    : 0;

            // En baskın kategorinin satış oranı.
            float topCategoryRate =
                totalQuantity > 0
                    ? (float)topCategoryQuantity
                      / totalQuantity
                    : 0f;

            // Kategori çeşitliliğini hesapla.
            double categoryDiversity =
                CalculateEntropy(
                    categoryGroups.Select(
                        x => x.Quantity),
                    totalQuantity);

            return new SalesClusterInput
            {
                City = city.Key,

                // Log dönüşümü ile satış hacmini dengeler.
                TotalQuantity =
                    (float)Math.Log(
                        1d + Math.Max(
                            0,
                            totalQuantity)),

                // Log dönüşümü ile fiyatı dengeler.
                AverageUnitPrice =
                    (float)Math.Log(
                        1d + Math.Max(
                            0d,
                            averageUnitPrice)),

                // Log dönüşümü ile sepet değerini dengeler.
                AverageOrderAmount =
                    (float)Math.Log(
                        1d + Math.Max(
                            0d,
                            averageOrderAmount)),

                // Log dönüşümü ile ciroyu dengeler.
                TotalRevenue =
                    (float)Math.Log(
                        1d + Math.Max(
                            0d,
                            totalRevenue)),

                // İndirim oranı.
                AverageDiscountRate =
                    averageDiscountRate,

                // Kampanya oranı.
                CampaignRate =
                    campaignRate,

                // Satış başına ciro.
                RevenuePerQuantity =
                    (float)Math.Log(
                        1d + Math.Max(
                            0f,
                            revenuePerQuantity)),

                // Kategori sayısı.
                CategoryCount =
                    categoryCount,

                // Baskın kategori oranı.
                TopCategoryRate =
                    topCategoryRate,

                // Kategori çeşitliliği.
                CategoryDiversity =
                    (float)categoryDiversity
            };
        }

        // Kategori çeşitliliğini Shannon Entropy ile hesaplar.
        private static double CalculateEntropy(
            IEnumerable<int> quantities,
            double totalQuantity)
        {
            if (totalQuantity <= 0)
                return 0d;

            double entropy = 0d;

            foreach (int quantity in quantities)
            {
                double probability =
                    (double)quantity / totalQuantity;

                if (probability > 0d)
                {
                    entropy -=
                        probability *
                        Math.Log(probability);
                }
            }

            return entropy;
        }

        // ML.NET tahmin sonucunu tutar.
        private class ClusterPrediction
        {
            [ColumnName("PredictedLabel")]
            public uint PredictedClusterId { get; set; }

            public float[] Score { get; set; } =
                Array.Empty<float>();
        }
    }
}