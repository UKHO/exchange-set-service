using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class LargeExchangeSetInstance : ILargeExchangeSetInstance
    {
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig;
        private Queue<int> queue;

        public LargeExchangeSetInstance(IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig)
        {
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
            SetQueue();
        }
        public int GetCurrentInstaceCount() => queue.Peek();

        public int GetInstanceCount()
        {
            var newInstanceCount = queue.Dequeue();
            queue.Enqueue(newInstanceCount);
            return newInstanceCount;
        }

        public void ResetInstanceCount() => SetQueue();

        private void SetQueue()
        {
            queue = new Queue<int>();
            //// TO DO: use LargeExchangeSetInstance
            for (int i = 1; i <= essFulfilmentStorageconfig.Value.MediumExchangeSetInstance; i++)
            {
                queue.Enqueue(i);
            }
        }
    }
}
