using System.IO;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IHashHelper
    {
        byte[] CalculateMD5(byte[] requestBytes);
        byte[] CalculateMD5(Stream requestStream);
    }
}
