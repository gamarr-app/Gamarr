using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.GameImport;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.GameImport
{
    [TestFixture]

    // TODO: Add tests to ensure helpers for augmenters are correctly passed.
    public class ImportDecisionMakerFixture : CoreTest<ImportDecisionMaker>
    {
        private List<string> _videoFiles;
        private LocalGame _localGame;
        private Game _game;
        private QualityModel _quality;
        private ParsedGameInfo _fileInfo;

        private Mock<IImportDecisionEngineSpecification> _pass1;
        private Mock<IImportDecisionEngineSpecification> _pass2;
        private Mock<IImportDecisionEngineSpecification> _pass3;

        private Mock<IImportDecisionEngineSpecification> _fail1;
        private Mock<IImportDecisionEngineSpecification> _fail2;
        private Mock<IImportDecisionEngineSpecification> _fail3;

        [SetUp]
        public void Setup()
        {
            _pass1 = new Mock<IImportDecisionEngineSpecification>();
            _pass2 = new Mock<IImportDecisionEngineSpecification>();
            _pass3 = new Mock<IImportDecisionEngineSpecification>();

            _fail1 = new Mock<IImportDecisionEngineSpecification>();
            _fail2 = new Mock<IImportDecisionEngineSpecification>();
            _fail3 = new Mock<IImportDecisionEngineSpecification>();

            _pass1.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>())).Returns(ImportSpecDecision.Accept());
            _pass2.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>())).Returns(ImportSpecDecision.Accept());
            _pass3.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>())).Returns(ImportSpecDecision.Accept());

            _fail1.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>())).Returns(ImportSpecDecision.Reject(ImportRejectionReason.Unknown, "_fail1"));
            _fail2.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>())).Returns(ImportSpecDecision.Reject(ImportRejectionReason.Unknown, "_fail2"));
            _fail3.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>())).Returns(ImportSpecDecision.Reject(ImportRejectionReason.Unknown, "_fail3"));

            _game = Builder<Game>.CreateNew()
                                     .With(e => e.Path = @"C:\Test\Game".AsOsAgnostic())
                                     .With(e => e.QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                                     .Build();

            _quality = new QualityModel(Quality.Scene);

            _localGame = new LocalGame
            {
                Game = _game,
                Quality = _quality,
                Path = @"C:\Test\Unsorted\The.Game.2018.DVDRip.XviD-OSiTV.avi"
            };

            _fileInfo = new ParsedGameInfo
            {
                GameTitles = new List<string> { "The Game" },
                Year = 2018,
                Quality = _quality
            };

            Mocker.GetMock<IParsingService>()
                .Setup(c => c.ParseMinimalPathGameInfo(It.IsAny<string>()))
                .Returns(_fileInfo);

            GivenVideoFiles(new List<string> { @"C:\Test\Unsorted\The.Game.2018.DVDRip.XviD-OSiTV.avi".AsOsAgnostic() });
        }

        private void GivenSpecifications(params Mock<IImportDecisionEngineSpecification>[] mocks)
        {
            Mocker.SetConstant(mocks.Select(c => c.Object));
        }

        private void GivenVideoFiles(IEnumerable<string> videoFiles)
        {
            _videoFiles = videoFiles.ToList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.FilterExistingFiles(_videoFiles, It.IsAny<Game>()))
                  .Returns(_videoFiles);
        }

        private void GivenAugmentationSuccess()
        {
            Mocker.GetMock<IAggregationService>()
                  .Setup(s => s.Augment(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                  .Callback<LocalGame, DownloadClientItem>((localGame, downloadClientItem) =>
                  {
                      localGame.Game = _localGame.Game;
                  });
        }

        [Test]
        public void should_call_all_specifications()
        {
            var downloadClientItem = Builder<DownloadClientItem>.CreateNew().Build();
            GivenAugmentationSuccess();
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            Subject.GetImportDecisions(_videoFiles, _game, downloadClientItem, null, false, true);

            _fail1.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), downloadClientItem), Times.Once());
            _fail2.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), downloadClientItem), Times.Once());
            _fail3.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), downloadClientItem), Times.Once());
            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), downloadClientItem), Times.Once());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), downloadClientItem), Times.Once());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalGame>(), downloadClientItem), Times.Once());
        }

        [Test]
        public void should_return_rejected_if_single_specs_fail()
        {
            GivenSpecifications(_fail1);

            var result = Subject.GetImportDecisions(_videoFiles, _game);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_rejected_if_one_of_specs_fail()
        {
            GivenSpecifications(_pass1, _fail1, _pass2, _pass3);

            var result = Subject.GetImportDecisions(_videoFiles, _game);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_approved_if_all_specs_pass()
        {
            GivenAugmentationSuccess();
            GivenSpecifications(_pass1, _pass2, _pass3);

            var result = Subject.GetImportDecisions(_videoFiles, _game);

            result.Single().Approved.Should().BeTrue();
        }

        [Test]
        public void should_have_same_number_of_rejections_as_specs_that_failed()
        {
            GivenAugmentationSuccess();
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            var result = Subject.GetImportDecisions(_videoFiles, _game);
            result.Single().Rejections.Should().HaveCount(3);
        }

        [Test]
        public void should_not_blowup_the_process_due_to_failed_parse()
        {
            GivenSpecifications(_pass1);

            Mocker.GetMock<IAggregationService>()
                  .Setup(c => c.Augment(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                  .Throws<TestException>();

            _videoFiles = new List<string>
                {
                    "The.Game.2021.DVDRip.XviD-OSiTV",
                    "The.Game.2021.DVDRip.XviD-OSiTV",
                    "The.Game.2021.DVDRip.XviD-OSiTV"
                };

            GivenVideoFiles(_videoFiles);

            Subject.GetImportDecisions(_videoFiles, _game);

            Mocker.GetMock<IAggregationService>()
                  .Verify(c => c.Augment(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()), Times.Exactly(_videoFiles.Count));

            ExceptionVerification.ExpectedErrors(3);
        }

        [Test]
        public void should_call_parsing_service_with_filename_as_simpletitle()
        {
            GivenSpecifications(_pass1, _pass2, _pass3);

            Mocker.GetMock<IParsingService>()
                .Setup(c => c.ParseMinimalPathGameInfo(It.IsAny<string>()))
                .Returns<ParsedGameInfo>(null);

            var folderInfo = new ParsedGameInfo { SimpleReleaseTitle = "A Game Folder 2018", Quality = _quality };

            var result = Subject.GetImportDecisions(_videoFiles, _game, null, folderInfo, true);

            var fileNames = _videoFiles.Select(System.IO.Path.GetFileName);

            Mocker.GetMock<IAggregationService>()
                  .Setup(c => c.Augment(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                  .Throws<TestException>();
        }

        [Test]
        public void should_return_a_decision_when_exception_is_caught()
        {
            Mocker.GetMock<IAggregationService>()
                  .Setup(c => c.Augment(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                  .Throws<TestException>();

            _videoFiles = new List<string>
                {
                    "The.Game.2021.DVDRip.XviD-OSiTV"
                };

            GivenVideoFiles(_videoFiles);

            Subject.GetImportDecisions(_videoFiles, _game).Should().HaveCount(1);

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
