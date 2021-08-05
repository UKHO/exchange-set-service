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
            int smallMaxInstanceCount = 4;
            int response = 0;
            for (int i = 0; i < (smallMaxInstanceCount + 1); i++)
            {
                response = smallExchangeSetInstance.GetInstanceNumber(smallMaxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response);
                Assert.AreEqual(smallExchangeSetInstance.GetCurrentInstanceNumber(), response);
                if (smallExchangeSetInstance.GetCurrentInstanceNumber() != smallMaxInstanceCount)
                {
                    Assert.AreNotEqual(smallMaxInstanceCount, response);
                }
                else
                {
                    Assert.AreEqual(smallMaxInstanceCount, response);
                }
            }
            Assert.AreEqual(1, response);
        }

        [Test]
        public void WhenMediumExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int mediumMaxInstanceCount = 3;
            int response = 0;
            for (int i = 0; i < (mediumMaxInstanceCount + 1); i++)
            {
                response = mediumExchangeSetInstance.GetInstanceNumber(mediumMaxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response);
                Assert.AreEqual(mediumExchangeSetInstance.GetCurrentInstanceNumber(), response);
                if (mediumExchangeSetInstance.GetCurrentInstanceNumber() != mediumMaxInstanceCount)
                {
                    Assert.AreNotEqual(mediumMaxInstanceCount, response);
                }
                else
                {
                    Assert.AreEqual(mediumMaxInstanceCount, response);
                }
            }
            Assert.AreEqual(1, response);
        }

        [Test]
        public void WhenLargeExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int largeMaxInstanceCount = 2;
            int response = 0;
            for (int i = 0; i < (largeMaxInstanceCount + 1); i++)
            {
                response = largeExchangeSetInstance.GetInstanceNumber(largeMaxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response);
                Assert.AreEqual(largeExchangeSetInstance.GetCurrentInstanceNumber(), response);
                if (largeExchangeSetInstance.GetCurrentInstanceNumber() != largeMaxInstanceCount)
                {
                    Assert.AreNotEqual(largeMaxInstanceCount, response);
                }
                else
                {
                    Assert.AreEqual(largeMaxInstanceCount, response);
                }
            }
            Assert.AreEqual(1, response);
        }

        [Test]
        public void WhenAllExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int maxInstanceCount = 8;
            int smallMaxInstanceCount = 5;
            int mediumMaxInstanceCount = 2;
            int largeMaxInstanceCount = 3;
            int response1 = 0;
            int response2 = 0;
            int response3 = 0;
            for (int i = 0; i < (maxInstanceCount + 1); i++)
            {
                response1 = largeExchangeSetInstance.GetInstanceNumber(largeMaxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response1);
                Assert.IsNotNull(response1);
                Assert.AreEqual(largeExchangeSetInstance.GetCurrentInstanceNumber(), response1);
                if (largeExchangeSetInstance.GetCurrentInstanceNumber() != largeMaxInstanceCount)
                {
                    Assert.AreNotEqual(largeMaxInstanceCount, response1);
                }
                else
                {
                    Assert.AreEqual(largeMaxInstanceCount, response1);
                }

                response2 = mediumExchangeSetInstance.GetInstanceNumber(mediumMaxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response2);
                Assert.IsNotNull(response2);
                Assert.AreEqual(mediumExchangeSetInstance.GetCurrentInstanceNumber(), response2);
                if (mediumExchangeSetInstance.GetCurrentInstanceNumber() != mediumMaxInstanceCount)
                {
                    Assert.AreNotEqual(mediumMaxInstanceCount, response2);
                }
                else
                {
                    Assert.AreEqual(mediumMaxInstanceCount, response2);
                }

                response3 = smallExchangeSetInstance.GetInstanceNumber(smallMaxInstanceCount);
                Assert.IsInstanceOf(typeof(int), response3);
                Assert.IsNotNull(response3);
                Assert.AreEqual(smallExchangeSetInstance.GetCurrentInstanceNumber(), response3);
                if (smallExchangeSetInstance.GetCurrentInstanceNumber() != smallMaxInstanceCount)
                {
                    Assert.AreNotEqual(smallMaxInstanceCount, response3);
                }
                else
                {
                    Assert.AreEqual(smallMaxInstanceCount, response3);
                }
            }
            Assert.AreEqual(3, response1);
            Assert.AreEqual(1, response2);
            Assert.AreEqual(4, response3);
        }
    }
}
