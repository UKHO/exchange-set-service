using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Extensions
{
    public static class FluentValidationResultExtensions
    {
        public static bool HasBadRequestErrors(this ValidationResult validationResult, out List<Error> badRequestErrors)
        {
            badRequestErrors = new List<Error>();
            return HasErrors(validationResult, HttpStatusCode.BadRequest, out badRequestErrors);
        }

        private static bool HasErrors(this ValidationResult validationResult, HttpStatusCode errorCode, out List<Error> errors)
        {
            errors = new List<Error>();
            if (validationResult.Errors.Any(e => e.ErrorCode.Equals(errorCode.ToString())))
            {
                errors = validationResult.Errors.Where(e => e.ErrorCode.Equals(errorCode.ToString()))
                                    .Select(f => new Error { Source = f.PropertyName, Description = f.ToString() })
                                    .ToList();
                return true;
            }
            return false;
        }
    }
}
