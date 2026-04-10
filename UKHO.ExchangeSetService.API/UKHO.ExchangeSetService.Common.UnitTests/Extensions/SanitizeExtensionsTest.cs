using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.UnitTests.Extensions
{
    [TestFixture]
    public class SanitizeExtensionsTest
    {
        [Test]
        public void WhenProductIdentifiersAreNull_ThenSanitizeProductIdentifiersReturnsNull()
        {
            string[] productIdentifiers = null;

            var result = productIdentifiers.SanitizeProductIdentifiers();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void WhenProductIdentifiersContainNull_ThenSanitizeProductIdentifiersReturnsArrayContainingNull()
        {
            string[] productIdentifiers = [" GB123456 ", null, " GB654321 "];

            var result = productIdentifiers.SanitizeProductIdentifiers();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0], Is.Null);
        }

        [Test]
        public void WhenProductIdentifiersAreProvided_ThenSanitizeProductIdentifiersTrimsAllValues()
        {
            string[] productIdentifiers = [" GB123456 ", "  GB654321", "GB111111  "];

            var result = productIdentifiers.SanitizeProductIdentifiers();

            Assert.That(result, Is.EqualTo(["GB123456", "GB654321", "GB111111"]));
        }

        [Test]
        public void WhenProductIdentifiersAreEmpty_ThenSanitizeProductIdentifiersReturnsEmptyArray()
        {
            string[] productIdentifiers = [];

            var result = productIdentifiers.SanitizeProductIdentifiers();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void WhenExchangeSetLayoutIsNullOrWhitespace_ThenSanitizeExchangeSetLayoutReturnsStandard(string input)
        {
            var result = input.SanitizeExchangeSetLayout();

            Assert.That(result, Is.EqualTo(ExchangeSetLayout.standard.ToString()));
        }

        [Test]
        public void WhenExchangeSetLayoutIsValid_ThenSanitizeExchangeSetLayoutReturnsTrimmedParsedValue()
        {
            var result = " LARGE ".SanitizeExchangeSetLayout();

            Assert.That(result, Is.EqualTo(ExchangeSetLayout.large.ToString()));
        }

        [Test]
        public void WhenExchangeSetLayoutIsInvalid_ThenSanitizeExchangeSetLayoutReturnsStandard()
        {
            var result = "invalid-layout".SanitizeExchangeSetLayout();

            Assert.That(result, Is.EqualTo(ExchangeSetLayout.standard.ToString()));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void WhenExchangeSetStandardIsNullOrWhitespace_ThenSanitizeExchangeSetStandardReturnsS63(string input)
        {
            var result = input.SanitizeExchangeSetStandard();

            Assert.That(result, Is.EqualTo(ExchangeSetStandard.s63.ToString()));
        }

        [Test]
        public void WhenExchangeSetStandardIsValid_ThenSanitizeExchangeSetStandardReturnsTrimmedParsedValue()
        {
            var result = " S57 ".SanitizeExchangeSetStandard();

            Assert.That(result, Is.EqualTo(ExchangeSetStandard.s57.ToString()));
        }

        [Test]
        public void WhenExchangeSetStandardIsInvalid_ThenSanitizeExchangeSetStandardReturnsS63()
        {
            var result = "invalid-standard".SanitizeExchangeSetStandard();

            Assert.That(result, Is.EqualTo(ExchangeSetStandard.s63.ToString()));
        }

        [Test]
        public void WhenStringIsNull_ThenSanitizeStringReturnsNull()
        {
            string input = null;

            var result = input.SanitizeString();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void WhenStringIsEmpty_ThenSanitizeStringReturnsEmptyString()
        {
            var result = string.Empty.SanitizeString();

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void WhenStringContainsControlCharacters_ThenSanitizeStringRemovesControlCharacters()
        {
            var result = "abc\r\ndef\tghi".SanitizeString();

            Assert.That(result, Is.EqualTo("abcdefghi"));
        }

        [Test]
        public void WhenStringContainsSpaces_ThenSanitizeStringPreservesSpaces()
        {
            var result = "value with spaces".SanitizeString();

            Assert.That(result, Is.EqualTo("value with spaces"));
        }
    }
}
