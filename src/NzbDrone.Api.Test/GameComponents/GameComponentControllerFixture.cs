using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Gamarr.Api.V3.GameComponents;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RomCatalog;
using NzbDrone.Test.Common;

namespace NzbDrone.Api.Test.GameComponents
{
    [TestFixture]
    public class GameComponentControllerFixture : TestBase<GameComponentController>
    {
        private GameFile _gameFile;

        [SetUp]
        public void Setup()
        {
            _gameFile = new GameFile
            {
                Id = 41,
                GameId = 51,
                ComponentId = 50,
                RelativePath = "base.nsp",
                Size = 33554432
            };

            Mocker.GetMock<IMediaFileService>()
                  .Setup(x => x.GetFilesByGame(51))
                  .Returns(new List<GameFile> { _gameFile });

            Mocker.GetMock<IGameComponentService>()
                  .Setup(x => x.GetByGame(51))
                  .Returns(new List<GameComponent>
                  {
                      new GameComponent
                      {
                          Id = 50,
                          GameId = 51,
                          ComponentType = GameComponentType.Base,
                          Key = "base",
                          Title = "Mario Kart DS"
                      }
                  });

            Mocker.GetMock<INoIntroCatalogEntryRepository>()
                  .Setup(x => x.All())
                  .Returns(new List<NoIntroCatalogEntry>
                  {
                      new NoIntroCatalogEntry
                      {
                          Id = 1,
                          CatalogSourceId = 7,
                          SystemKey = "nintendo---nintendo-ds",
                          CanonicalName = "Mario Kart DS (Europe)",
                          CanonicalFileName = "Mario Kart DS (Europe).nds"
                      }
                  });

            Mocker.GetMock<INoIntroCatalogSourceRepository>()
                  .Setup(x => x.All())
                  .Returns(new List<NoIntroCatalogSource> { new NoIntroCatalogSource { Id = 7, Name = "No-Intro Nintendo DS" } });

            Mocker.GetMock<INoIntroCatalogHashRepository>()
                  .Setup(x => x.GetByEntryIds(It.IsAny<List<int>>()))
                  .Returns(new List<NoIntroCatalogHash>
                  {
                      new NoIntroCatalogHash
                      {
                          Id = 2,
                          CatalogEntryId = 1,
                          HashType = "sha1",
                          HashValue = "CE97D9B43F0D3CA0D48B781983E8A16F6393378F",
                          IsPrimary = true
                      }
                  });
        }

        [Test]
        public void should_report_the_exact_match_from_the_stored_hash_without_reading_the_file()
        {
            Mocker.GetMock<IGameFileHashService>()
                  .Setup(x => x.GetCurrentHashes(It.IsAny<List<GameFile>>()))
                  .Returns(new List<GameFileHash>
                  {
                      new GameFileHash
                      {
                          GameFileId = 41,
                          Size = 33554432,
                          Sha1 = "CE97D9B43F0D3CA0D48B781983E8A16F6393378F"
                      }
                  });

            var resource = Subject.GetComponents(51).Should().ContainSingle().Subject;

            resource.NoIntroCatalogMatches.Should().ContainSingle(x => x.HashType == "sha1" && x.CanonicalFileName == "Mario Kart DS (Europe).nds");

            // Nothing was hashed and nothing needs hashing (#205).
            Mocker.GetMock<IGameFileHashService>().Verify(x => x.QueueHashing(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_omit_the_hash_match_and_queue_hashing_when_a_file_has_no_stored_hash()
        {
            Mocker.GetMock<IGameFileHashService>()
                  .Setup(x => x.GetCurrentHashes(It.IsAny<List<GameFile>>()))
                  .Returns(new List<GameFileHash>());

            var resource = Subject.GetComponents(51).Should().ContainSingle().Subject;

            // The title match still renders; only the exact hash is missing.
            resource.NoIntroCatalogMatches.Should().OnlyContain(x => x.HashType == null && x.HashValue == null);
            Mocker.GetMock<IGameFileHashService>().Verify(x => x.QueueHashing(51), Times.Once());
        }

        [Test]
        public void should_not_depend_on_the_disk_or_game_path_at_all()
        {
            // The 40-400 s GET came from hashing the game's files inside the
            // request (#205). Taking neither IDiskProvider nor IGameService is
            // what makes that impossible to reintroduce by accident.
            var parameterTypes = typeof(GameComponentController).GetConstructors().Single().GetParameters().Select(x => x.ParameterType);

            parameterTypes.Should().NotContain(typeof(IDiskProvider)).And.NotContain(typeof(IGameService));
        }
    }
}
