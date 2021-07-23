using System;
using System.IO;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class CancellationProductFileContentHelper
    {
        public static void CheckCatalogueFileContent(string inputfile, CancellationResponseModel scsResponse)
        {
            string[] fileContent = File.ReadAllLines(inputfile);

            string contentFifthLine = fileContent[4];
            Console.WriteLine(contentFifthLine);
        }

    }
}
