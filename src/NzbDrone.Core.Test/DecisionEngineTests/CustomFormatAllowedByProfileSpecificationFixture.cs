using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class CustomFormatAllowedByProfileSpecificationFixture : CoreTest<CustomFormatAllowedbyProfileSpecification>
    {
        private RemoteGame _remoteGame;

        private CustomFormat _format1;
        private CustomFormat _format2;

        [SetUp]
        public void Setup()
        {
            _format1 = new CustomFormat("Awesome Format");
            _format1.Id = 1;

            _format2 = new CustomFormat("Cool Format");
            _format2.Id = 2;

            var fakeSeries = Builder<Game>.CreateNew()
                .With(c => c.QualityProfile = new QualityProfile
                {
                    Cutoff = Quality.GOG.Id,
                    MinFormatScore = 1
                })
                .Build();

            _remoteGame = new RemoteGame
            {
                Game = fakeSeries,
                ParsedGameInfo = new ParsedGameInfo { Quality = new QualityModel(Quality.Scene, new Revision(version: 2)) },
            };

            CustomFormatsTestHelpers.GivenCustomFormats(_format1, _format2);
        }

        [Test]
        public void should_allow_if_format_score_greater_than_min()
        {
            _remoteGame.CustomFormats = new List<CustomFormat> { _format1 };
            _remoteGame.Game.QualityProfile.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name);
            _remoteGame.CustomFormatScore = _remoteGame.Game.QualityProfile.CalculateCustomFormatScore(_remoteGame.CustomFormats);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_deny_if_format_score_not_greater_than_min()
        {
            _remoteGame.CustomFormats = new List<CustomFormat> { _format2 };
            _remoteGame.Game.QualityProfile.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name);
            _remoteGame.CustomFormatScore = _remoteGame.Game.QualityProfile.CalculateCustomFormatScore(_remoteGame.CustomFormats);

            Console.WriteLine(_remoteGame.CustomFormatScore);
            Console.WriteLine(_remoteGame.Game.QualityProfile.MinFormatScore);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_deny_if_format_score_not_greater_than_min_2()
        {
            _remoteGame.CustomFormats = new List<CustomFormat> { _format2, _format1 };
            _remoteGame.Game.QualityProfile.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name);
            _remoteGame.CustomFormatScore = _remoteGame.Game.QualityProfile.CalculateCustomFormatScore(_remoteGame.CustomFormats);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_allow_if_all_format_is_defined_in_profile()
        {
            _remoteGame.CustomFormats = new List<CustomFormat> { _format2, _format1 };
            _remoteGame.Game.QualityProfile.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name, _format2.Name);
            _remoteGame.CustomFormatScore = _remoteGame.Game.QualityProfile.CalculateCustomFormatScore(_remoteGame.CustomFormats);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_deny_if_no_format_was_parsed_and_min_score_positive()
        {
            _remoteGame.CustomFormats = new List<CustomFormat> { };
            _remoteGame.Game.QualityProfile.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name, _format2.Name);
            _remoteGame.CustomFormatScore = _remoteGame.Game.QualityProfile.CalculateCustomFormatScore(_remoteGame.CustomFormats);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_allow_if_no_format_was_parsed_min_score_is_zero()
        {
            _remoteGame.CustomFormats = new List<CustomFormat> { };
            _remoteGame.Game.QualityProfile.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(_format1.Name, _format2.Name);
            _remoteGame.Game.QualityProfile.MinFormatScore = 0;
            _remoteGame.CustomFormatScore = _remoteGame.Game.QualityProfile.CalculateCustomFormatScore(_remoteGame.CustomFormats);

            Subject.IsSatisfiedBy(_remoteGame, null).Accepted.Should().BeTrue();
        }
    }
}
