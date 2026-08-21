using ECommerceSalesIntelligence.Context;
using ECommerceSalesIntelligence.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSalesIntelligence.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var query = _context.SalesRecords.AsNoTracking();

            var model = new DashboardViewModel();

            model.TotalRevenue = await query
                .Select(x => (decimal?)x.TotalAmount)
                .SumAsync() ?? 0;

            model.TotalQuantity = await query
                .Select(x => (long?)x.Quantity)
                .SumAsync() ?? 0;

            model.TotalSalesRecords = await query
                .LongCountAsync();

            model.AverageSaleAmount = await query
                .Select(x => (decimal?)x.TotalAmount)
                .AverageAsync() ?? 0;

            model.AverageUnitPrice = await query
                .Select(x => (decimal?)x.UnitPrice)
                .AverageAsync() ?? 0;

            var campaignCount = await query
                .LongCountAsync(x => x.IsCampaign);

            model.CampaignRate =
                model.TotalSalesRecords > 0
                    ? (double)campaignCount / model.TotalSalesRecords
                    : 0;

            model.StartDate = await query
                .Select(x => (DateTime?)x.OrderDate)
                .MinAsync();

            model.EndDate = await query
                .Select(x => (DateTime?)x.OrderDate)
                .MaxAsync();

            model.DailySales = await query
                .GroupBy(x => x.OrderDate.Date)
                .OrderBy(x => x.Key)
                .Select(g => new DailySalesItem
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    Quantity = g.Sum(x => (long)x.Quantity)
                })
                .ToListAsync();

            var categoryData = await query
                .GroupBy(x => x.CategoryName)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    Quantity = g.Sum(x => (long)x.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            model.CategorySales = categoryData
                .Select(x => new CategorySalesItem
                {
                    CategoryName = string.IsNullOrWhiteSpace(x.CategoryName)
                        ? "Bilinmeyen"
                        : x.CategoryName,

                    Revenue = x.Revenue,

                    Quantity = x.Quantity,

                    RevenuePercentage = model.TotalRevenue > 0
                        ? (double)(x.Revenue / model.TotalRevenue) * 100
                        : 0
                })
                .ToList();

            model.CitySales = await query
                .GroupBy(x => x.City)
                .Select(g => new CitySalesItem
                {
                    City = string.IsNullOrWhiteSpace(g.Key)
                        ? "Bilinmeyen"
                        : g.Key,

                    Revenue = g.Sum(x => x.TotalAmount),

                    Quantity = g.Sum(x => (long)x.Quantity),

                    AverageSaleAmount = g.Average(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            var paymentData = await query
                .GroupBy(x => x.PaymentMethod)
                .Select(g => new
                {
                    PaymentMethod = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    Quantity = g.Sum(x => (long)x.Quantity),
                    Count = g.LongCount()
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            model.PaymentMethods = paymentData
                .Select(x => new PaymentMethodSalesItem
                {
                    PaymentMethod = string.IsNullOrWhiteSpace(x.PaymentMethod)
                        ? "Bilinmeyen"
                        : x.PaymentMethod,

                    Revenue = x.Revenue,

                    Quantity = x.Quantity,

                    Percentage = model.TotalRevenue > 0
                        ? (double)(x.Revenue / model.TotalRevenue) * 100
                        : 0
                })
                .ToList();

            var campaignData = await query
                .GroupBy(x => x.IsCampaign)
                .Select(g => new
                {
                    IsCampaign = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    Quantity = g.Sum(x => (long)x.Quantity),
                    RecordCount = g.LongCount()
                })
                .ToListAsync();

            var campaign = campaignData
                .FirstOrDefault(x => x.IsCampaign);

            var nonCampaign = campaignData
                .FirstOrDefault(x => !x.IsCampaign);

            model.CampaignSummary = new CampaignSalesSummary
            {
                CampaignRevenue = campaign?.Revenue ?? 0,

                NonCampaignRevenue = nonCampaign?.Revenue ?? 0,

                CampaignQuantity = campaign?.Quantity ?? 0,

                NonCampaignQuantity = nonCampaign?.Quantity ?? 0,

                CampaignRecordCount = campaign?.RecordCount ?? 0,

                NonCampaignRecordCount = nonCampaign?.RecordCount ?? 0,

                CampaignRate = model.TotalSalesRecords > 0
                    ? (double)(campaign?.RecordCount ?? 0)
                        / model.TotalSalesRecords * 100
                    : 0,

                NonCampaignRate = model.TotalSalesRecords > 0
                    ? (double)(nonCampaign?.RecordCount ?? 0)
                        / model.TotalSalesRecords * 100
                    : 0
            };

            model.TopProducts = await query
                .GroupBy(x => x.ProductName)
                .Select(g => new ProductSalesItem
                {
                    ProductName = string.IsNullOrWhiteSpace(g.Key)
                        ? "Bilinmeyen"
                        : g.Key,

                    Quantity = g.Sum(x => (long)x.Quantity),

                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(10)
                .ToListAsync();

            return model;
        }
    }
}