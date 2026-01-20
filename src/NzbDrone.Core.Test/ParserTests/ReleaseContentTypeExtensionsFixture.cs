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
        [TestCase(ReleaseContentType.Expansion, false)]
        public void RequiresBaseGame_should_return_correct_value(ReleaseContentType type, bool expected)
        {
            type.RequiresBaseGame().Should().Be(expected);
        }

        [Test]
        [TestCase(ReleaseContentType.BaseGame, true)]
        [TestCase(ReleaseContentType.BaseGameWithAllDlc, true)]
        [TestCase(ReleaseContentType.Unknown, true)]
        [TestCase(ReleaseContentType.DlcOnly, false)]
        [TestCase(ReleaseContentType.UpdateOnly, false)]
        [TestCase(ReleaseContentType.SeasonPass, false)]
        [TestCase(ReleaseContentType.Expansion, false)]
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
        public void IncludesDlc_should_return_correct_value(ReleaseContentType type, bool expected)
        {
            type.IncludesDlc().Should().Be(expected);
        }
    }
}
