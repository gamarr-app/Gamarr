using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class GameComponentServiceFixture : CoreTest<GameComponentService>
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = new Game
            {
                Id = 5,
                GameMetadata = new GameMetadata
                {
                    Title = "Hades",
                    DlcReferences = new List<DlcReference>
                    {
                        new DlcReference(111, "The Blood Price"),
                        new DlcReference(222, "Warm Winds")
                    }
                }
            };

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.GetByGame(_game.Id))
                  .Returns(new List<GameComponent>());

            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile>());
        }

        private List<GameComponent> CapturedInserts()
        {
            List<GameComponent> captured = null;

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.InsertMany(It.IsAny<IList<GameComponent>>()))
                  .Callback<IList<GameComponent>>(list => captured = list.ToList());

            Subject.EnsureComponents(_game);

            return captured ?? new List<GameComponent>();
        }

        [Test]
        public void should_create_base_and_metadata_dlc_components()
        {
            var inserted = CapturedInserts();

            inserted.Should().Contain(c => c.ComponentType == GameComponentType.Base && c.Key == "base" && c.Monitored);

            // Metadata DLC slots exist but start unmonitored (opt-in per DLC)
            inserted.Where(c => c.ComponentType == GameComponentType.Dlc).Should().HaveCount(2);
            inserted.Where(c => c.ComponentType == GameComponentType.Dlc).Should().OnlyContain(c => !c.Monitored && c.ExternalId > 0);
            inserted.Should().Contain(c => c.Key == "igdb:111" && c.Title == "The Blood Price");
        }

        [Test]
        public void should_create_update_and_imported_dlc_components_from_files()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile>
                  {
                      new GameFile { Id = 1, GameId = _game.Id, RelativePath = "" },
                      new GameFile { Id = 2, GameId = _game.Id, RelativePath = "Updates/v1.5" },
                      new GameFile { Id = 3, GameId = _game.Id, RelativePath = "DLC/Some.Release.DLC-GRP" }
                  });

            var inserted = CapturedInserts();

            inserted.Should().Contain(c => c.ComponentType == GameComponentType.Update && c.Key == "v1.5" && c.Monitored);
            inserted.Should().Contain(c => c.ComponentType == GameComponentType.Dlc && c.Key == "import:Some.Release.DLC-GRP" && c.Monitored);
        }

        [Test]
        public void should_be_idempotent_when_components_already_exist()
        {
            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.GetByGame(_game.Id))
                  .Returns(new List<GameComponent>
                  {
                      new GameComponent { Id = 1, GameId = _game.Id, ComponentType = GameComponentType.Base, Key = "base" },
                      new GameComponent { Id = 2, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:111" },
                      new GameComponent { Id = 3, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:222" }
                  });

            Subject.EnsureComponents(_game);

            Mocker.GetMock<IGameComponentRepository>()
                  .Verify(r => r.InsertMany(It.IsAny<IList<GameComponent>>()), Times.Never());
        }

        [Test]
        public void should_reconcile_components_when_a_file_is_added_by_import()
        {
            Mocker.GetMock<IGameService>()
                  .Setup(s => s.GetGame(_game.Id))
                  .Returns(_game);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile>
                  {
                      new GameFile { Id = 2, GameId = _game.Id, RelativePath = "Updates/v1.5" }
                  });

            List<GameComponent> captured = null;

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.InsertMany(It.IsAny<IList<GameComponent>>()))
                  .Callback<IList<GameComponent>>(list => captured = list.ToList());

            Subject.Handle(new GameFileAddedEvent(new GameFile { Id = 2, GameId = _game.Id, RelativePath = "Updates/v1.5" }));

            captured.Should().NotBeNull();
            captured.Should().Contain(c => c.ComponentType == GameComponentType.Update && c.Key == "v1.5");
        }

        [Test]
        public void should_link_files_to_their_existing_components()
        {
            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.GetByGame(_game.Id))
                  .Returns(new List<GameComponent>
                  {
                      new GameComponent { Id = 10, GameId = _game.Id, ComponentType = GameComponentType.Base, Key = "base" },
                      new GameComponent { Id = 11, GameId = _game.Id, ComponentType = GameComponentType.Update, Key = "v1.5" },
                      new GameComponent { Id = 12, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:111" },
                      new GameComponent { Id = 13, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:222" }
                  });

            var baseFile = new GameFile { Id = 1, GameId = _game.Id, RelativePath = "" };
            var updateFile = new GameFile { Id = 2, GameId = _game.Id, RelativePath = "Updates/v1.5" };

            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { baseFile, updateFile });

            Subject.EnsureComponents(_game);

            baseFile.ComponentId.Should().Be(10);
            updateFile.ComponentId.Should().Be(11);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(m => m.Update(It.Is<List<GameFile>>(l => l.Count == 2)), Times.Once());
        }
    }
}
