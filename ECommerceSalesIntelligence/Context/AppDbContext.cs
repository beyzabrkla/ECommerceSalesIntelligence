using ECommerceSalesIntelligence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSalesIntelligence.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<SalesRecord> SalesRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1 milyonluk veride performans için indexler
            // Indexi sorgu performansını artırmak için kullanıyoruz.
            //Büyük veri setlerinde, sık kullanılan sütunlar üzerinde indeksler oluşturmak, sorguların daha hızlı çalışmasını sağlar.
            modelBuilder.Entity<SalesRecord>().HasIndex(s => s.OrderDate);
            modelBuilder.Entity<SalesRecord>().HasIndex(s => s.City);
            modelBuilder.Entity<SalesRecord>().HasIndex(s => s.CategoryName);
        }
    }
}
