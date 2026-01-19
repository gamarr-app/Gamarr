using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Specifications
{
    [TestFixture]
    public class NotSampleSpecificationFixture : CoreTest<NotSampleSpecification>
    {
        private Game _game;
        private LocalGame _localEpisode;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .Build();

            _localEpisode = new LocalGame
            {
                Path = @"C:\Test\30 Rock\30.rock.s01e01.avi",
                Game = _game,
            };
        }

        [Test]
        public void should_return_true_for_existing_file()
        {
            _localEpisode.ExistingFile = true;
            Subject.IsSatisfiedBy(_localEpisode, null).Accepted.Should().BeTrue();
        }
    }
}
