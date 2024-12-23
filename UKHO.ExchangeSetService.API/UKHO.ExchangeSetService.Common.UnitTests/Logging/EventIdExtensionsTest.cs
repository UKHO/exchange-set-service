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
            Assert.That((int)EventIds.SalesCatalogueServiceNonOkResponse, Is.EqualTo(EventIds.SalesCatalogueServiceNonOkResponse.ToEventId().Id));
        }

        [Test]
        public void WhenExtensionSetEventName_ThenValidateItIsSameAsToString()
        {
            Assert.That(EventIds.SCSPostProductVersionsRequestCompleted.ToEventId().Name, Is.EqualTo(EventIds.SCSPostProductVersionsRequestCompleted.ToString()));
        }
    }
}
