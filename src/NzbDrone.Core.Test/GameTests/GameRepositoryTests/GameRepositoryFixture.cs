using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Games;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests.GameRepositoryTests
{
    [TestFixture]

    public class GameRepositoryFixture : DbTest<GameRepository, Game>
    {
        private IQualityProfileRepository _profileRepository;

        [SetUp]
        public void Setup()
        {
            _profileRepository = Mocker.Resolve<QualityProfileRepository>();
            Mocker.SetConstant<IQualityProfileRepository>(_profileRepository);

            Mocker.GetMock<ICustomFormatService>()
                .Setup(x => x.All())
                .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_load_quality_profile()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(Quality.GOG, Quality.Scene, Quality.Uplay),
                FormatItems = CustomFormatsTestHelpers.GetDefaultFormatItems(),
                MinFormatScore = 0,
                Cutoff = Quality.GOG.Id,
                Name = "TestProfile"
            };

            _profileRepository.Insert(profile);

            var game = Builder<Game>.CreateNew().BuildNew();
            game.QualityProfileId = profile.Id;

            Subject.Insert(game);

            Subject.All().Single().QualityProfile.Should().NotBeNull();
        }
    }
}
