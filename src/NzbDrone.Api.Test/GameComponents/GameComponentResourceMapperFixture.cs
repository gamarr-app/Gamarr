using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Gamarr.Api.V3.GameComponents;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RomCatalog;

namespace NzbDrone.Api.Test.GameComponents
{
    [TestFixture]
    public class GameComponentResourceMapperFixture
    {
        [Test]
        public void should_prefer_hash_matched_nointro_entry_over_title_matches()
        {
            var component = new GameComponent
            {
                Id = 50,
                GameId = 51,
                Title = "Mario Kart DS"
            };

            var context = new GameComponentNoIntroCatalogContext
            {
                GameFiles = new List<GameFile>
                {
                    new GameFile
                    {
                        Id = 41,
                        ComponentId = 50,
                        Size = 33554432
                    }
                },
                Entries = new List<NoIntroCatalogEntry>
                {
                    Entry(1, "Mario Kart DS (Europe) (En,Fr,De,Es,It)"),
                    Entry(2, "Mario Kart DS (Japan)"),
                    Entry(3, "Mario Kart DS (USA, Australia) (En,Fr,De,Es,It)")
                },
                Sources = new List<NoIntroCatalogSource>
                {
                    new NoIntroCatalogSource
                    {
                        Id = 7,
                        Name = "No-Intro Nintendo DS",
                        CatalogVersion = "2026.05.02"
                    }
                },
                HashMatches = new List<NoIntroCatalogFileHashMatch>
                {
                    new NoIntroCatalogFileHashMatch
                    {
                        GameFileId = 41,
                        CatalogEntryId = 1,
                        HashType = "sha1",
                        HashValue = "CE97D9B43F0D3CA0D48B781983E8A16F6393378F"
                    }
                }
            };

            var resource = component.ToResource(context);

            resource.NoIntroCatalogMatches.Should().ContainSingle()
                .Subject.Should().Match<GameComponentNoIntroCatalogResource>(match =>
                    match.CanonicalFileName == "Mario Kart DS (Europe) (En,Fr,De,Es,It).nds" &&
                    match.HashType == "sha1" &&
                    match.HashValue == "CE97D9B43F0D3CA0D48B781983E8A16F6393378F");
        }

        private static NoIntroCatalogEntry Entry(int id, string canonicalName)
        {
            return new NoIntroCatalogEntry
            {
                Id = id,
                CatalogSourceId = 7,
                SystemKey = "nintendo---nintendo-ds",
                CanonicalName = canonicalName,
                CanonicalFileName = $"{canonicalName}.nds"
            };
        }
    }
}
