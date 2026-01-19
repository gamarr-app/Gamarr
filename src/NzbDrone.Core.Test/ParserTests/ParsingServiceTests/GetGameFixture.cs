using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Games;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class GetGameFixture : CoreTest<ParsingService>
    {
        [Test]
        public void should_use_passed_in_title_when_it_cannot_be_parsed()
        {
            const string title = "30 Game";

            Subject.GetGame(title);

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.FindByTitle(title), Times.Once());
        }

        [Test]
        public void should_use_parsed_series_title()
        {
            const string title = "30.Game.2015.720p.hdtv";

            Subject.GetGame(title);

            Mocker.GetMock<IGameService>()
                .Verify(s => s.FindByTitle(Parser.Parser.ParseGameTitle(title, false).GameTitles, It.IsAny<int>(), It.IsAny<List<string>>(), null), Times.Once());
        }

        /*[Test]
        public void should_fallback_to_title_without_year_and_year_when_title_lookup_fails()
        {
            const string title = "Game.2004.S01E01.720p.hdtv";
            var parsedEpisodeInfo = Parser.Parser.ParseGameTitle(title,false,false);

            Subject.GetGame(title);

            Mocker.GetMock<IGameService>()
                  .Verify(s => s.FindByTitle(parsedEpisodeInfo.GameTitleInfo.TitleWithoutYear,
                                             parsedEpisodeInfo.GameTitleInfo.Year), Times.Once());
        }*/
    }
}
