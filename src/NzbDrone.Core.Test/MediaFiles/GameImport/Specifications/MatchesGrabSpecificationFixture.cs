using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
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
    public class MatchesGrabSpecificationFixture : CoreTest<MatchesGrabSpecification>
    {
        private Game _game1;
        private Game _game2;
        private Game _game3;
        private LocalGame _localGame;
        private DownloadClientItem _downloadClientItem;

        [SetUp]
        public void Setup()
        {
            _game1 = Builder<Game>.CreateNew()
                .With(e => e.Id = 1)
                .Build();

            _game2 = Builder<Game>.CreateNew()
                .With(e => e.Id = 2)
                .Build();

            _game3 = Builder<Game>.CreateNew()
                .With(e => e.Id = 3)
                .Build();

            _localGame = Builder<LocalGame>.CreateNew()
                                                 .With(l => l.Path = @"C:\Test\Unsorted\Series.Title.S01E01.720p.HDTV-Sonarr\S01E05.mkv".AsOsAgnostic())
                                                 .With(l => l.Game = _game1)
                                                 .With(l => l.Release = null)
                                                 .Build();

            _downloadClientItem = Builder<DownloadClientItem>.CreateNew().Build();
        }

        private void GivenHistoryForGames(params Game[] games)
        {
            if (games.Empty())
            {
                return;
            }

            var grabbedHistories = Builder<GameHistory>.CreateListOfSize(games.Length)
                .All()
                .With(h => h.EventType == GameHistoryEventType.Grabbed)
                .BuildList();

            for (var i = 0; i < grabbedHistories.Count; i++)
            {
                grabbedHistories[i].GameId = games[i].Id;
            }

            _localGame.Release = new GrabbedReleaseInfo(grabbedHistories);
        }

        [Test]
        public void should_be_accepted_for_existing_file()
        {
            _localGame.ExistingFile = true;

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_no_download_client_item()
        {
            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_no_grab_release_info()
        {
            GivenHistoryForGames();

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_file_episode_matches_single_grab_release_info()
        {
            GivenHistoryForGames(_game1);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_rejected_if_file_episode_does_not_match_single_grab_release_info()
        {
            GivenHistoryForGames(_game2);

            Subject.IsSatisfiedBy(_localGame, _downloadClientItem).Accepted.Should().BeFalse();
        }
    }
}
