using AutoMapper;
using ECommerceSalesIntelligence.Entities;
using ECommerceSalesIntelligence.Models;

namespace ECommerceSalesIntelligence.Mappings
{
    public class GeneralMapping : Profile
    {
        public GeneralMapping()
        {
            // Veritabanından gelen veriyi Forecasting modeline eşleme
            CreateMap<SalesRecord, SalesData>()
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.OrderDate)) // Tarih alanını eşleme
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => (float)src.Quantity)); // Miktar alanını eşleme ve float tipine dönüştürme
        }
    }
}
