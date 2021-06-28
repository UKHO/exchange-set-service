namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class BlocksHelper
    {
        public string GetBlockIds(int blockNum)
        {
            string blockId = $"Block_{blockNum:00000}";
            return blockId;
        }
    }
}
