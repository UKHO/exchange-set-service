using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class ExchangeSetInstance : ISmallExchangeSetInstance, IMediumExchangeSetInstance, ILargeExchangeSetInstance
    {
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig;
        private Queue<int> queue;
        private readonly Object _lock = new Object();

        public ExchangeSetInstance(IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig)
        {
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
            SetQueue();
        }
        public int GetCurrentInstaceCount() => queue.Peek();

        public int GetInstanceCount()
        {
            lock (_lock)
            {
                var newInstanceCount = queue.Dequeue();
                queue.Enqueue(newInstanceCount);
                return newInstanceCount;
            }
        }

        public void ResetInstanceCount() => SetQueue();

        private void SetQueue()
        {
            queue = new Queue<int>();
            for (int i = 1; i <= essFulfilmentStorageconfig.Value.SmallExchangeSetInstance; i++)
            {
                queue.Enqueue(i);
            }
        }
    }
}
