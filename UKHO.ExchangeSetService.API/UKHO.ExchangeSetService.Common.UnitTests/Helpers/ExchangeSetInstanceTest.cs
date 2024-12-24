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
                Assert.That(response, Is.InstanceOf<int>());
                Assert.That(smallExchangeSetInstance.GetCurrentInstanceNumber(), Is.EqualTo(response));
                if (smallExchangeSetInstance.GetCurrentInstanceNumber() != smallMaxInstanceCount)
                {
                    Assert.That(smallMaxInstanceCount, Is.Not.EqualTo(response));
                }
                else
                {
                    Assert.That(smallMaxInstanceCount, Is.EqualTo(response));
                }
            }
            Assert.That(1, Is.EqualTo(response));
        }

        [Test]
        public void WhenMediumExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int mediumMaxInstanceCount = 3;
            int response = 0;
            for (int i = 0; i < (mediumMaxInstanceCount + 1); i++)
            {
                response = mediumExchangeSetInstance.GetInstanceNumber(mediumMaxInstanceCount);
                Assert.That(response, Is.InstanceOf<int>());
                Assert.That(mediumExchangeSetInstance.GetCurrentInstanceNumber(), Is.EqualTo(response));
                if (mediumExchangeSetInstance.GetCurrentInstanceNumber() != mediumMaxInstanceCount)
                {
                    Assert.That(mediumMaxInstanceCount, Is.Not.EqualTo(response));
                }
                else
                {
                    Assert.That(mediumMaxInstanceCount, Is.EqualTo(response));
                }
            }
            Assert.That(1, Is.EqualTo(response));
        }

        [Test]
        public void WhenLargeExchangeSetInstance_ThenReturnsNotNullInResponse()
        {
            int largeMaxInstanceCount = 2;
            int response = 0;
            for (int i = 0; i < (largeMaxInstanceCount + 1); i++)
            {
                response = largeExchangeSetInstance.GetInstanceNumber(largeMaxInstanceCount);
                Assert.That(response, Is.InstanceOf<int>());
                Assert.That(largeExchangeSetInstance.GetCurrentInstanceNumber(), Is.EqualTo(response));
                if (largeExchangeSetInstance.GetCurrentInstanceNumber() != largeMaxInstanceCount)
                {
                    Assert.That(largeMaxInstanceCount, Is.Not.EqualTo(response));
                }
                else
                {
                    Assert.That(largeMaxInstanceCount, Is.EqualTo(response));
                }
            }
            Assert.That(1, Is.EqualTo(response));
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
                Assert.That(response1, Is.InstanceOf<int>());
                Assert.That(response1, Is.Not.Null);
                Assert.That(largeExchangeSetInstance.GetCurrentInstanceNumber(), Is.EqualTo(response1));
                if (largeExchangeSetInstance.GetCurrentInstanceNumber() != largeMaxInstanceCount)
                {
                    Assert.That(largeMaxInstanceCount, Is.Not.EqualTo(response1));
                }
                else
                {
                    Assert.That(largeMaxInstanceCount, Is.EqualTo(response1));
                }

                response2 = mediumExchangeSetInstance.GetInstanceNumber(mediumMaxInstanceCount);
                Assert.That(response2, Is.InstanceOf<int>());
                Assert.That(response2,Is.Not.Null);
                Assert.That(mediumExchangeSetInstance.GetCurrentInstanceNumber(), Is.EqualTo(response2));
                if (mediumExchangeSetInstance.GetCurrentInstanceNumber() != mediumMaxInstanceCount)
                {
                    Assert.That(mediumMaxInstanceCount, Is.Not.EqualTo(response2));
                }
                else
                {
                    Assert.That(mediumMaxInstanceCount, Is.EqualTo(response2));
                }

                response3 = smallExchangeSetInstance.GetInstanceNumber(smallMaxInstanceCount);
                Assert.That(response3, Is.InstanceOf<int>());
                Assert.That(response3, Is.Not.Null);
                Assert.That(smallExchangeSetInstance.GetCurrentInstanceNumber(), Is.EqualTo(response3));
                if (smallExchangeSetInstance.GetCurrentInstanceNumber() != smallMaxInstanceCount)
                {
                    Assert.That(smallMaxInstanceCount, Is.Not.EqualTo(response3));
                }
                else
                {
                    Assert.That(smallMaxInstanceCount, Is.EqualTo(response3));
                }
            }
            Assert.That(3, Is.EqualTo(response1));
            Assert.That(1, Is.EqualTo(response2));
            Assert.That(4, Is.EqualTo(response3));
        }
    }
}
