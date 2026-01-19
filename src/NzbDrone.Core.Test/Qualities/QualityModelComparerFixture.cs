using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityModelComparerFixture : CoreTest
    {
        public QualityModelComparer Subject { get; set; }

        [SetUp]
        public void Setup()
        {
        }

        private void GivenDefaultProfile()
        {
            Subject = new QualityModelComparer(new QualityProfile { Items = QualityFixture.GetDefaultQualities() });
        }

        private void GivenCustomProfile()
        {
            Subject = new QualityModelComparer(new QualityProfile { Items = QualityFixture.GetDefaultQualities(Quality.GOG, Quality.Repack) });
        }

        private void GivenGroupedProfile()
        {
            var profile = new QualityProfile
            {
                Items = new List<QualityProfileQualityItem>
                                      {
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = false,
                                              Quality = Quality.Scene
                                          },
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = false,
                                              Quality = Quality.Repack
                                          },
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = true,
                                              Items = new List<QualityProfileQualityItem>
                                                      {
                                                          new QualityProfileQualityItem
                                                          {
                                                              Allowed = true,
                                                              Quality = Quality.Steam
                                                          },
                                                          new QualityProfileQualityItem
                                                          {
                                                              Allowed = true,
                                                              Quality = Quality.Epic
                                                          }
                                                      }
                                          },
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = true,
                                              Quality = Quality.GOG
                                          }
                                      }
            };

            Subject = new QualityModelComparer(profile);
        }

        [Test]
        public void should_be_greater_when_first_quality_is_greater_than_second()
        {
            GivenDefaultProfile();

            // In default order: Scene < GOG < Repack < ISO < Retail
            var first = new QualityModel(Quality.Repack);
            var second = new QualityModel(Quality.Scene);

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_be_lesser_when_second_quality_is_greater_than_first()
        {
            GivenDefaultProfile();

            // In default order: Scene < GOG < Repack < ISO < Retail
            var first = new QualityModel(Quality.Scene);
            var second = new QualityModel(Quality.Repack);

            var compare = Subject.Compare(first, second);

            compare.Should().BeLessThan(0);
        }

        [Test]
        public void should_be_greater_when_first_quality_is_a_proper_for_the_same_quality()
        {
            GivenDefaultProfile();

            var first = new QualityModel(Quality.GOG, new Revision(version: 2));
            var second = new QualityModel(Quality.GOG, new Revision(version: 1));

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_be_greater_when_using_a_custom_profile()
        {
            GivenCustomProfile();

            var first = new QualityModel(Quality.Repack);
            var second = new QualityModel(Quality.GOG);

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_ignore_group_order_by_default()
        {
            GivenGroupedProfile();

            var first = new QualityModel(Quality.Steam);
            var second = new QualityModel(Quality.Epic);

            var compare = Subject.Compare(first, second);

            compare.Should().Be(0);
        }

        [Test]
        public void should_respect_group_order()
        {
            GivenGroupedProfile();

            var first = new QualityModel(Quality.Steam);
            var second = new QualityModel(Quality.Epic);

            var compare = Subject.Compare(first, second, true);

            compare.Should().BeLessThan(0);
        }
    }
}
