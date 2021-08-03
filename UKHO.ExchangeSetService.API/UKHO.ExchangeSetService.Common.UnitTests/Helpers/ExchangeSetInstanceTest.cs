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

        [SetUp]
        public void Setup()
        {
            smallExchangeSetInstance = new SmallExchangeSetInstance();
            mediumExchangeSetInstance = new MediumExchangeSetInstance();
            largeExchangeSetInstance = new LargeExchangeSetInstance();
        }

        [Test]
        public void WhenSmallExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int maxInstanceCount = 4;
            for (int i = 0; i < maxInstanceCount; i++)
            {
                var response = smallExchangeSetInstance.GetInstanceNumber(maxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response);
                Assert.IsNotNull(response);
                Assert.AreEqual(smallExchangeSetInstance.GetCurrentInstanceNumber(), response);
                if (smallExchangeSetInstance.GetCurrentInstanceNumber() != maxInstanceCount)
                {
                    Assert.AreNotEqual(maxInstanceCount, response);
                }
                else
                {
                    Assert.AreEqual(maxInstanceCount, response);
                }
            }
        }

        [Test]
        public void WhenMediumExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int maxInstanceCount = 3;
            for (int i = 0; i < maxInstanceCount; i++)
            {
                var response = mediumExchangeSetInstance.GetInstanceNumber(maxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response);
                Assert.IsNotNull(response);
                Assert.AreEqual(mediumExchangeSetInstance.GetCurrentInstanceNumber(), response);
                if (mediumExchangeSetInstance.GetCurrentInstanceNumber() != maxInstanceCount)
                {
                    Assert.AreNotEqual(maxInstanceCount, response);
                }
                else
                {
                    Assert.AreEqual(maxInstanceCount, response);
                }
            }
        }

        [Test]
        public void WhenLargeExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int maxInstanceCount = 2;
            var response = largeExchangeSetInstance.GetInstanceNumber(maxInstanceCount);
            Assert.IsInstanceOf(typeof(int), response);
            Assert.IsNotNull(response);
            Assert.AreEqual(largeExchangeSetInstance.GetCurrentInstanceNumber(), response);
            if (largeExchangeSetInstance.GetCurrentInstanceNumber() != maxInstanceCount)
            {
                Assert.AreNotEqual(maxInstanceCount, response);
            }
            else
            {
                Assert.AreEqual(maxInstanceCount, response);
            }
        }

        [Test]
        public void WhenAllExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int maxInstanceCount = 2;
            for (int i = 0; i < maxInstanceCount; i++)
            {
                var response1 = largeExchangeSetInstance.GetInstanceNumber(maxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response1);
                Assert.IsNotNull(response1);
                Assert.AreEqual(largeExchangeSetInstance.GetCurrentInstanceNumber(), response1);
                if (largeExchangeSetInstance.GetCurrentInstanceNumber() != maxInstanceCount)
                {
                    Assert.AreNotEqual(maxInstanceCount, response1);
                }
                else
                {
                    Assert.AreEqual(maxInstanceCount, response1);
                }

                var response2 = mediumExchangeSetInstance.GetInstanceNumber(maxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response2);
                Assert.IsNotNull(response2);
                Assert.AreEqual(mediumExchangeSetInstance.GetCurrentInstanceNumber(), response2);
                if (mediumExchangeSetInstance.GetCurrentInstanceNumber() != maxInstanceCount)
                {
                    Assert.AreNotEqual(maxInstanceCount, response2);
                }
                else
                {
                    Assert.AreEqual(maxInstanceCount, response2);
                }

                var response3 = smallExchangeSetInstance.GetInstanceNumber(maxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response3);
                Assert.IsNotNull(response3);
                Assert.AreEqual(smallExchangeSetInstance.GetCurrentInstanceNumber(), response3);
                if (smallExchangeSetInstance.GetCurrentInstanceNumber() != maxInstanceCount)
                {
                    Assert.AreNotEqual(maxInstanceCount, response3);
                }
                else
                {
                    Assert.AreEqual(maxInstanceCount, response3);
                }
            }
        }
    }
}
