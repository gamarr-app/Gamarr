using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class SceneCheckerFixture
    {
        [TestCase("Some.Game.2..2013.German.DTS.DL.720p.BluRay.x264-Pate")]
        public void should_return_true_for_scene_names(string title)
        {
            SceneChecker.IsSceneTitle(title).Should().BeTrue();
        }

        [TestCase("S08E05 - Virtual In-Stanity [WEBDL-720p]")]
        [TestCase("S08E05 - Virtual In-Stanity.With.Dots [WEBDL-720p]")]
        [TestCase("Something")]
        [TestCase("86de66b7ef385e2fa56a3e41b98481ea1658bfab")]
        [TestCase("Some.Game.2..2013.German.DTS.DL.720p.BluRay.x264-", Description = "no group")]
        [TestCase("Some.Game.2..2013.German.DTS.DL-Pate", Description = "no quality")]
        [TestCase("2013.German.DTS.DL.BluRay.x264-Pate", Description = "no gametitle")]
        public void should_return_false_for_non_scene_names(string title)
        {
            SceneChecker.IsSceneTitle(title).Should().BeFalse();
        }

        [TestCase("Some.Game.2..2013.German.DTS.DL.720p.BluRay.x264-Pate_", "Some.Game.2..2013.German.DTS.DL.720p.BluRay.x264-Pate", Description = "underscore at the end")]
        [TestCase("Some.Game.2..2013.German.DTS.DL.720p.BluRay.x264-Pate.mkv", "Some.Game.2..2013.German.DTS.DL.720p.BluRay.x264-Pate", Description = "file extension")]
        [TestCase("Some.Game.2..2013.German.DTS.DL.720p.BluRay.x264-Pate.nzb", "Some.Game.2..2013.German.DTS.DL.720p.BluRay.x264-Pate", Description = "file extension")]
        [TestCase("Some.Game.2..2013.German.DTS.DL.【720p】.BluRay.x264-Pate.nzb", "Some.Game.2..2013.German.DTS.DL.[720p].BluRay.x264-Pate", Description = "brackets")]
        public void should_correctly_parse_scene_names(string title, string result)
        {
            SceneChecker.GetSceneTitle(title).Should().Be(result);
        }
    }
}
