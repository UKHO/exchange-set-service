using AutoMapper;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<ProductCounts, ExchangeSetResponse>()
                .ForMember(dest => dest.ExchangeSetCellCount, opt => opt.MapFrom(src => src.ReturnedProductCount));
            CreateMap<RequestedProductsNotReturned, RequestedProductsNotInExchangeSet>();
        }
    }
}
