using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ReleaseContentTypeExtensionsFixture
    {
        [Test]
        [TestCase(ReleaseContentType.DlcOnly, true)]
        [TestCase(ReleaseContentType.UpdateOnly, true)]
        [TestCase(ReleaseContentType.SeasonPass, true)]
        [TestCase(ReleaseContentType.BaseGame, false)]
        [TestCase(ReleaseContentType.BaseGameWithAllDlc, false)]
        [TestCase(ReleaseContentType.Unknown, false)]

        // An expansion is add-on content applied on top of a base game, so it
        // requires one. It was false here until #212, which meant both prefix
        // paths in ParsingService bailed before the DLC name latitude in
        // IsAddonOfGameTitle could run and a named expansion release never
        // found its game
        [TestCase(ReleaseContentType.Expansion, true)]

        // A demo replaces the base game rather than adding to it (#207), so it
        // does not "require" one - DemoBuildSpecification rejects it instead
        [TestCase(ReleaseContentType.Demo, false)]
        public void RequiresBaseGame_should_return_correct_value(ReleaseContentType type, bool expected)
        {
            type.RequiresBaseGame().Should().Be(expected);
        }

        [Test]

        // The DLC content types that do NOT bring their own base game, i.e. the
        // ones that fill a DLC component slot of an existing game (#212)
        [TestCase(ReleaseContentType.DlcOnly, true)]
        [TestCase(ReleaseContentType.SeasonPass, true)]
        [TestCase(ReleaseContentType.Expansion, true)]

        // Carries the base game itself, so it fills the BASE slot
        [TestCase(ReleaseContentType.BaseGameWithAllDlc, false)]
        [TestCase(ReleaseContentType.BaseGame, false)]
        [TestCase(ReleaseContentType.Unknown, false)]

        // Fills an update slot, not a DLC slot
        [TestCase(ReleaseContentType.UpdateOnly, false)]
        [TestCase(ReleaseContentType.Demo, false)]
        public void OccupiesDlcSlot_should_return_correct_value(ReleaseContentType type, bool expected)
        {
            type.OccupiesDlcSlot().Should().Be(expected);
        }

        [Test]
        [TestCase(ReleaseContentType.BaseGame, true)]
        [TestCase(ReleaseContentType.BaseGameWithAllDlc, true)]
        [TestCase(ReleaseContentType.Unknown, true)]
        [TestCase(ReleaseContentType.DlcOnly, false)]
        [TestCase(ReleaseContentType.UpdateOnly, false)]
        [TestCase(ReleaseContentType.SeasonPass, false)]
        [TestCase(ReleaseContentType.Expansion, false)]
        [TestCase(ReleaseContentType.Demo, false)]
        public void IncludesBaseGame_should_return_correct_value(ReleaseContentType type, bool expected)
        {
            type.IncludesBaseGame().Should().Be(expected);
        }

        [Test]
        [TestCase(ReleaseContentType.BaseGameWithAllDlc, true)]
        [TestCase(ReleaseContentType.DlcOnly, true)]
        [TestCase(ReleaseContentType.SeasonPass, true)]
        [TestCase(ReleaseContentType.Expansion, true)]
        [TestCase(ReleaseContentType.BaseGame, false)]
        [TestCase(ReleaseContentType.UpdateOnly, false)]
        [TestCase(ReleaseContentType.Unknown, false)]
        [TestCase(ReleaseContentType.Demo, false)]
        public void IncludesDlc_should_return_correct_value(ReleaseContentType type, bool expected)
        {
            type.IncludesDlc().Should().Be(expected);
        }
    }
}
