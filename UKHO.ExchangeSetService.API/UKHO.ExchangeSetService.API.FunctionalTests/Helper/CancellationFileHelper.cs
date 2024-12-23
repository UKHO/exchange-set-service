using NUnit.Framework;
using System.IO;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class CancellationFileHelper
    {
        public static void CheckCatalogueFileContent(string inputFile, int editionNumber, int updateNumber, string batchId)
        {
            string catalogueFileContent = File.ReadAllText(inputFile);

            Assert.That(catalogueFileContent.Contains($"VERSION=1.0,EDTN={editionNumber},UPDN={updateNumber}"), Is.True,$"batchId {batchId}, Content not found file content : {catalogueFileContent} and search content : 'VERSION=1.0,EDTN={editionNumber},UPDN={updateNumber}'");

        }

        public static void CheckProductFileContent(string inputFile, string productName, int editionNumber)
        {
            if (File.Exists(inputFile))
            {
                string[] eachLineContent = null;
                string[] productFileContent = File.ReadAllLines(inputFile);

                for (int i = 4; i < productFileContent.Length; i++)
                {
                    eachLineContent = productFileContent[i].Split(',');

                    if (eachLineContent[0].Contains(productName))
                    {
                        Assert.That(editionNumber.ToString(), Is.EqualTo(eachLineContent[2]), $"Product.TXT contains edition number for cancel product{eachLineContent[2]}, Instead of expected {editionNumber}");
                    }
                }
            }
            else
            {
                Assert.Fail($"File Doesn't Exists {inputFile}");
            }

        }
    }
}
