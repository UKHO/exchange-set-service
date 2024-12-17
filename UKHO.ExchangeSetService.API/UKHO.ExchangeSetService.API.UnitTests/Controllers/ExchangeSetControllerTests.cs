
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Controllers;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ExchangeSetControllerTests
    {
        private ExchangeSetController _fakeExchangeSetController;

        [SetUp]
        public void Setup()
        {
            _fakeExchangeSetController = new ExchangeSetController();
        }
    }
}
