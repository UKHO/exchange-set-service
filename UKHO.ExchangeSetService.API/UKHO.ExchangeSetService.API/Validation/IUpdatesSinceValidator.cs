﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using FluentValidation.Results;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Validation
{
    public interface IUpdatesSinceValidator
    {
        Task<ValidationResult> Validate(UpdatesSinceRequest updatesSinceRequest);
    }
}