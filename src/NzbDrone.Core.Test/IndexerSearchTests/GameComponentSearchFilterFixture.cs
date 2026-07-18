using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class GameComponentSearchFilterFixture : CoreTest
    {
        private static DownloadDecision Decision(string title, ReleaseContentType contentType)
        {
            return new DownloadDecision(new RemoteGame
            {
                Release = new ReleaseInfo { Title = title },
                ParsedGameInfo = new ParsedGameInfo { ContentType = contentType }
            });
        }

        private static List<DownloadDecision> AllKinds()
        {
            return new List<DownloadDecision>
            {
                Decision("Hades-RAZOR", ReleaseContentType.BaseGame),
                Decision("Hades.GOTY-GRP", ReleaseContentType.BaseGameWithAllDlc),
                Decision("Hades.Unknown-GRP", ReleaseContentType.Unknown),
                Decision("Hades.Update.v1.5-GRP", ReleaseContentType.UpdateOnly),
                Decision("Hades.The.Blood.Price.DLC-GRP", ReleaseContentType.DlcOnly),
                Decision("Hades.Warm.Winds.DLC-GRP", ReleaseContentType.DlcOnly)
            };
        }

        [Test]
        public void base_component_should_keep_only_base_game_releases()
        {
            var component = new GameComponent { ComponentType = GameComponentType.Base, Title = "Hades" };

            var result = GameSearchService.FilterDecisionsToComponent(AllKinds(), component);

            result.Select(d => d.RemoteGame.Release.Title)
                  .Should().BeEquivalentTo("Hades-RAZOR", "Hades.GOTY-GRP", "Hades.Unknown-GRP");
        }

        [Test]
        public void update_component_should_keep_only_update_releases()
        {
            var component = new GameComponent { ComponentType = GameComponentType.Update, Key = "v1.5", Title = "v1.5" };

            var result = GameSearchService.FilterDecisionsToComponent(AllKinds(), component);

            result.Select(d => d.RemoteGame.Release.Title)
                  .Should().BeEquivalentTo("Hades.Update.v1.5-GRP");
        }

        [Test]
        public void dlc_component_should_keep_only_matching_dlc_releases()
        {
            var component = new GameComponent { ComponentType = GameComponentType.Dlc, Key = "igdb:111", Title = "The Blood Price" };

            var result = GameSearchService.FilterDecisionsToComponent(AllKinds(), component);

            result.Select(d => d.RemoteGame.Release.Title)
                  .Should().BeEquivalentTo("Hades.The.Blood.Price.DLC-GRP");
        }

        [Test]
        public void dlc_component_with_empty_title_should_match_nothing()
        {
            var component = new GameComponent { ComponentType = GameComponentType.Dlc, Key = "igdb:111", Title = "" };

            var result = GameSearchService.FilterDecisionsToComponent(AllKinds(), component);

            result.Should().BeEmpty();
        }
    }
}
