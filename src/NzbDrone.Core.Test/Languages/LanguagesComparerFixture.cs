using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Test.Languages
{
    [TestFixture]
    public class LanguagesComparerFixture
    {
        private LanguagesComparer _comparer;

        [SetUp]
        public void Setup()
        {
            _comparer = new LanguagesComparer();
        }

        [Test]
        public void Compare_should_return_zero_for_two_empty_lists()
        {
            var x = new List<Language>();
            var y = new List<Language>();

            _comparer.Compare(x, y).Should().Be(0);
        }

        [Test]
        public void Compare_should_return_positive_when_x_empty_y_has_items()
        {
            var x = new List<Language>();
            var y = new List<Language> { Language.English };

            _comparer.Compare(x, y).Should().Be(1);
        }

        [Test]
        public void Compare_should_return_negative_when_x_has_items_y_empty()
        {
            var x = new List<Language> { Language.English };
            var y = new List<Language>();

            _comparer.Compare(x, y).Should().Be(-1);
        }

        [Test]
        public void Compare_should_return_positive_when_x_has_more_items_than_y()
        {
            var x = new List<Language> { Language.English, Language.French, Language.German };
            var y = new List<Language> { Language.English, Language.French };

            _comparer.Compare(x, y).Should().Be(1);
        }

        [Test]
        public void Compare_should_return_negative_when_x_has_fewer_items_than_y()
        {
            var x = new List<Language> { Language.English, Language.French };
            var y = new List<Language> { Language.English, Language.French, Language.German };

            _comparer.Compare(x, y).Should().Be(-1);
        }

        [Test]
        public void Compare_should_return_positive_when_x_multi_y_single()
        {
            var x = new List<Language> { Language.English, Language.French };
            var y = new List<Language> { Language.English };

            _comparer.Compare(x, y).Should().Be(1);
        }

        [Test]
        public void Compare_should_return_negative_when_x_single_y_multi()
        {
            var x = new List<Language> { Language.English };
            var y = new List<Language> { Language.English, Language.French };

            _comparer.Compare(x, y).Should().Be(-1);
        }

        [Test]
        public void Compare_should_compare_by_name_when_both_single()
        {
            var x = new List<Language> { Language.English };
            var y = new List<Language> { Language.French };

            // English comes before French alphabetically
            _comparer.Compare(x, y).Should().BeLessThan(0);
        }

        [Test]
        public void Compare_should_return_zero_when_both_same_single()
        {
            var x = new List<Language> { Language.English };
            var y = new List<Language> { Language.English };

            _comparer.Compare(x, y).Should().Be(0);
        }

        [Test]
        public void Compare_should_return_zero_when_both_multi_same_count()
        {
            var x = new List<Language> { Language.English, Language.French };
            var y = new List<Language> { Language.German, Language.Spanish };

            _comparer.Compare(x, y).Should().Be(0);
        }
    }
}
