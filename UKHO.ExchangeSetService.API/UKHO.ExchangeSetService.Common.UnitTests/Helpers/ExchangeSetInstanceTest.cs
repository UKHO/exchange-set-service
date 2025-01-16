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
                Assert.That(response, Is.EqualTo(smallExchangeSetInstance.GetCurrentInstanceNumber()));
                if (smallExchangeSetInstance.GetCurrentInstanceNumber() != smallMaxInstanceCount)
                {
                    Assert.That(response, Is.Not.EqualTo(smallMaxInstanceCount));
                }
                else
                {
                    Assert.That(response, Is.EqualTo(smallMaxInstanceCount));
                }
            }
            Assert.That(response, Is.EqualTo(1));
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
                Assert.That(response, Is.EqualTo(mediumExchangeSetInstance.GetCurrentInstanceNumber()));
                if (mediumExchangeSetInstance.GetCurrentInstanceNumber() != mediumMaxInstanceCount)
                {
                    Assert.That(response, Is.Not.EqualTo(mediumMaxInstanceCount));
                }
                else
                {
                    Assert.That(response, Is.EqualTo(mediumMaxInstanceCount));
                }
            }
            Assert.That(response, Is.EqualTo(1));
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
                Assert.That(response, Is.EqualTo(largeExchangeSetInstance.GetCurrentInstanceNumber()));
                if (largeExchangeSetInstance.GetCurrentInstanceNumber() != largeMaxInstanceCount)
                {
                    Assert.That(response, Is.Not.EqualTo(largeMaxInstanceCount));
                }
                else
                {
                    Assert.That(response, Is.EqualTo(largeMaxInstanceCount));
                }
            }
            Assert.That(response, Is.EqualTo(1));
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
                Assert.That(response1, Is.EqualTo(largeExchangeSetInstance.GetCurrentInstanceNumber()));
                if (largeExchangeSetInstance.GetCurrentInstanceNumber() != largeMaxInstanceCount)
                {
                    Assert.That(response1, Is.Not.EqualTo(largeMaxInstanceCount));
                }
                else
                {
                    Assert.That(response1, Is.EqualTo(largeMaxInstanceCount));
                }

                response2 = mediumExchangeSetInstance.GetInstanceNumber(mediumMaxInstanceCount);
                Assert.That(response2, Is.InstanceOf<int>());
                Assert.That(response2, Is.EqualTo(mediumExchangeSetInstance.GetCurrentInstanceNumber()));
                if (mediumExchangeSetInstance.GetCurrentInstanceNumber() != mediumMaxInstanceCount)
                {
                    Assert.That(response2, Is.Not.EqualTo(mediumMaxInstanceCount));
                }
                else
                {
                    Assert.That(response2, Is.EqualTo(mediumMaxInstanceCount));
                }

                response3 = smallExchangeSetInstance.GetInstanceNumber(smallMaxInstanceCount);
                Assert.That(response3, Is.InstanceOf<int>());
                Assert.That(response3, Is.EqualTo(smallExchangeSetInstance.GetCurrentInstanceNumber()));
                if (smallExchangeSetInstance.GetCurrentInstanceNumber() != smallMaxInstanceCount)
                {
                    Assert.That(response3, Is.Not.EqualTo(smallMaxInstanceCount));
                }
                else
                {
                    Assert.That(response3, Is.EqualTo(smallMaxInstanceCount));
                }
            }
            Assert.That(response1, Is.EqualTo(3));
            Assert.That(response2, Is.EqualTo(1));
            Assert.That(response3, Is.EqualTo(4));
        }
    }
}
