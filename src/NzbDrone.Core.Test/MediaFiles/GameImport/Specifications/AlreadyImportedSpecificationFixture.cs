using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Specifications
{
    [TestFixture]
    public class AlreadyImportedSpecificationFixture : CoreTest<AlreadyImportedSpecification>
    {
        private Game _game;
        private LocalGame _localGame;
        private DownloadClientItem _downloadClientItem;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                .With(s => s.Path = @"C:\Test\Games\Casablanca".AsOsAgnostic())
                .Build();

            _localGame = new LocalGame
            {
                Path = @"C:\Test\Unsorted\Casablanca\Casablanca.1942.avi".AsOsAgnostic(),
                Game = _game
            };

            _downloadClientItem = Builder<DownloadClientItem>.CreateNew()
                .Build();
        }

        private void GivenHistory(List<GameHistory> history)
        {
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.GetByGameId(It.IsAny<int>(), null))
                .Returns(history);
        }

        [Test]
        public void should_accepted_if_download_client_item_is_null()
        {
            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_if_episode_does_not_have_file()
        {
            _game.GameFileId = 0;

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_if_episode_has_not_been_imported()
        {
            var history = Builder<GameHistory>.CreateListOfSize(1)
                .All()
                .With(h => h.GameId = _game.Id)
                .With(h => h.EventType = GameHistoryEventType.Grabbed)
                .Build()
                .ToList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_if_episode_was_grabbed_after_being_imported()
        {
            var history = Builder<GameHistory>.CreateListOfSize(3)
                .All()
                .With(h => h.GameId = _game.Id)
                .TheFirst(1)
                .With(h => h.EventType = GameHistoryEventType.Grabbed)
                .With(h => h.Date = DateTime.UtcNow)
                .TheNext(1)
                .With(h => h.EventType = GameHistoryEventType.DownloadFolderImported)
                .With(h => h.Date = DateTime.UtcNow.AddDays(-1))
                .TheNext(1)
                .With(h => h.EventType = GameHistoryEventType.Grabbed)
                .With(h => h.Date = DateTime.UtcNow.AddDays(-2))
                .Build()
                .ToList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_if_episode_imported_after_being_grabbed()
        {
            var history = Builder<GameHistory>.CreateListOfSize(2)
                .All()
                .With(h => h.GameId = _game.Id)
                .TheFirst(1)
                .With(h => h.EventType = GameHistoryEventType.DownloadFolderImported)
                .With(h => h.Date = DateTime.UtcNow.AddDays(-1))
                .TheNext(1)
                .With(h => h.EventType = GameHistoryEventType.Grabbed)
                .With(h => h.Date = DateTime.UtcNow.AddDays(-2))
                .Build()
                .ToList();

            GivenHistory(history);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeFalse();
        }
    }
}
