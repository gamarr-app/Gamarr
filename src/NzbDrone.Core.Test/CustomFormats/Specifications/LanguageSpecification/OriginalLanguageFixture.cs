using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats.Specifications.LanguageSpecification
{
    [TestFixture]
    public class OriginalLanguageFixture : CoreTest<Core.CustomFormats.LanguageSpecification>
    {
        private CustomFormatInput _input;

        [SetUp]
        public void Setup()
        {
            _input = new CustomFormatInput
            {
                GameInfo = Builder<ParsedGameInfo>.CreateNew().Build(),
                Game = Builder<Game>.CreateNew().With(m => m.GameMetadata.Value.OriginalLanguage = Language.English).Build(),
                Size = 100.Megabytes(),
                Languages = new List<Language>
                {
                    Language.French
                },
                Filename = "Game.Title.2024"
            };
        }

        public void GivenLanguages(params Language[] languages)
        {
            _input.Languages = languages.ToList();
        }

        [Test]
        public void should_match_same_single_language()
        {
            GivenLanguages(Language.English);

            Subject.Value = Language.Original.Id;
            Subject.Negate = false;

            Subject.IsSatisfiedBy(_input).Should().BeTrue();
        }

        [Test]
        public void should_not_match_different_single_language()
        {
            Subject.Value = Language.Original.Id;
            Subject.Negate = false;

            Subject.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_not_match_negated_same_single_language()
        {
            GivenLanguages(Language.English);

            Subject.Value = Language.Original.Id;
            Subject.Negate = true;

            Subject.IsSatisfiedBy(_input).Should().BeFalse();
        }

        [Test]
        public void should_match_negated_different_single_language()
        {
            Subject.Value = Language.Original.Id;
            Subject.Negate = true;

            Subject.IsSatisfiedBy(_input).Should().BeTrue();
        }
    }
}
