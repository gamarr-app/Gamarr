using System;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.GameTests
{
    [TestFixture]
    public class GameIsAvailableFixture : CoreTest
    {
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                     .Build();
        }

        private void SetGameProperties(DateTime? cinema, DateTime? physical, DateTime? digital, GameStatusType minimumAvailability)
        {
            _game.GameMetadata.Value.InDevelopment = cinema;
            _game.GameMetadata.Value.PhysicalRelease = physical;
            _game.GameMetadata.Value.DigitalRelease = digital;
            _game.MinimumAvailability = minimumAvailability;
        }

        // minAvail = TBA
        [TestCase(null, null, null, GameStatusType.TBA, true)]
        [TestCase("2000/01/01 21:10:42", null, null, GameStatusType.TBA, true)]
        [TestCase("2100/01/01 21:10:42", null, null, GameStatusType.TBA, true)]
        [TestCase(null, "2000/01/01 21:10:42", null, GameStatusType.TBA, true)]
        [TestCase(null, "2100/01/01 21:10:42", null, GameStatusType.TBA, true)]
        [TestCase(null, null, "2000/01/01 21:10:42", GameStatusType.TBA, true)]
        [TestCase(null, null, "2100/01/01 21:10:42", GameStatusType.TBA, true)]

        // minAvail = Announced
        [TestCase(null, null, null, GameStatusType.Announced, true)]
        [TestCase("2000/01/01 21:10:42", null, null, GameStatusType.Announced, true)]
        [TestCase("2100/01/01 21:10:42", null, null, GameStatusType.Announced, true)]
        [TestCase(null, "2000/01/01 21:10:42", null, GameStatusType.Announced, true)]
        [TestCase(null, "2100/01/01 21:10:42", null, GameStatusType.Announced, true)]
        [TestCase(null, null, "2000/01/01 21:10:42", GameStatusType.Announced, true)]
        [TestCase(null, null, "2100/01/01 21:10:42", GameStatusType.Announced, true)]

        // minAvail = InDevelopment
        // InDevelopment is known and in the past others are not known or in future
        [TestCase("2000/01/01 21:10:42", null, null, GameStatusType.InDevelopment, true)]
        [TestCase("2000/01/01 21:10:42", "2100/01/01 21:10:42", null, GameStatusType.InDevelopment, true)]
        [TestCase("2000/01/01 21:10:42", "2100/01/01 21:10:42", "2100/01/01 21:10:42", GameStatusType.InDevelopment, true)]
        [TestCase("2000/01/01 21:10:42", null, "2100/01/01 21:10:42", GameStatusType.InDevelopment, true)]

        // InDevelopment is known and in the future others are not known or in future
        [TestCase("2100/01/01 21:10:42", null, null, GameStatusType.InDevelopment, false)]
        [TestCase("2100/01/01 21:10:42", "2100/01/01 21:10:42", null, GameStatusType.InDevelopment, false)]
        [TestCase("2100/01/01 21:10:42", "2100/01/01 21:10:42", "2100/01/01 21:10:42", GameStatusType.InDevelopment, false)]
        [TestCase("2100/01/01 21:10:42", null, "2100/01/01 21:10:42", GameStatusType.InDevelopment, false)]

        // handle the cases where InDevelopment date is not known but Digital/Physical are and passed -- this refers to the issue being fixed along with these tests
        [TestCase(null, "2000/01/01 21:10:42", null, GameStatusType.InDevelopment, true)]
        [TestCase(null, "2000/01/01 21:10:42", "2000/01/01 21:10:42", GameStatusType.InDevelopment, true)]
        [TestCase(null, null, "2000/01/01 21:10:42", GameStatusType.InDevelopment, true)]

        // same as previous but digital/physical are in future
        [TestCase(null, "2100/01/01 21:10:42", null, GameStatusType.InDevelopment, false)]
        [TestCase(null, "2100/01/01 21:10:42", "2100/01/01 21:10:42", GameStatusType.InDevelopment, false)]
        [TestCase(null, null, "2100/01/01 21:10:42", GameStatusType.InDevelopment, false)]

        // no date values
        [TestCase(null, null, null, GameStatusType.InDevelopment, false)]

        // minAvail = Released
        [TestCase(null, null, null, GameStatusType.Released, false)]
        [TestCase("2000/01/01 21:10:42", null, null, GameStatusType.Released, true)]
        [TestCase("2100/01/01 21:10:42", null, null, GameStatusType.Released, false)]
        [TestCase(null, "2000/01/01 21:10:42", null, GameStatusType.Released, true)]
        [TestCase(null, "2100/01/01 21:10:42", null, GameStatusType.Released, false)]
        [TestCase(null, null, "2000/01/01 21:10:42", GameStatusType.Released, true)]
        [TestCase(null, null, "2100/01/01 21:10:42", GameStatusType.Released, false)]
        public void should_have_correct_availability(DateTime? cinema, DateTime? physical, DateTime? digital, GameStatusType minimumAvailability, bool result)
        {
            SetGameProperties(cinema, physical, digital, minimumAvailability);
            _game.IsAvailable().Should().Be(result);
        }

        [Test]
        public void positive_delay_should_effect_availability()
        {
            SetGameProperties(null, DateTime.Now.AddDays(-5), null, GameStatusType.Released);
            _game.IsAvailable().Should().BeTrue();
            _game.IsAvailable(6).Should().BeFalse();
        }

        [Test]
        public void negative_delay_should_effect_availability()
        {
            SetGameProperties(null, DateTime.Now.AddDays(5), null, GameStatusType.Released);
            _game.IsAvailable().Should().BeFalse();
            _game.IsAvailable(-6).Should().BeTrue();
        }

        [Test]
        public void minimum_availability_released_no_date_but_ninety_days_after_cinemas()
        {
            SetGameProperties(DateTime.Now.AddDays(-91), null, null, GameStatusType.Released);
            _game.IsAvailable().Should().BeTrue();
            SetGameProperties(DateTime.Now.AddDays(-89), null, null, GameStatusType.Released);
            _game.IsAvailable().Should().BeFalse();
            SetGameProperties(DateTime.Now.AddDays(-89), DateTime.Now.AddDays(-40), null, GameStatusType.Released);
            _game.IsAvailable().Should().BeTrue();
            SetGameProperties(DateTime.Now.AddDays(-91), DateTime.Now.AddDays(40), null, GameStatusType.Released);
            _game.IsAvailable().Should().BeFalse();
        }
    }
}
