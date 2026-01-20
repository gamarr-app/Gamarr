using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class PagingSpecFixture : CoreTest
    {
        [Test]
        public void should_initialize_with_empty_filter_expressions()
        {
            var spec = new PagingSpec<Game>();

            spec.FilterExpressions.Should().NotBeNull();
            spec.FilterExpressions.Should().BeEmpty();
        }

        [Test]
        public void should_set_page_properties()
        {
            var spec = new PagingSpec<Game>
            {
                Page = 1,
                PageSize = 10,
                SortKey = "Title",
                SortDirection = SortDirection.Ascending
            };

            spec.Page.Should().Be(1);
            spec.PageSize.Should().Be(10);
            spec.SortKey.Should().Be("Title");
            spec.SortDirection.Should().Be(SortDirection.Ascending);
        }

        [Test]
        public void should_set_total_records()
        {
            var spec = new PagingSpec<Game>
            {
                TotalRecords = 100
            };

            spec.TotalRecords.Should().Be(100);
        }

        [Test]
        public void should_set_records()
        {
            var games = new List<Game>
            {
                new Game { Id = 1, Title = "Game 1" },
                new Game { Id = 2, Title = "Game 2" }
            };

            var spec = new PagingSpec<Game>
            {
                Records = games
            };

            spec.Records.Should().HaveCount(2);
        }

        [Test]
        public void should_add_filter_expression()
        {
            var spec = new PagingSpec<Game>();
            spec.FilterExpressions.Add(g => g.Monitored == true);

            spec.FilterExpressions.Should().HaveCount(1);
        }

        [Test]
        public void should_support_descending_sort()
        {
            var spec = new PagingSpec<Game>
            {
                SortDirection = SortDirection.Descending
            };

            spec.SortDirection.Should().Be(SortDirection.Descending);
        }

        [Test]
        public void should_support_default_sort()
        {
            var spec = new PagingSpec<Game>
            {
                SortDirection = SortDirection.Default
            };

            spec.SortDirection.Should().Be(SortDirection.Default);
        }
    }
}
