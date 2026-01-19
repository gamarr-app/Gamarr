using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Integration.Test.Client;
using Gamarr.Api.V3.Games;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class CalendarFixture : IntegrationTest
    {
        public ClientBase<GameResource> Calendar;

        protected override void InitRestClients()
        {
            base.InitRestClients();

            Calendar = new ClientBase<GameResource>(RestClient, ApiKey, "calendar");
        }

        [Test]
        public void should_be_able_to_get_games()
        {
            var game = EnsureGame(680, "Pulp Fiction", true);

            var request = Calendar.BuildRequest();
            request.AddParameter("start", new DateTime(1993, 10, 1).ToString("s") + "Z");
            request.AddParameter("end", new DateTime(1995, 10, 3).ToString("s") + "Z");
            var items = Calendar.Get<List<GameResource>>(request);

            items = items.Where(v => v.Id == game.Id).ToList();

            items.Should().HaveCount(1);
            items.First().Title.Should().Be("Pulp Fiction");
        }

        [Test]
        public void should_not_be_able_to_get_unmonitored_games()
        {
            var game = EnsureGame(680, "Pulp Fiction", false);

            var request = Calendar.BuildRequest();
            request.AddParameter("start", new DateTime(1993, 10, 1).ToString("s") + "Z");
            request.AddParameter("end", new DateTime(1995, 10, 3).ToString("s") + "Z");
            request.AddParameter("unmonitored", "false");
            var items = Calendar.Get<List<GameResource>>(request);

            items = items.Where(v => v.Id == game.Id).ToList();

            items.Should().BeEmpty();
        }

        [Test]
        public void should_be_able_to_get_unmonitored_games()
        {
            var game = EnsureGame(680, "Pulp Fiction", false);

            var request = Calendar.BuildRequest();
            request.AddParameter("start", new DateTime(1993, 10, 1).ToString("s") + "Z");
            request.AddParameter("end", new DateTime(1995, 10, 3).ToString("s") + "Z");
            request.AddParameter("unmonitored", "true");
            var items = Calendar.Get<List<GameResource>>(request);

            items = items.Where(v => v.Id == game.Id).ToList();

            items.Should().HaveCount(1);
            items.First().Title.Should().Be("Pulp Fiction");
        }
    }
}
