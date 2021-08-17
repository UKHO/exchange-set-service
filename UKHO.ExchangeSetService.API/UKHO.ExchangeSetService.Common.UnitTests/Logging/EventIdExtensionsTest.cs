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
            Assert.AreEqual(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId().Id, (int)EventIds.SalesCatalogueServiceNonOkResponse);
        }

        [Test]
        public void WhenExtensionSetEventName_ThenValidateItIsSameAsToString()
        {
            Assert.AreEqual(EventIds.SCSPostProductVersionsRequestCompleted.ToEventId().Name, EventIds.SCSPostProductVersionsRequestCompleted.ToString());
        }
    }
}
