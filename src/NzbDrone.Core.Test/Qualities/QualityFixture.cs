using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityFixture : CoreTest
    {
        public static object[] FromIntCases =
                {
                        new object[] { 0, Quality.Unknown },
                        new object[] { 1, Quality.Scene },
                        new object[] { 2, Quality.SceneCracked },
                        new object[] { 3, Quality.GOG },
                        new object[] { 4, Quality.Steam },
                        new object[] { 5, Quality.Epic },
                        new object[] { 6, Quality.Origin },
                        new object[] { 7, Quality.Uplay },
                        new object[] { 8, Quality.Repack },
                        new object[] { 9, Quality.RepackAllDLC },
                        new object[] { 10, Quality.ISO },
                        new object[] { 11, Quality.Retail },
                        new object[] { 12, Quality.Portable },
                        new object[] { 13, Quality.Preload },
                        new object[] { 14, Quality.UpdateOnly },
                        new object[] { 15, Quality.MultiLang },
                };

        public static object[] ToIntCases =
                {
                        new object[] { Quality.Unknown, 0 },
                        new object[] { Quality.Scene, 1 },
                        new object[] { Quality.SceneCracked, 2 },
                        new object[] { Quality.GOG, 3 },
                        new object[] { Quality.Steam, 4 },
                        new object[] { Quality.Epic, 5 },
                        new object[] { Quality.Origin, 6 },
                        new object[] { Quality.Uplay, 7 },
                        new object[] { Quality.Repack, 8 },
                        new object[] { Quality.RepackAllDLC, 9 },
                        new object[] { Quality.ISO, 10 },
                        new object[] { Quality.Retail, 11 },
                        new object[] { Quality.Portable, 12 },
                        new object[] { Quality.Preload, 13 },
                        new object[] { Quality.UpdateOnly, 14 },
                        new object[] { Quality.MultiLang, 15 },
                };

        [Test]
        [TestCaseSource("FromIntCases")]
        public void should_be_able_to_convert_int_to_qualityTypes(int source, Quality expected)
        {
            var quality = (Quality)source;
            quality.Should().Be(expected);
        }

        [Test]
        [TestCaseSource("ToIntCases")]
        public void should_be_able_to_convert_qualityTypes_to_int(Quality source, int expected)
        {
            var i = (int)source;
            i.Should().Be(expected);
        }

        public static List<QualityProfileQualityItem> GetDefaultQualities(params Quality[] allowed)
        {
            var qualities = new List<Quality>
            {
                Quality.Unknown,
                Quality.Scene,
                Quality.SceneCracked,
                Quality.GOG,
                Quality.Steam,
                Quality.Epic,
                Quality.Origin,
                Quality.Uplay,
                Quality.Repack,
                Quality.RepackAllDLC,
                Quality.ISO,
                Quality.Retail,
                Quality.Portable,
                Quality.Preload,
                Quality.UpdateOnly,
                Quality.MultiLang
            };

            if (allowed.Length == 0)
            {
                allowed = qualities.ToArray();
            }

            var items = qualities
                .Except(allowed)
                .Concat(allowed)
                .Select(v => new QualityProfileQualityItem
                {
                    Quality = v,
                    Allowed = allowed.Contains(v)
                }).ToList();

            return items;
        }
    }
}
