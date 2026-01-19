using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators;
using NzbDrone.Core.MediaFiles.GameImport.Aggregation.Aggregators.Augmenters.Language;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.GameImport.Aggregation.Aggregators
{
    [TestFixture]
    public class AggregateLanguageFixture : CoreTest<AggregateLanguage>
    {
        private LocalGame _localGame;
        private Game _game;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>.CreateNew()
                                   .With(m => m.GameMetadata.Value.OriginalLanguage = Language.English)
                                   .Build();

            _localGame = Builder<LocalGame>.CreateNew()
                                                 .With(l => l.DownloadClientGameInfo = null)
                                                 .With(l => l.FolderGameInfo = null)
                                                 .With(l => l.FileGameInfo = null)
                                                 .With(l => l.Game = _game)
                                                 .Build();
        }

        private void GivenAugmenters(List<Language> fileNameLanguages, List<Language> folderNameLanguages, List<Language> clientLanguages, List<Language> mediaInfoLanguages)
        {
            var fileNameAugmenter = new Mock<IAugmentLanguage>();
            var folderNameAugmenter = new Mock<IAugmentLanguage>();
            var clientInfoAugmenter = new Mock<IAugmentLanguage>();
            var mediaInfoAugmenter = new Mock<IAugmentLanguage>();

            fileNameAugmenter.Setup(s => s.AugmentLanguage(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                   .Returns(new AugmentLanguageResult(fileNameLanguages, Confidence.Filename));

            folderNameAugmenter.Setup(s => s.AugmentLanguage(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                   .Returns(new AugmentLanguageResult(folderNameLanguages, Confidence.Foldername));

            clientInfoAugmenter.Setup(s => s.AugmentLanguage(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                   .Returns(new AugmentLanguageResult(clientLanguages, Confidence.DownloadClientItem));

            mediaInfoAugmenter.Setup(s => s.AugmentLanguage(It.IsAny<LocalGame>(), It.IsAny<DownloadClientItem>()))
                   .Returns(new AugmentLanguageResult(mediaInfoLanguages, Confidence.MediaInfo));

            var mocks = new List<Mock<IAugmentLanguage>> { fileNameAugmenter, folderNameAugmenter, clientInfoAugmenter, mediaInfoAugmenter };

            Mocker.SetConstant<IEnumerable<IAugmentLanguage>>(mocks.Select(c => c.Object));
        }

        private ParsedGameInfo GetParsedGameInfo(List<Language> languages)
        {
            return new ParsedGameInfo
            {
                Languages =  languages
            };
        }

        [Test]
        public void should_return_default_if_no_info_is_known()
        {
            var result = Subject.Aggregate(_localGame, null);

            result.Languages.Should().Contain(_game.GameMetadata.Value.OriginalLanguage);
        }

        [Test]
        public void should_return_file_language_when_only_file_info_is_known()
        {
            GivenAugmenters(new List<Language> { Language.French },
                            null,
                            null,
                            null);

            Subject.Aggregate(_localGame, null).Languages.Should().Equal(new List<Language> { Language.French });
        }

        [Test]
        public void should_return_folder_language_when_folder_info_is_known()
        {
            GivenAugmenters(new List<Language> { Language.French },
                            new List<Language> { Language.German },
                            null,
                            null);

            var aggregation = Subject.Aggregate(_localGame, null);

            aggregation.Languages.Should().Equal(new List<Language> { Language.German });
        }

        [Test]
        public void should_return_download_client_item_language_when_download_client_item_info_is_known()
        {
            GivenAugmenters(new List<Language> { Language.French },
                            new List<Language> { Language.German },
                            new List<Language> { Language.Spanish },
                            null);

            Subject.Aggregate(_localGame, null).Languages.Should().Equal(new List<Language> { Language.Spanish });
        }

        [Test]
        public void should_return_multi_language()
        {
            GivenAugmenters(new List<Language> { Language.Unknown },
                            new List<Language> { Language.French, Language.German },
                            new List<Language> { Language.Unknown },
                            null);

            Subject.Aggregate(_localGame, null).Languages.Should().Equal(new List<Language> { Language.French, Language.German });
        }

        [Test]
        public void should_use_mediainfo_over_others()
        {
            GivenAugmenters(new List<Language> { Language.Unknown },
                            new List<Language> { Language.French, Language.German },
                            new List<Language> { Language.Unknown },
                            new List<Language> { Language.Japanese, Language.English });

            Subject.Aggregate(_localGame, null).Languages.Should().Equal(new List<Language> { Language.Japanese, Language.English });
        }

        [Test]
        public void should_not_use_mediainfo_if_unknown()
        {
            GivenAugmenters(new List<Language> { Language.Unknown },
                            new List<Language> { Language.French, Language.German },
                            new List<Language> { Language.Unknown },
                            new List<Language> { Language.Unknown });

            Subject.Aggregate(_localGame, null).Languages.Should().Equal(new List<Language> { Language.French, Language.German });
        }
    }
}
