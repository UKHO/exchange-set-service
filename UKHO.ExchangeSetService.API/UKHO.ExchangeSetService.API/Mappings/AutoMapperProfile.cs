using AutoMapper;
using System.Diagnostics.CodeAnalysis;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.Mappings
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it used in startup
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
