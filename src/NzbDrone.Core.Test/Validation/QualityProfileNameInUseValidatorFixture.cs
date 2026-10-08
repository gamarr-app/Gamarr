using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class QualityProfileNameInUseValidatorFixture : CoreTest<QualityProfileNameInUseValidator>
    {
        private void GivenProfiles(params QualityProfile[] profiles)
        {
            Mocker.GetMock<IQualityProfileService>()
                  .Setup(s => s.All())
                  .Returns(new List<QualityProfile>(profiles));
        }

        [SetUp]
        public void Setup()
        {
            GivenProfiles(new QualityProfile { Id = 1, Name = "Any" },
                          new QualityProfile { Id = 2, Name = "DRM-Free" });
        }

        [Test]
        public void should_be_valid_when_name_is_not_used()
        {
            Subject.Validate(0, "Scene").Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_adding_a_profile_with_an_existing_name()
        {
            Subject.Validate(0, "Any").Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_existing_name_differs_only_by_case()
        {
            Subject.Validate(0, "aNy").Should().BeFalse();
        }

        [Test]
        public void should_be_valid_when_profile_keeps_its_own_name()
        {
            Subject.Validate(1, "Any").Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_renaming_onto_another_profiles_name()
        {
            Subject.Validate(1, "DRM-Free").Should().BeFalse();
        }

        [Test]
        public void should_be_valid_when_name_is_empty()
        {
            Subject.Validate(0, "").Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_no_profiles_exist()
        {
            GivenProfiles();

            Subject.Validate(0, "Any").Should().BeTrue();
        }
    }
}
