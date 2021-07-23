using NUnit.Framework;
using System.IO;


namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class CancellationFileHelper
    {
        public static void CheckCatalogueFileContent(string inputFile, int editionNumber, int updateNumber)
        {
            string catalogueFileContent = File.ReadAllText(inputFile);

            Assert.True(catalogueFileContent.Contains($"VERSION=1.0,EDTN={editionNumber},UPDN={updateNumber}"));

        }

        public static void CheckProductFileContent(string inputFile, string productName, int editionNumber)
        {
            string[] eachLineContent = null;
            string[] productFileContent = File.ReadAllLines(inputFile);

            for (int i = 4; i < productFileContent.Length; i++)
            {
                eachLineContent = productFileContent[i].Split(',');

                if (eachLineContent[0].Contains(productName))
                {
                    Assert.AreEqual(eachLineContent[2], editionNumber.ToString(), $"Product.TXT contains edition number for cancel product{eachLineContent[2]}, Instead of expected {editionNumber}");
                }
            }

        }

        public static void DeleteDirectory(string fileName)
        {
            string path = Path.GetTempPath();

            if (Directory.Exists(path))
            {
                string folder = Path.GetFileName(Path.Combine(path, fileName));
                if (folder.Contains(".zip"))
                {
                    folder = folder.Replace(".zip", "");
                }

                //Delete V01XO1 Directory and sub directories from temp Directory
                Directory.Delete(Path.Combine(path, folder), true);

                //Delete V01X01.zip file from temp Directory
                if (File.Exists(Path.Combine(path, fileName)))
                {
                    File.Delete(Path.Combine(path, fileName));
                }


            }

        }
    }

    
}
