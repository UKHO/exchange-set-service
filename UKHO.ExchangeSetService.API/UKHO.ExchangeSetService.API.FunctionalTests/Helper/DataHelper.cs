using System.Collections.Generic;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class DataHelper
    {
        public ProductVersionModel ProductVersionModel { get; set; }
        public UpdatesSinceModel UpdatesSinceModel { get; set; }

        public ProductVersionModel GetProductVersionModelData(string productName, int? editionNumber, int? updateNumber)
        {
            ProductVersionModel = new ProductVersionModel()
            {
                ProductName = productName,
                EditionNumber = editionNumber,
                UpdateNumber = updateNumber
            };

            return ProductVersionModel;
        }

        public List<string> GetProductIdentifierData()
        {
            return new List<string>() { "DE5NOBRK", "DE4NO18Q", "DE416080" };
        }

        public List<string> GetOnlyProductIdentifierData()
        {
            return new List<string>() { "DE260001" };
        }

        public List<string> GetProductIdentifiers()
        {
            return new List<string>() { "DE360010", "DE416040" };
        }

        public List<string> GetReissueProducts()
        {
            return new List<string>() { "JP5BHTR7", "JP5P9F59" };
        }

        public List<string> GetProductIdentifiersForLargeMedia()
        {
            return new List<string>() { "FR570300", "SE6IIFE1", "NO3B2020", "GB20486A", "RU3P0ZM0", "US5CN13M", "CA172005", "DE521900", "NZ300661", "KR676D03" };
        }

        public List<string> GetProductIdentifiersForLargeMediaAndAio()
        {
            return new List<string>() { "FR570300", "SE6IIFE1", "NO3B2020", "GB20486A", "RU3P0ZM0", "US5CN13M", "CA172005", "DE521900", "NZ300661", "KR676D03", "GB800001" };
        }

        public List<string> GetProductIdentifiersForEncAndAio()
        {
            return new List<string>() { "GB800001", "DE260001" };
        }

        public List<string> GetProductIdentifiersForAioOnly()
        {
            return new List<string>() { "GB800001" };
        }

        public List<string> GetProductIdentifiersForLargeMediaAndAioNotPresent()
        {
            return new List<string>() { "FR570300", "SE6IIFE1", "NO3B2020", "GB20486A", "RU3P0ZM0", "US5CN13M", "CA172005", "DE521900", "NZ300661", "KR676D03", "GZ800112" };
        }

        public List<string> GetAioProductIdentifierData()
        {
            return new List<string>() { "DE5NOBRK", "DE4NO18Q", "DE416080", "GB800001" };

        }

        public List<string> GetAioProductIdentifierAndInvalidData()
        {
            return new List<string>() { "DE5NOBRK", "DE4NO18Q", "DE416080", "GB800001", "ABCDEFGH" };
        }

        public List<string> GetDuplicateAioProductIdentifierData()
        {
            return new List<string>() { "DE416080", "GB800001", "GB800001" };
        }

        public List<string> GetAdditionalAioProductIdentifierData()
        {
            return new List<string>() { "DE5NOBRK", "DE4NO18Q", "DE416080", "GZ800112" };
        }

        public List<string> GetProductIdentifiersForInvalidProduct()
        {
            return new List<string>() { "AB1234GH" };
        }

        public List<string> GetProductIdentifiersForInvalidEncAndInValidAio()
        {
            return new List<string>() { "AB1234GH", "GZ800112" };
        }

        public List<string> GetProductIdentifiersForInvalidAioCells()
        {
            return new List<string>() { "GZ800112" };
        }

        public List<string> GetProductIdentifiersForInvalidProductAndValidAio()
        {
            return new List<string>() { "AB1234GH", "GB800001" };
        }

        public List<string> GetProductIdentifiersForInvalidEncAndValidAndInvalidAioCell()
        {
            return new List<string>() { "AB1234GH", "GZ800112", "GB800001", };
        }

        public List<string> GetProductIdentifiersForValidAndInvalidAioCell()
        {
            return new List<string>() { "GB800001", "GZ800112" };
        }

        public List<string> GetProductIdentifiersS57()
        {
            return new List<string>() { "GB602571" };
        }

        public UpdatesSinceModel GetSinceDateTime(string dateTime)
        {
            UpdatesSinceModel = new UpdatesSinceModel()
            {
                SinceDateTime = dateTime
            };
            return UpdatesSinceModel;
        }

        public List<string> GetProductNamesForS100()
        {
            return new List<string>() { "101GB40079ABCDEFG", "102NO32904820801012", "104US00_CHES_TYPE1_20210630_0600", "111US00_ches_dcf8_20190703T00Z" };
        }
    }
}
