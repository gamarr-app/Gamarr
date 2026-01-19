using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Metadata.Consumers.Xbmc;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras.Metadata.Consumers.Xbmc
{
    [TestFixture]
    public class FindMetadataFileFixture : CoreTest<XbmcMetadata>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\Games\The.Game".AsOsAgnostic())
                                     .Build();
        }

        [Test]
        public void should_return_null_if_filename_is_not_handled()
        {
            var path = Path.Combine(_game.Path, "file.jpg");

            Subject.FindMetadataFile(_game, path).Should().BeNull();
        }

        [Test]
        public void should_return_metadata_for_xbmc_nfo()
        {
            var path = Path.Combine(_game.Path, "the.game.2017.nfo");

            Mocker.GetMock<IDetectXbmcNfo>()
                  .Setup(v => v.IsXbmcNfoFile(path))
                  .Returns(true);

            Subject.FindMetadataFile(_game, path).Type.Should().Be(MetadataType.GameMetadata);

            Mocker.GetMock<IDetectXbmcNfo>()
                  .Verify(v => v.IsXbmcNfoFile(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_return_null_for_scene_nfo()
        {
            var path = Path.Combine(_game.Path, "the.game.2017.nfo");

            Mocker.GetMock<IDetectXbmcNfo>()
                  .Setup(v => v.IsXbmcNfoFile(path))
                  .Returns(false);

            Subject.FindMetadataFile(_game, path).Should().BeNull();

            Mocker.GetMock<IDetectXbmcNfo>()
                  .Verify(v => v.IsXbmcNfoFile(It.IsAny<string>()), Times.Once());
        }
    }
}
