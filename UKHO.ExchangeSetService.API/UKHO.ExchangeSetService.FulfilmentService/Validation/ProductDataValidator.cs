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
            Int16 maxValue = 100,substringValue = 4;
            RuleFor(p => p)
              .Must(pi => pi != null)
              .WithErrorCode(HttpStatusCode.BadRequest.ToString())
              .WithMessage("products cannot be null or empty.");

            RuleForEach(p => p).ChildRules(orders =>
            {
                orders.RuleForEach(x => x.Bundle).ChildRules(orders =>
                {
                    orders.RuleFor(x => x.BundleType)
                      .Must(p => p.Equals("DVD"))
                      .When(p => string.IsNullOrWhiteSpace(p.BundleType))
                      .WithMessage("BundleType value cannot not be empty and must be DVD.")
                      .WithErrorCode(HttpStatusCode.InternalServerError.ToString());

                    orders.RuleFor(x => x.Location)
                      .Must(p => !string.IsNullOrWhiteSpace(p))
                      .WithMessage("Location cannot be null or empty")
                      .Must(p => (p.StartsWith("M1;B") || p.StartsWith("M2;B")) && Convert.ToInt16(p[^(p.Length-substringValue)..]) > 0 && Convert.ToInt16(p[^(p.Length-substringValue)..]) < maxValue)
                      .WithMessage(x => $" Location must starts with M1 or M2 and Base must be in between B1 - B99; Product :  {x.BundleType}, Location : {x.Location}")
                      .WithErrorCode(HttpStatusCode.InternalServerError.ToString());
                });
            });
        }

        Task<ValidationResult> IProductDataValidator.Validate(List<Products> salesCatalogueProductResponse)
        {
            return ValidateAsync(salesCatalogueProductResponse);
        }
    }
}