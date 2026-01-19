using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class EditionTagsFixture : CoreTest<FileNameBuilder>
    {
        private Game _game;
        private GameFile _gameFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _game = Builder<Game>
                .CreateNew()
                .With(m => m.Title = "Game Title")
                .Build();

            _gameFile = new GameFile { Quality = new QualityModel(), ReleaseGroup = "GamarrTest", Edition = "Uncut" };

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameGames = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                .Setup(v => v.All())
                .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_add_edition_tag()
        {
            _namingConfig.StandardGameFormat = "{Game Title} [{Edition Tags}]";

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be("Game Title [Uncut]");
        }

        [TestCase("{Game Title} {Edition Tags}")]
        [TestCase("{Game Title} {{Edition Tags}}")]
        [TestCase("{Game Title} {edition-{Edition Tags}}")]
        [TestCase("{Game Title} {{edition-{Edition Tags}}}")]
        public void should_conditional_hide_edition_tags(string gameFormat)
        {
            _gameFile.Edition = "";
            _namingConfig.StandardGameFormat = gameFormat;

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be("Game Title");
        }

        [TestCase("{Game Title} {{Edition Tags}}")]
        public void should_handle_edition_curly_brackets(string gameFormat)
        {
            _namingConfig.StandardGameFormat = gameFormat;

            Subject.BuildFileName(_game, _gameFile)
                .Should().Be("Game Title {Uncut}");
        }

        [TestCase("{Game Title} {{edition-{Edition Tags}}}")]
        public void should_handle_edition_tag_curly_brackets(string gameFormat)
        {
            _namingConfig.StandardGameFormat = gameFormat;

            Subject.BuildFileName(_game, _gameFile)
                .Should().Be("Game Title {{edition-Uncut}}");
        }

        [TestCase("1st anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [1st Anniversary Edition]")]
        [TestCase("2nd Anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [2nd Anniversary Edition]")]
        [TestCase("3rd anniversary Edition", "{Game Title} [{Edition Tags}]", "Game Title [3rd Anniversary Edition]")]
        [TestCase("4th anNiverSary eDitIOn", "{Game Title} [{Edition Tags}]", "Game Title [4th Anniversary Edition]")]
        [TestCase("5th anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [5th Anniversary Edition]")]
        [TestCase("6th anNiverSary EDITION", "{Game Title} [{Edition Tags}]", "Game Title [6th Anniversary Edition]")]
        [TestCase("7TH anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [7th Anniversary Edition]")]
        [TestCase("8Th anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [8th Anniversary Edition]")]
        [TestCase("9tH anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [9th Anniversary Edition]")]
        [TestCase("10th anniversary edition", "{Game Title} [{edition tags}]", "Game Title [10th anniversary edition]")]
        [TestCase("10TH anniversary edition", "{Game Title} [{edition tags}]", "Game Title [10th anniversary edition]")]
        [TestCase("10Th anniversary edition", "{Game Title} [{edition tags}]", "Game Title [10th anniversary edition]")]
        [TestCase("10th anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [10th Anniversary Edition]")]
        [TestCase("10TH anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [10th Anniversary Edition]")]
        [TestCase("10Th anniversary edition", "{Game Title} [{Edition Tags}]", "Game Title [10th Anniversary Edition]")]
        [TestCase("10th anniversary edition", "{Game Title} [{EDITION TAGS}]", "Game Title [10TH ANNIVERSARY EDITION]")]
        [TestCase("10TH anniversary edition", "{Game Title} [{EDITION TAGS}]", "Game Title [10TH ANNIVERSARY EDITION]")]
        [TestCase("10Th anniversary edition", "{Game Title} [{EDITION TAGS}]", "Game Title [10TH ANNIVERSARY EDITION]")]
        public void should_always_lowercase_ordinals(string edition, string gameFormat, string expected)
        {
            _gameFile.Edition = edition;
            _namingConfig.StandardGameFormat = gameFormat;

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be(expected);
        }

        [TestCase("imax", "{Game Title} [{edition tags}]", "Game Title [imax]")]
        [TestCase("IMAX", "{Game Title} [{edition tags}]", "Game Title [imax]")]
        [TestCase("Imax", "{Game Title} [{edition tags}]", "Game Title [imax]")]
        [TestCase("imax", "{Game Title} [{Edition Tags}]", "Game Title [IMAX]")]
        [TestCase("IMAX", "{Game Title} [{Edition Tags}]", "Game Title [IMAX]")]
        [TestCase("Imax", "{Game Title} [{Edition Tags}]", "Game Title [IMAX]")]
        [TestCase("imax", "{Game Title} [{EDITION TAGS}]", "Game Title [IMAX]")]
        [TestCase("IMAX", "{Game Title} [{EDITION TAGS}]", "Game Title [IMAX]")]
        [TestCase("Imax", "{Game Title} [{EDITION TAGS}]", "Game Title [IMAX]")]
        [TestCase("imax edition", "{Game Title} [{edition tags}]", "Game Title [imax edition]")]
        [TestCase("imax edition", "{Game Title} [{Edition Tags}]", "Game Title [IMAX Edition]")]
        [TestCase("Imax edition", "{Game Title} [{EDITION TAGS}]", "Game Title [IMAX EDITION]")]
        [TestCase("imax version", "{Game Title} [{Edition Tags}]", "Game Title [IMAX Version]")]
        [TestCase("IMAX-edition", "{Game Title} [{Edition Tags}]", "Game Title [IMAX-Edition]")]
        [TestCase("IMAX_edition", "{Game Title} [{Edition Tags}]", "Game Title [IMAX_Edition]")]
        [TestCase("IMAX.eDiTioN", "{Game Title} [{Edition Tags}]", "Game Title [IMAX.Edition]")]
        [TestCase("IMAX ed.", "{Game Title} [{edition tags}]", "Game Title [imax ed.]")]
        [TestCase("IMAX ed.", "{Game Title} [{Edition Tags}]", "Game Title [IMAX Ed.]")]
        [TestCase("Imax-ed.", "{Game Title} [{Edition Tags}]", "Game Title [IMAX-Ed.]")]
        [TestCase("imax.Ed", "{Game Title} [{Edition Tags}]", "Game Title [IMAX.Ed]")]
        [TestCase("Imax_ed", "{Game Title} [{Edition Tags}]", "Game Title [IMAX_Ed]")]
        [TestCase("3d", "{Game Title} [{edition tags}]", "Game Title [3d]")]
        [TestCase("3D", "{Game Title} [{edition tags}]", "Game Title [3d]")]
        [TestCase("3d", "{Game Title} [{Edition Tags}]", "Game Title [3D]")]
        [TestCase("3D", "{Game Title} [{Edition Tags}]", "Game Title [3D]")]
        [TestCase("3d", "{Game Title} [{EDITION TAGS}]", "Game Title [3D]")]
        [TestCase("3D", "{Game Title} [{EDITION TAGS}]", "Game Title [3D]")]
        [TestCase("hdr", "{Game Title} [{edition tags}]", "Game Title [hdr]")]
        [TestCase("HDR", "{Game Title} [{edition tags}]", "Game Title [hdr]")]
        [TestCase("Hdr", "{Game Title} [{edition tags}]", "Game Title [hdr]")]
        [TestCase("hdr", "{Game Title} [{Edition Tags}]", "Game Title [HDR]")]
        [TestCase("HDR", "{Game Title} [{Edition Tags}]", "Game Title [HDR]")]
        [TestCase("Hdr", "{Game Title} [{Edition Tags}]", "Game Title [HDR]")]
        [TestCase("hdr", "{Game Title} [{EDITION TAGS}]", "Game Title [HDR]")]
        [TestCase("HDR", "{Game Title} [{EDITION TAGS}]", "Game Title [HDR]")]
        [TestCase("Hdr", "{Game Title} [{EDITION TAGS}]", "Game Title [HDR]")]
        [TestCase("dv", "{Game Title} [{edition tags}]", "Game Title [dv]")]
        [TestCase("DV", "{Game Title} [{edition tags}]", "Game Title [dv]")]
        [TestCase("Dv", "{Game Title} [{edition tags}]", "Game Title [dv]")]
        [TestCase("dv", "{Game Title} [{Edition Tags}]", "Game Title [DV]")]
        [TestCase("DV", "{Game Title} [{Edition Tags}]", "Game Title [DV]")]
        [TestCase("Dv", "{Game Title} [{Edition Tags}]", "Game Title [DV]")]
        [TestCase("dv", "{Game Title} [{EDITION TAGS}]", "Game Title [DV]")]
        [TestCase("DV", "{Game Title} [{EDITION TAGS}]", "Game Title [DV]")]
        [TestCase("Dv", "{Game Title} [{EDITION TAGS}]", "Game Title [DV]")]
        [TestCase("sdr", "{Game Title} [{edition tags}]", "Game Title [sdr]")]
        [TestCase("SDR", "{Game Title} [{edition tags}]", "Game Title [sdr]")]
        [TestCase("Sdr", "{Game Title} [{edition tags}]", "Game Title [sdr]")]
        [TestCase("sdr", "{Game Title} [{Edition Tags}]", "Game Title [SDR]")]
        [TestCase("SDR", "{Game Title} [{Edition Tags}]", "Game Title [SDR]")]
        [TestCase("Sdr", "{Game Title} [{Edition Tags}]", "Game Title [SDR]")]
        [TestCase("sdr", "{Game Title} [{EDITION TAGS}]", "Game Title [SDR]")]
        [TestCase("SDR", "{Game Title} [{EDITION TAGS}]", "Game Title [SDR]")]
        [TestCase("Sdr", "{Game Title} [{EDITION TAGS}]", "Game Title [SDR]")]
        [TestCase("THEATRICAL", "{Game Title} [{Edition Tags}]", "Game Title [Theatrical]")]
        [TestCase("director's CUt", "{Game Title} [{Edition Tags}]", "Game Title [Director's Cut]")]
        public void should_always_uppercase_special_strings(string edition, string gameFormat, string expected)
        {
            _gameFile.Edition = edition;
            _namingConfig.StandardGameFormat = gameFormat;

            Subject.BuildFileName(_game, _gameFile)
                   .Should().Be(expected);
        }
    }
}
