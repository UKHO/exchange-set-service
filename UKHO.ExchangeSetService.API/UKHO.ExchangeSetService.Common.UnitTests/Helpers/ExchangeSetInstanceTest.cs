using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class ExchangeSetInstanceTest
    {
        private ISmallExchangeSetInstance smallExchangeSetInstance;
        private IMediumExchangeSetInstance mediumExchangeSetInstance;
        private ILargeExchangeSetInstance largeExchangeSetInstance;
        public int maxInstanceCount = 0;
        [SetUp]
        public void Setup()
        {
            smallExchangeSetInstance = new ExchangeSetInstance();
            mediumExchangeSetInstance = new ExchangeSetInstance();
            largeExchangeSetInstance = new ExchangeSetInstance();
        }

        [Test]
        public void WhenSmallExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            var response = smallExchangeSetInstance.GetInstanceCount(maxInstanceCount);
            Assert.IsInstanceOf(typeof(int), response);
            Assert.AreEqual(1, response);
            Assert.IsNotNull(response);
        }

        [Test]
        public void WhenMediumExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            var response = mediumExchangeSetInstance.GetInstanceCount(maxInstanceCount);
            Assert.IsInstanceOf(typeof(int), response);
            Assert.AreEqual(1, response);
            Assert.IsNotNull(response);
        }

        [Test]
        public void WhenLargeExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            var response = largeExchangeSetInstance.GetInstanceCount(maxInstanceCount);
            Assert.IsInstanceOf(typeof(int), response);
            Assert.AreEqual(1, response);
            Assert.IsNotNull(response);
        }

        [Test]
        public void WhenSmallExchangeSetInstanceCurrentInstance_ThenReturnsNotNullInResponse()
        {
            var response = smallExchangeSetInstance.GetCurrentInstaceCount();
            Assert.IsInstanceOf(typeof(int), response);
            Assert.AreEqual(0, response);
            Assert.IsNotNull(response);
        }

        [Test]
        public void WhenMediumExchangeSetInstanceCurrentInstance_ThenReturnsNotNullInResponse()
        {
            var response = mediumExchangeSetInstance.GetCurrentInstaceCount();
            Assert.IsInstanceOf(typeof(int), response);
            Assert.AreEqual(0, response);
            Assert.IsNotNull(response);
        }

        [Test]
        public void WhenLargeExchangeSetInstanceCurrentInstance_ThenReturnsNotNullInResponse()
        {
            var response = largeExchangeSetInstance.GetCurrentInstaceCount();
            Assert.IsInstanceOf(typeof(int), response);
            Assert.AreEqual(0, response);
            Assert.IsNotNull(response);
        }
    }
}
