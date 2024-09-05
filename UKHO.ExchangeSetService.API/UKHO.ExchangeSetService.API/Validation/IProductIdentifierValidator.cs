using System.Threading.Tasks;
using FluentValidation.Results;
using UKHO.ExchangeSetService.Common.Models.Request;

public interface IProductIdentifierValidator
{
    Task<ValidationResult> Validate(ProductIdentifierRequest productIdentifierRequest);
}