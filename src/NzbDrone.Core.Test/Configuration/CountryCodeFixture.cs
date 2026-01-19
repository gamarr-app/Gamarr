using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Configuration
{
    [TestFixture]
    public class CountryCodeFixture : CoreTest
    {
        [Test]
        public void should_have_us_as_first_value()
        {
            CountryCode.US.Should().Be(CountryCode.US);
            ((int)CountryCode.US).Should().Be(0);
        }

        [Test]
        public void should_contain_major_regions()
        {
            var values = Enum.GetValues(typeof(CountryCode)).Cast<CountryCode>().ToList();

            values.Should().Contain(CountryCode.US);
            values.Should().Contain(CountryCode.GB);
            values.Should().Contain(CountryCode.DE);
            values.Should().Contain(CountryCode.FR);
            values.Should().Contain(CountryCode.JP);
        }

        [Test]
        public void should_have_unique_values()
        {
            var values = Enum.GetValues(typeof(CountryCode)).Cast<int>().ToList();
            var names = Enum.GetNames(typeof(CountryCode)).ToList();

            values.Should().OnlyHaveUniqueItems();
            names.Should().OnlyHaveUniqueItems();
        }

        [TestCase(CountryCode.US, "US")]
        [TestCase(CountryCode.GB, "GB")]
        [TestCase(CountryCode.AU, "AU")]
        [TestCase(CountryCode.DE, "DE")]
        [TestCase(CountryCode.JP, "JP")]
        public void should_have_correct_string_representation(CountryCode code, string expected)
        {
            code.ToString().Should().Be(expected);
        }

        [Test]
        public void should_be_parseable_from_string()
        {
            Enum.Parse<CountryCode>("US").Should().Be(CountryCode.US);
            Enum.Parse<CountryCode>("GB").Should().Be(CountryCode.GB);
            Enum.Parse<CountryCode>("JP").Should().Be(CountryCode.JP);
        }

        [Test]
        public void should_be_usable_in_config_service()
        {
            // CountryCode is used for CertificationCountry in config
            // This test verifies the enum is properly defined for configuration use
            var defaultValue = CountryCode.US;
            defaultValue.Should().BeDefined();
        }
    }
}
