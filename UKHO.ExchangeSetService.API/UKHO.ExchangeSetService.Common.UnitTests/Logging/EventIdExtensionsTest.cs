using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.UnitTests.Logging
{
    [TestFixture]
    public class EventIdExtensionsTest
    {
        [Test]
        public void WhenExtensionSetEventId_ThenValidateItIsEqualToActualValue()
        {
            Assert.AreEqual((int)EventIds.SalesCatalogueServiceNonOkResponse, EventIds.SalesCatalogueServiceNonOkResponse.ToEventId().Id);
        }

        [Test]
        public void WhenExtensionSetEventName_ThenValidateItIsSameAsToString()
        {
            Assert.AreEqual(EventIds.SCSPostProductVersionsRequestCompleted.ToEventId().Name, EventIds.SCSPostProductVersionsRequestCompleted.ToString());
        }
    }
}
