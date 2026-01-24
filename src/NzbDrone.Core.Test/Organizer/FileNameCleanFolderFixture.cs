using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Test.Organizer
{
    [TestFixture]
    public class FileNameCleanFolderFixture
    {
        [TestCase("Game Title ()", "Game Title")]
        [TestCase("Game Title (2023)", "Game Title (2023)")]
        [TestCase("Game Title []", "Game Title")]
        [TestCase("Game Title {}", "Game Title")]
        [TestCase("Game Title ( )", "Game Title")]
        [TestCase("Game Title [ ]", "Game Title")]
        [TestCase("Game Title { }", "Game Title")]
        [TestCase("Game Title (2023) ()", "Game Title (2023)")]
        public void should_clean_folder_name(string input, string expected)
        {
            FileNameBuilder.CleanFolderName(input).Should().Be(expected);
        }
    }
}
