using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Specifications
{
    [TestFixture]
    public class MatchesFolderSpecificationFixture : CoreTest<MatchesFolderSpecification>
    {
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _localGame = Builder<LocalGame>.CreateNew()
                                                 .With(l => l.Path = @"C:\Test\Unsorted\Game.Title.v1.0-Gamarr\game.exe".AsOsAgnostic())
                                                 .With(l => l.FileGameInfo =
                                                     Builder<ParsedGameInfo>.CreateNew()
                                                                               .Build())
                                                 .Build();
        }

        [Test]
        public void should_be_accepted_for_existing_file()
        {
            _localGame.ExistingFile = true;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_for_new_file()
        {
            _localGame.ExistingFile = false;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }
    }
}
