using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests.GameMetadataRepositoryTests
{
    [TestFixture]

    public class GameMetadataRepositoryFixture : DbTest<GameMetadataRepository, GameMetadata>
    {
        private GameMetadataRepository _gameMetadataRepo;
        private List<GameMetadata> _metadataList;

        [SetUp]
        public void Setup()
        {
            _gameMetadataRepo = Mocker.Resolve<GameMetadataRepository>();
            _metadataList = Builder<GameMetadata>.CreateListOfSize(10).All().With(x => x.Id = 0).BuildList();
        }

        [Test]
        public void upsert_many_should_insert_list_of_new()
        {
            var updated = _gameMetadataRepo.UpsertMany(_metadataList);
            AllStoredModels.Should().HaveCount(_metadataList.Count);
            updated.Should().BeTrue();
        }

        [Test]
        public void upsert_many_should_upsert_existing_with_id_0()
        {
            var clone = _metadataList.JsonClone();
            var updated = _gameMetadataRepo.UpsertMany(clone);

            updated.Should().BeTrue();
            AllStoredModels.Should().HaveCount(_metadataList.Count);

            updated = _gameMetadataRepo.UpsertMany(_metadataList);
            updated.Should().BeFalse();
            AllStoredModels.Should().HaveCount(_metadataList.Count);
        }

        [Test]
        public void upsert_many_should_upsert_mixed_list_of_old_and_new()
        {
            var clone = _metadataList.Take(5).ToList().JsonClone();
            var updated = _gameMetadataRepo.UpsertMany(clone);

            updated.Should().BeTrue();
            AllStoredModels.Should().HaveCount(clone.Count);

            updated = _gameMetadataRepo.UpsertMany(_metadataList);
            updated.Should().BeTrue();
            AllStoredModels.Should().HaveCount(_metadataList.Count);
        }
    }
}
