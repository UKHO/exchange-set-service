using System.IO;
using System.Security.Cryptography;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class HashHelper : IHashHelper
    {
        public byte[] CalculateMD5(byte[] requestBytes)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(requestBytes);

            return hash;
        }

        public byte[] CalculateMD5(Stream requestStream)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(requestStream);

            return hash;
        }
    }
}
