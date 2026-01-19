using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Metadata.Consumers.Wdtv;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Extras.Metadata.Consumers.Wdtv
{
    [TestFixture]
    public class FindMetadataFileFixture : CoreTest<WdtvMetadata>
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

        [TestCase(".xml", MetadataType.GameMetadata)]
        [TestCase(".metathumb", MetadataType.GameImage)]
        public void should_return_metadata_for_game_if_valid_file_for_game(string extension, MetadataType type)
        {
            var path = Path.Combine(_game.Path, "the.game.2011" + extension);

            Subject.FindMetadataFile(_game, path).Type.Should().Be(type);
        }

        [TestCase(".xml")]
        [TestCase(".metathumb")]
        public void should_return_null_if_not_valid_file_for_game(string extension)
        {
            var path = Path.Combine(_game.Path, "the.game" + extension);

            Subject.FindMetadataFile(_game, path).Should().BeNull();
        }

        [Test]
        public void should_return_game_image_for_folder_jpg_in_game_folder()
        {
            var path = Path.Combine(_game.Path, "folder.jpg");

            Subject.FindMetadataFile(_game, path).Type.Should().Be(MetadataType.GameImage);
        }
    }
}
