using AutoMapper;
using ECommerceSalesIntelligence.Entities;
using ECommerceSalesIntelligence.Models;
using ECommerceSalesIntelligence.Models.Classification;

namespace ECommerceSalesIntelligence.Mappings
{
    public class GeneralMapping : Profile
    {
        public GeneralMapping()
        {
            // Forecasting eşlemesi
            CreateMap<SalesRecord, SalesData>()
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.OrderDate))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => (float)src.Quantity));

            // -- YENİ: SalesRecord'dan ClassificationInput'a Map Kuralları --
            CreateMap<SalesRecord, SalesClassificationInput>()
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.TargetMonth, opt => opt.MapFrom(src => src.OrderDate.ToString("yyyy-MM")))
                .ForMember(dest => dest.LastMonth, opt => opt.MapFrom(src => (float)src.Quantity))
                .ForMember(dest => dest.Label, opt => opt.Ignore()); // Label kod içinde hesaplanacak

            // Classification Input -> Prediction eşlemesi
            CreateMap<SalesClassificationInput, SalesClassificationPrediction>();
        }
    }
}