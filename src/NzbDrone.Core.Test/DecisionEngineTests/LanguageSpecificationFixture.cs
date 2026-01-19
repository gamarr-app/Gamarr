using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class LanguageSpecificationFixture : CoreTest
    {
        private RemoteGame _remoteGame;

        [SetUp]
        public void Setup()
        {
            _remoteGame = new RemoteGame
            {
                ParsedGameInfo = new ParsedGameInfo
                {
                    Languages = new List<Language> { Language.English }
                },
                Game = new Game
                         {
                             QualityProfile = new QualityProfile
                             {
                                 Language = Language.English
                             },
                             GameMetadata = new GameMetadata
                             {
                                 OriginalLanguage = Language.French
                             }
                         }
            };
        }

        private void WithEnglishRelease()
        {
            _remoteGame.Languages = new List<Language> { Language.English };
        }

        private void WithGermanRelease()
        {
            _remoteGame.Languages = new List<Language> { Language.German };
        }

        private void WithFrenchRelease()
        {
            _remoteGame.Languages = new List<Language> { Language.French };
        }

        [Test]
        public void should_return_true_if_language_is_english()
        {
            WithEnglishRelease();

            Mocker.Resolve<LanguageSpecification>().IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_language_is_german()
        {
            WithGermanRelease();

            Mocker.Resolve<LanguageSpecification>().IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_release_is_german_and_profile_original()
        {
            _remoteGame.Game.QualityProfile.Language = Language.Original;

            WithGermanRelease();

            Mocker.Resolve<LanguageSpecification>().IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_release_is_french_and_profile_original()
        {
            _remoteGame.Game.QualityProfile.Language = Language.Original;

            WithFrenchRelease();

            Mocker.Resolve<LanguageSpecification>().IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_allowed_language_any()
        {
            _remoteGame.Game.QualityProfile = new QualityProfile
            {
                Language = Language.Any
            };

            WithGermanRelease();

            Mocker.Resolve<LanguageSpecification>().IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();

            WithEnglishRelease();

            Mocker.Resolve<LanguageSpecification>().IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }
    }
}
