using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Collections;
using NzbDrone.Core.Games.Commands;
using NzbDrone.Core.Games.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Games.Collections
{
    [TestFixture]
    public class GameCollectionAddedHandlerFixture : CoreTest<GameCollectionAddedHandler>
    {
        private GameCollection _collection;

        [SetUp]
        public void Setup()
        {
            _collection = new GameCollection
            {
                Id = 7,
                IgdbId = 123,
                Title = "Test Collection"
            };
        }

        [Test]
        public void should_queue_refresh_collections_command_when_collection_is_added()
        {
            Subject.Handle(new CollectionAddedEvent(_collection));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push(
                      It.Is<RefreshCollectionsCommand>(c => c.CollectionIds.Contains(_collection.Id)),
                      CommandPriority.Normal,
                      CommandTrigger.Unspecified), Times.Once());
        }

        [Test]
        public void should_use_correct_collection_id_in_command()
        {
            _collection.Id = 55;

            Subject.Handle(new CollectionAddedEvent(_collection));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Push(
                      It.Is<RefreshCollectionsCommand>(c => c.CollectionIds.Count == 1 && c.CollectionIds[0] == 55),
                      CommandPriority.Normal,
                      CommandTrigger.Unspecified), Times.Once());
        }
    }
}
