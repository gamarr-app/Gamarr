using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.GameImport.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Specifications
{
    [TestFixture]
    public class NotIgnoredExtensionSpecificationFixture : CoreTest<NotIgnoredExtensionSpecification>
    {
        private LocalGame _localGame;

        [SetUp]
        public void Setup()
        {
            _localGame = new LocalGame
            {
                Path = @"C:\Test\Unsorted\Kirby\Kirby.zip".AsOsAgnostic(),
                Size = 100
            };

            Mocker.GetMock<IIgnoredExtensionService>()
                  .Setup(s => s.IsIgnored(It.IsAny<string>()))
                  .Returns<string>(p => p != null && p.EndsWith(".zip"));
        }

        [Test]
        public void should_reject_a_file_with_an_ignored_extension()
        {
            var decision = Subject.IsSatisfiedBy(_localGame, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(ImportRejectionReason.IgnoredExtension);
        }

        [Test]
        public void should_accept_a_file_that_is_not_ignored()
        {
            _localGame.Path = @"C:\Test\Unsorted\Kirby\Kirby.xci".AsOsAgnostic();

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_a_release_folder_even_when_its_name_looks_ignored()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_localGame.Path))
                  .Returns(true);

            Subject.IsSatisfiedBy(_localGame, null).Accepted.Should().BeTrue();
        }
    }
}
