using AutoMapper;
using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace ECommerceSalesIntelligence.Services
{
    public class ClusteringService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly MLContext _mlContext;

        public ClusteringService(AppDbContext context, IMapper mapper, MLContext mlContext)
        {
            _context = context;
            _mapper = mapper;
            _mlContext = mlContext;
        }

        public List<ClusterResultViewModel> TrainAndCluster(int clusterCount = 4)
        {
            var rawData = _context.SalesRecords
                .AsNoTracking()
                .GroupBy(s => s.City)
                .Select(g => new SalesClusterInput
                {
                    City = g.Key,
                    UnitPrice = (float)g.Average(s => s.UnitPrice),
                    Quantity = (float)g.Average(s => s.Quantity),
                    TotalAmount = (float)g.Sum(s => s.TotalAmount)
                })
                .ToList();

            if (rawData == null || rawData.Count == 0) return new List<ClusterResultViewModel>();

            IDataView dataView = _mlContext.Data.LoadFromEnumerable(rawData);

            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(SalesClusterInput.UnitPrice),
                    nameof(SalesClusterInput.Quantity),
                    nameof(SalesClusterInput.TotalAmount))
                .Append(_mlContext.Transforms.NormalizeMinMax("NormalizedFeatures", "Features"))
                .Append(_mlContext.Clustering.Trainers.KMeans(
                    featureColumnName: "NormalizedFeatures",
                    numberOfClusters: clusterCount));

            var model = pipeline.Fit(dataView);
            var transformedData = model.Transform(dataView);

            var predictions = _mlContext.Data.CreateEnumerable<SalesClusterPrediction>(transformedData, reuseRowObject: false).ToList();

            var results = new List<ClusterResultViewModel>();
            for (int i = 0; i < rawData.Count; i++)
            {
                results.Add(new ClusterResultViewModel
                {
                    ClusterId = predictions[i].PredictedClusterId,
                    City = rawData[i].City,
                    UnitPrice = rawData[i].UnitPrice,
                    Quantity = rawData[i].Quantity,
                    TotalAmount = rawData[i].TotalAmount
                });
            }
            return results;
        }
    }
}