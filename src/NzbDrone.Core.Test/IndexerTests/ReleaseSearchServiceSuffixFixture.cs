using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class ReleaseSearchServiceSuffixFixture
    {
        private static readonly Regex UpdateSuffixRegex = new Regex(
            @"\s+-\s+(?:(?:v\d|patch|update|hotfix|build)\b.*|(?:\w+\s+)*update)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private string StripUpdateSuffix(string title)
        {
            return UpdateSuffixRegex.Replace(title, string.Empty);
        }

        [TestCase("Cyberpunk 2077 - v1.2.3", "Cyberpunk 2077")]
        [TestCase("Elden Ring - Patch 1.5.0", "Elden Ring")]
        [TestCase("Hollow Knight - Update 2", "Hollow Knight")]
        [TestCase("Stardew Valley - Hotfix", "Stardew Valley")]
        [TestCase("No Mans Sky - Thank You Update", "No Mans Sky")]
        [TestCase("Factorio - Build 12345", "Factorio")]
        [TestCase("Terraria - v2 Final", "Terraria")]
        public void should_strip_update_suffix(string input, string expected)
        {
            StripUpdateSuffix(input).Should().Be(expected);
        }

        [TestCase("Game Title", "Game Title")]
        [TestCase("Game Title - DLC Pack", "Game Title - DLC Pack")]
        [TestCase("Game Title - Deluxe Edition", "Game Title - Deluxe Edition")]
        public void should_not_strip_non_update_suffix(string input, string expected)
        {
            StripUpdateSuffix(input).Should().Be(expected);
        }
    }
}
