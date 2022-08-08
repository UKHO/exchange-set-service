using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Net;
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
            Int16 maxValue = 100;
            RuleFor(p => p)
              .Must(pi => pi != null)
              .WithErrorCode(HttpStatusCode.BadRequest.ToString())
              .WithMessage("products cannot be null or empty.");

            RuleForEach(p => p).ChildRules(orders =>
            {
                orders.RuleForEach(x => x.Bundle).ChildRules(orders =>
                {
                    orders.RuleFor(x => x.BundleType)
                      .Must(p => p.StartsWith("DVD"))
                      .When(p => string.IsNullOrWhiteSpace(p.BundleType))
                      .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                      .WithMessage("BundleType cannot be null or empty.");

                    orders.RuleFor(x => x.Location)
                      .Must(p => !string.IsNullOrWhiteSpace(p))
                      .WithMessage("Location cannot be null or empty")
                      .Must(p => (p.Contains("M1;B") || p.Contains("M2;B")) && Convert.ToInt16(p.Replace("M1;B", "").Replace("M2;B", "")) > 0 && Convert.ToInt16(p.Replace("M1;B", "").Replace("M2;B", "")) < maxValue)
                      .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                      .WithMessage(x => $" Location must starts with M1/M2 and Base must be in between 1 - 99; Product :  {x.BundleType}, Location : {x.Location}");
                });
            });
        }

        Task<ValidationResult> IProductDataValidator.Validate(List<Products> salesCatalogueProductResponse)
        {
            return ValidateAsync(salesCatalogueProductResponse);
        }
    }
}