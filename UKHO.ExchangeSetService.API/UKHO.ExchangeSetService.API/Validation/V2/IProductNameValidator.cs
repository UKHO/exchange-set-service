using System.Threading.Tasks;
using FluentValidation.Results;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Validation.V2
{
    public interface IProductNameValidator
    {
        Task<ValidationResult> Validate(ProductNameRequest productNameRequest);
    }
}
