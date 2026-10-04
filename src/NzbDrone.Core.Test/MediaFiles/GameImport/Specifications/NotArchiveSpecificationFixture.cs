using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.MediaFiles.Archives;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Specifications
{
    [TestFixture]
    public class NotArchiveSpecificationFixture : CoreTest<NotArchiveSpecification>
    {
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _localGame = new LocalGame
            {
                Path = @"C:\Test\Unsorted\Kirby (BlueRoms)\Kirby.xci".AsOsAgnostic(),
                Size = 100,
                Game = Builder<Game>.CreateNew().Build()
            };
        }

        private void GivenFile(string fileName)
        {
            _localGame.Path = (@"C:\Test\Unsorted\Kirby (BlueRoms)\" + fileName).AsOsAgnostic();
        }

        [TestCase("Kirby.7z")]
        [TestCase("Kirby.zip")]
        [TestCase("Kirby.rar")]
        public void should_reject_an_archive_that_was_not_inspected(string fileName)
        {
            GivenFile(fileName);

            var decision = Subject.IsSatisfiedBy(_localGame, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(ImportRejectionReason.ArchiveFile);
        }

        [Test]
        public void should_accept_an_archive_with_an_inspected_game_file()
        {
            GivenFile("Kirby.7z");

            _localGame.ArchiveInspection = new GameArchiveInspection
            {
                ArchivePath = _localGame.Path,
                EntryName = "Kirby.xci",
                EntrySize = 7985954816
            };

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [TestCase("Kirby.xci")]
        [TestCase("Kirby.nsp")]
        [TestCase("Game.Title.iso")]
        public void should_accept_anything_that_is_not_an_archive(string fileName)
        {
            GivenFile(fileName);

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_a_folder_shaped_release()
        {
            _localGame.Path = @"C:\Test\Unsorted\Kirby (BlueRoms)".AsOsAgnostic();

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }
    }
}
