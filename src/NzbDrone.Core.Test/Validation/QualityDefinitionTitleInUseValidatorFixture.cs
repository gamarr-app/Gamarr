using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Test.Validation
{
    [TestFixture]
    public class QualityDefinitionTitleInUseValidatorFixture : CoreTest<QualityDefinitionTitleInUseValidator>
    {
        private void GivenDefinitions(params QualityDefinition[] definitions)
        {
            Mocker.GetMock<IQualityDefinitionService>()
                  .Setup(s => s.All())
                  .Returns(new List<QualityDefinition>(definitions));
        }

        [SetUp]
        public void Setup()
        {
            GivenDefinitions(new QualityDefinition { Id = 1, Title = "GOG" },
                             new QualityDefinition { Id = 2, Title = "Repack" },
                             new QualityDefinition { Id = 3, Title = "Scene" });
        }

        [Test]
        public void should_be_valid_when_title_is_not_used()
        {
            Subject.Validate(0, "Retail").Should().BeTrue();
        }

        [Test]
        public void should_be_invalid_when_renaming_onto_another_definitions_title()
        {
            Subject.Validate(1, "Repack").Should().BeFalse();
        }

        [Test]
        public void should_be_invalid_when_existing_title_differs_only_by_case()
        {
            Subject.Validate(1, "rePack").Should().BeFalse();
        }

        [Test]
        public void should_be_valid_when_definition_keeps_its_own_title()
        {
            Subject.Validate(1, "GOG").Should().BeTrue();
        }

        [Test]
        public void should_be_valid_when_title_is_empty()
        {
            Subject.Validate(1, "").Should().BeTrue();
        }

        [Test]
        public void should_accept_a_batch_that_only_reorders_existing_titles()
        {
            var batch = new List<QualityDefinition>
            {
                new QualityDefinition { Id = 1, Title = "Repack" },
                new QualityDefinition { Id = 2, Title = "GOG" }
            };

            Subject.ValidateBatch(batch).Should().BeTrue();
        }

        [Test]
        public void should_reject_a_batch_containing_two_identical_titles()
        {
            var batch = new List<QualityDefinition>
            {
                new QualityDefinition { Id = 1, Title = "Repack" },
                new QualityDefinition { Id = 2, Title = "Repack" }
            };

            Subject.ValidateBatch(batch).Should().BeFalse();
        }

        [Test]
        public void should_reject_a_batch_whose_titles_differ_only_by_case()
        {
            var batch = new List<QualityDefinition>
            {
                new QualityDefinition { Id = 1, Title = "Repack" },
                new QualityDefinition { Id = 2, Title = "rePack" }
            };

            Subject.ValidateBatch(batch).Should().BeFalse();
        }

        [Test]
        public void should_reject_a_batch_colliding_with_a_definition_it_does_not_replace()
        {
            var batch = new List<QualityDefinition>
            {
                new QualityDefinition { Id = 1, Title = "Scene" }
            };

            Subject.ValidateBatch(batch).Should().BeFalse();
        }

        [Test]
        public void should_accept_a_batch_with_all_new_titles()
        {
            var batch = new List<QualityDefinition>
            {
                new QualityDefinition { Id = 1, Title = "Retail" },
                new QualityDefinition { Id = 2, Title = "Portable" }
            };

            Subject.ValidateBatch(batch).Should().BeTrue();
        }
    }
}
