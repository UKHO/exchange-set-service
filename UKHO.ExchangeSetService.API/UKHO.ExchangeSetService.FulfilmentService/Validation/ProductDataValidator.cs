using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
namespace UKHO.ExchangeSetService.FulfilmentService.Validation
{
    public interface IProductDataValidator
    {
        Task<ValidationResult> Validate(List<Products> salesCatalogueProductResponse);
    }
    public class ProductDataValidator : AbstractValidator<List<Products>>, IProductDataValidator
    {
        public ProductDataValidator()
        {
            RuleFor(p =>p)
              .Must(pi => pi != null)
              .WithMessage("products cannot be null or empty.");
            
            RuleForEach(p => p).ChildRules(orders =>
            {
                orders.RuleForEach(x => x.Bundle).ChildRules(orders =>
                {
                    orders.RuleFor(x => x.BundleType)
                      .Must(p => p.StartsWith("DVD"))
                      .When(p => string.IsNullOrWhiteSpace(p.BundleType))
                      .WithMessage("BundleType cannot be null or empty.");

                    orders.RuleFor(x => x.Location)
                      .Must(p => p.StartsWith("M1") || p.StartsWith("M2"))
                      .NotEqual("M0")
                      .When(p => string.IsNullOrWhiteSpace(p.Location))
                      .WithMessage("Location cannot be null or empty");
                });
            });
        }
        Task<ValidationResult> IProductDataValidator.Validate(List<Products> salesCatalogueProductResponse)
        {
            return ValidateAsync(salesCatalogueProductResponse);
        }
    }
}