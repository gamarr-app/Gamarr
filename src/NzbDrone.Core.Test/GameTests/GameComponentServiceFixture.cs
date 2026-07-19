using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.AlternativeTitles;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.RomCatalog;
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

        private static NoIntroCatalogEntry Entry(string canonicalName, PlatformFamily platform, string numberedCanonicalFileName = null)
        {
            return new NoIntroCatalogEntry
            {
                SystemKey = platform == PlatformFamily.NintendoDS ? "nintendo---nintendo-ds" : "nintendo---game-boy-advance",
                CanonicalName = canonicalName,
                CanonicalFileName = $"{canonicalName}.nds",
                NumberedCanonicalFileName = numberedCanonicalFileName,
                PlatformFamily = platform
            };
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
        public void should_create_nointro_catalog_components_for_matching_game_title_and_platform()
        {
            _game.Platform = PlatformFamily.NintendoDS;
            _game.GameMetadata.Value.Title = "Mario Kart DS";
            _game.GameMetadata.Value.DlcReferences = new List<DlcReference>();

            Mocker.GetMock<INoIntroCatalogEntryRepository>()
                  .Setup(r => r.All())
                  .Returns(new List<NoIntroCatalogEntry>
                  {
                      Entry("Mario Kart DS (Europe) (En,Fr,De,Es,It)", PlatformFamily.NintendoDS),
                      Entry("Mario Kart DS (USA, Australia) (En,Fr,De,Es,It)", PlatformFamily.NintendoDS),
                      Entry("Mario Kart DS (Japan)", PlatformFamily.NintendoDS),
                      Entry("Pokemon Dash (USA)", PlatformFamily.NintendoDS),
                      Entry("Mario Kart DS (USA)", PlatformFamily.NintendoGBA)
                  });

            var inserted = CapturedInserts();

            inserted.Should().Contain(c => c.ComponentType == GameComponentType.Base && c.Key == "base");
            inserted.Where(c => c.ComponentType == GameComponentType.NoIntroRetailRom).Select(c => c.Title).Should().BeEquivalentTo(
                "Europe (En,Fr,De,Es,It)",
                "USA, Australia (En,Fr,De,Es,It)",
                "Japan");
            inserted.Where(c => c.ComponentType == GameComponentType.NoIntroRetailRom).Should().OnlyContain(c => c.Monitored && c.Key.StartsWith("nointro:retail:"));
        }

        [Test]
        public void should_create_download_play_components_for_matching_game_title_and_platform()
        {
            _game.Platform = PlatformFamily.NintendoDS;
            _game.GameMetadata.Value.Title = "Mario Kart DS";
            _game.GameMetadata.Value.DlcReferences = new List<DlcReference>();

            Mocker.GetMock<INoIntroCatalogEntryRepository>()
                  .Setup(r => r.All())
                  .Returns(new List<NoIntroCatalogEntry>
                  {
                      Entry("Mario Kart DS (Europe) (En,Fr,De,Es,It)", PlatformFamily.NintendoDS),
                      new NoIntroCatalogEntry
                      {
                          SystemKey = "nintendo---nintendo-ds--download-play",
                          CanonicalName = "Mario Kart DS (Europe) (Demo) (Download Station Vol. 1)",
                          CanonicalFileName = "Mario Kart DS (Europe) (Demo) (Download Station Vol. 1).nds",
                          PlatformFamily = PlatformFamily.NintendoDS
                      }
                  });

            var inserted = CapturedInserts();

            inserted.Should().Contain(c => c.ComponentType == GameComponentType.NoIntroRetailRom && c.Title == "Europe (En,Fr,De,Es,It)");
            inserted.Should().Contain(c => c.ComponentType == GameComponentType.NoIntroMultiboot && c.Title == "Europe (Demo) (Download Station Vol. 1)");
        }

        [Test]
        public void should_create_nointro_components_from_alternative_titles()
        {
            _game.Platform = PlatformFamily.NintendoDS;
            _game.GameMetadata.Value.Title = "Pokémon Weiß";
            _game.GameMetadata.Value.OriginalTitle = "Pokémon White Version";
            _game.GameMetadata.Value.DlcReferences = new List<DlcReference>();
            _game.GameMetadata.Value.AlternativeTitles = new List<AlternativeTitle>
            {
                new AlternativeTitle("Pokemon White"),
                new AlternativeTitle("Pocket Monsters White")
            };

            Mocker.GetMock<INoIntroCatalogEntryRepository>()
                  .Setup(r => r.All())
                  .Returns(new List<NoIntroCatalogEntry>
                  {
                      Entry("Pokemon - White Version (USA, Europe) (NDSi Enhanced)", PlatformFamily.NintendoDS),
                      Entry("Pokemon - Black Version (USA, Europe) (NDSi Enhanced)", PlatformFamily.NintendoDS)
                  });

            var inserted = CapturedInserts();

            inserted.Should().Contain(c => c.ComponentType == GameComponentType.NoIntroRetailRom && c.Title == "USA, Europe (NDSi Enhanced)");
            inserted.Where(c => c.ComponentType == GameComponentType.NoIntroRetailRom).Should().ContainSingle();
        }

        [Test]
        public void should_link_nointro_file_to_matching_catalog_component()
        {
            _game.Platform = PlatformFamily.NintendoDS;
            _game.GameMetadata.Value.Title = "Mario Kart DS";
            _game.GameMetadata.Value.DlcReferences = new List<DlcReference>();

            var baseSlot = new GameComponent { Id = 10, GameId = _game.Id, ComponentType = GameComponentType.Base, Key = "base" };
            var europeSlot = new GameComponent { Id = 11, GameId = _game.Id, ComponentType = GameComponentType.NoIntroRetailRom, Key = "nointro:retail:mario-kart-ds-europe-en-fr-de-es-it" };
            var file = new GameFile { Id = 1, GameId = _game.Id, RelativePath = "0201 - Mario Kart DS (Europe) (En,Fr,De,Es,It).nds" };

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.GetByGame(_game.Id))
                  .Returns(new List<GameComponent> { baseSlot, europeSlot });

            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { file });

            Mocker.GetMock<INoIntroCatalogEntryRepository>()
                  .Setup(r => r.All())
                  .Returns(new List<NoIntroCatalogEntry>
                  {
                      Entry("Mario Kart DS (Europe) (En,Fr,De,Es,It)", PlatformFamily.NintendoDS, "0201 - Mario Kart DS (Europe) (En,Fr,De,Es,It).nds")
                  });

            Subject.EnsureComponents(_game);

            file.ComponentId.Should().Be(11);
            Mocker.GetMock<IMediaFileService>()
                   .Verify(m => m.Update(It.Is<List<GameFile>>(l => l.Count == 1 && l[0].ComponentId == 11)), Times.Once());
        }

        [Test]
        public void should_link_folder_backed_nointro_file_to_matching_catalog_component()
        {
            _game.Platform = PlatformFamily.NintendoDS;
            _game.Path = "/games/Mario Kart DS";
            _game.GameMetadata.Value.Title = "Mario Kart DS";
            _game.GameMetadata.Value.DlcReferences = new List<DlcReference>();

            var baseSlot = new GameComponent { Id = 10, GameId = _game.Id, ComponentType = GameComponentType.Base, Key = "base" };
            var europeSlot = new GameComponent { Id = 11, GameId = _game.Id, ComponentType = GameComponentType.NoIntroRetailRom, Key = "nointro:retail:mario-kart-ds-europe-en-fr-de-es-it", Title = "Europe (En,Fr,De,Es,It)" };
            var file = new GameFile { Id = 1, GameId = _game.Id, RelativePath = string.Empty };

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.GetByGame(_game.Id))
                  .Returns(new List<GameComponent> { baseSlot, europeSlot });

            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { file });

            Mocker.GetMock<NzbDrone.Common.Disk.IDiskProvider>()
                  .Setup(d => d.FolderExists(_game.Path))
                  .Returns(true);

            Mocker.GetMock<NzbDrone.Common.Disk.IDiskProvider>()
                  .Setup(d => d.GetFiles(_game.Path, true))
                  .Returns(new List<string> { "/games/Mario Kart DS/0201 - Mario Kart DS (Europe) (En,Fr,De,Es,It).nds" });

            Mocker.GetMock<INoIntroCatalogEntryRepository>()
                  .Setup(r => r.All())
                  .Returns(new List<NoIntroCatalogEntry>
                  {
                      Entry("Mario Kart DS (Europe) (En,Fr,De,Es,It)", PlatformFamily.NintendoDS, "0201 - Mario Kart DS (Europe) (En,Fr,De,Es,It).nds")
                  });

            Subject.EnsureComponents(_game);

            file.ComponentId.Should().Be(11);
            Mocker.GetMock<IMediaFileService>()
                  .Verify(m => m.Update(It.Is<List<GameFile>>(l => l.Count == 1 && l[0].ComponentId == 11)), Times.Once());
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
        public void should_return_monitored_dlc_slots_without_files_as_missing()
        {
            var linked = new GameComponent { Id = 12, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:111", Title = "The Blood Price", Monitored = true };
            var missing = new GameComponent { Id = 13, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:222", Title = "Warm Winds", Monitored = true };

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.GetMonitoredDlc())
                  .Returns(new List<GameComponent> { linked, missing });

            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile>
                  {
                      new GameFile { Id = 3, GameId = _game.Id, RelativePath = "DLC/The.Blood.Price", ComponentId = 12 }
                  });

            var result = Subject.GetMonitoredMissingDlc();

            result.Should().ContainSingle(c => c.Id == 13);
        }

        [Test]
        public void should_link_imported_dlc_file_to_matching_metadata_slot()
        {
            var metadataSlot = new GameComponent { Id = 12, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:111", Title = "The Blood Price", Monitored = false };

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.GetByGame(_game.Id))
                  .Returns(new List<GameComponent>
                  {
                      new GameComponent { Id = 10, GameId = _game.Id, ComponentType = GameComponentType.Base, Key = "base" },
                      metadataSlot,
                      new GameComponent { Id = 13, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:222", Title = "Warm Winds" }
                  });

            var dlcFile = new GameFile { Id = 3, GameId = _game.Id, RelativePath = "DLC/Hades.The.Blood.Price.DLC-GRP" };

            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { dlcFile });

            Subject.EnsureComponents(_game);

            dlcFile.ComponentId.Should().Be(12);
            metadataSlot.Monitored.Should().BeTrue();

            // No duplicate import: slot for a DLC the metadata already tracks
            Mocker.GetMock<IGameComponentRepository>()
                  .Verify(r => r.InsertMany(It.IsAny<IList<GameComponent>>()), Times.Never());
        }

        [Test]
        public void should_merge_existing_import_slot_into_matching_metadata_slot()
        {
            var metadataSlot = new GameComponent { Id = 12, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:111", Title = "The Blood Price", Monitored = false };
            var importSlot = new GameComponent { Id = 20, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "import:Hades.The.Blood.Price.DLC-GRP", Title = "Hades.The.Blood.Price.DLC-GRP", Monitored = true };

            Mocker.GetMock<IGameComponentRepository>()
                  .Setup(r => r.GetByGame(_game.Id))
                  .Returns(new List<GameComponent>
                  {
                      new GameComponent { Id = 10, GameId = _game.Id, ComponentType = GameComponentType.Base, Key = "base" },
                      metadataSlot,
                      importSlot,
                      new GameComponent { Id = 13, GameId = _game.Id, ComponentType = GameComponentType.Dlc, Key = "igdb:222", Title = "Warm Winds" }
                  });

            var dlcFile = new GameFile { Id = 3, GameId = _game.Id, RelativePath = "DLC/Hades.The.Blood.Price.DLC-GRP", ComponentId = 20 };

            Mocker.GetMock<IMediaFileService>()
                  .Setup(m => m.GetFilesByGame(_game.Id))
                  .Returns(new List<GameFile> { dlcFile });

            Subject.EnsureComponents(_game);

            dlcFile.ComponentId.Should().Be(12);
            metadataSlot.Monitored.Should().BeTrue();

            Mocker.GetMock<IGameComponentRepository>()
                  .Verify(r => r.Delete(importSlot), Times.Once());
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
