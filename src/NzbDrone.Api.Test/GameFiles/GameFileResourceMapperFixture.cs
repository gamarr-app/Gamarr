using FluentAssertions;
using NUnit.Framework;
using Gamarr.Api.V3.GameFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Api.Test.GameFiles
{
    [TestFixture]
    public class GameFileResourceMapperFixture
    {
        [Test]
        public void should_use_linked_component_metadata_for_folder_backed_nointro_files()
        {
            var game = new Game { Path = "/games/Mario Kart DS" };
            var gameFile = new GameFile
            {
                Id = 43,
                GameId = 51,
                RelativePath = string.Empty,
                ComponentId = 53
            };
            var component = new GameComponent
            {
                Id = 53,
                GameId = 51,
                ComponentType = GameComponentType.NoIntroRetailRom,
                Key = "nointro:retail:mario-kart-ds-europe-en-fr-de-es-it",
                Title = "Europe (En,Fr,De,Es,It)"
            };

            var resource = gameFile.ToResource(game, null, null, component);

            resource.ComponentType.Should().Be("noIntroRetailRom");
            resource.ComponentKey.Should().Be(component.Key);
            resource.ComponentTitle.Should().Be(component.Title);
        }

        [Test]
        public void should_fall_back_to_derived_base_type_when_component_is_unknown()
        {
            var game = new Game { Path = "/games/Mario Kart DS" };
            var gameFile = new GameFile
            {
                Id = 43,
                GameId = 51,
                RelativePath = string.Empty,
                ComponentId = 0
            };

            var resource = gameFile.ToResource(game, null, null);

            resource.ComponentType.Should().Be("base");
            resource.ComponentKey.Should().BeNull();
            resource.ComponentTitle.Should().BeNull();
        }
    }
}
