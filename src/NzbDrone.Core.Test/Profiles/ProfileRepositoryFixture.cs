using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Profiles
{
    [TestFixture]
    public class ProfileRepositoryFixture : DbTest<QualityProfileRepository, QualityProfile>
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void should_be_able_to_read_and_write()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(Quality.GOG, Quality.Scene, Quality.Uplay),
                MinFormatScore = 0,
                FormatItems = CustomFormatsTestHelpers.GetDefaultFormatItems(),
                Cutoff = Quality.GOG.Id,
                Name = "TestProfile"
            };

            Subject.Insert(profile);

            StoredModel.Name.Should().Be(profile.Name);
            StoredModel.Cutoff.Should().Be(profile.Cutoff);

            StoredModel.Items.Should().Equal(profile.Items, (a, b) => a.Quality == b.Quality && a.Allowed == b.Allowed);
        }
    }
}
