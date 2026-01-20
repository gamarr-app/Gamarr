using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Test.ValidationTests
{
    [TestFixture]
    public class UrlValidatorFixture
    {
        private TestValidator _validator;

        private class TestModel
        {
            public string Url { get; set; }
        }

        private class TestValidator : AbstractValidator<TestModel>
        {
            public TestValidator()
            {
                RuleFor(x => x.Url).IsValidUrl();
            }
        }

        [SetUp]
        public void Setup()
        {
            _validator = new TestValidator();
        }

        [Test]
        public void should_be_valid_for_http_url()
        {
            var model = new TestModel { Url = "http://example.com" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_for_https_url()
        {
            var model = new TestModel { Url = "https://example.com" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_for_url_with_port()
        {
            var model = new TestModel { Url = "http://example.com:8080" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_valid_for_url_with_path()
        {
            var model = new TestModel { Url = "http://example.com/path/to/resource" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_for_null()
        {
            var model = new TestModel { Url = null };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_for_empty()
        {
            var model = new TestModel { Url = "" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_for_random_string()
        {
            var model = new TestModel { Url = "not a url" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_without_scheme()
        {
            var model = new TestModel { Url = "example.com" };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
        }
    }
}
