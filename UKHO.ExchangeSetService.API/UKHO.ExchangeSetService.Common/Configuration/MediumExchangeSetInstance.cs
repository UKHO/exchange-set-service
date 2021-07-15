using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class MediumExchangeSetInstance : IMediumExchangeSetInstance
    {
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig;
        private Queue<int> queue;

        public MediumExchangeSetInstance(IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig)
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
            for (int i = 1; i <= essFulfilmentStorageconfig.Value.MediumExchangeSetInstance; i++)
            {
                queue.Enqueue(i);
            }
        }
    }
}
