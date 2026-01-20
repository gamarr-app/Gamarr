using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.Extensions;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class XmlExtensionsFixture
    {
        [Test]
        public void GetStringValue_should_return_string_element()
        {
            var xml = XElement.Parse("<value><string>test value</string></value>");

            xml.GetStringValue().Should().Be("test value");
        }

        [Test]
        public void GetStringValue_should_return_null_when_empty()
        {
            var xml = XElement.Parse("<value><string></string></value>");

            xml.GetStringValue().Should().BeNull();
        }

        [Test]
        public void GetIntValue_should_return_int_element()
        {
            var xml = XElement.Parse("<value><i4>42</i4></value>");

            xml.GetIntValue().Should().Be(42);
        }

        [Test]
        public void GetIntValue_should_return_zero_when_invalid()
        {
            var xml = XElement.Parse("<value><i4>invalid</i4></value>");

            xml.GetIntValue().Should().Be(0);
        }

        [Test]
        public void GetLongValue_should_return_long_element()
        {
            var xml = XElement.Parse("<value><i8>9876543210</i8></value>");

            xml.GetLongValue().Should().Be(9876543210L);
        }

        [Test]
        public void GetLongValue_should_return_zero_when_invalid()
        {
            var xml = XElement.Parse("<value><i8>invalid</i8></value>");

            xml.GetLongValue().Should().Be(0L);
        }

        [Test]
        public void ElementAsString_should_return_element_value()
        {
            var xml = XElement.Parse("<parent><child>value</child></parent>");

            xml.ElementAsString("child").Should().Be("value");
        }

        [Test]
        public void ElementAsString_should_return_null_when_missing()
        {
            var xml = XElement.Parse("<parent></parent>");

            xml.ElementAsString("child").Should().BeNull();
        }

        [Test]
        public void ElementAsString_should_trim_when_requested()
        {
            var xml = XElement.Parse("<parent><child>  value  </child></parent>");

            xml.ElementAsString("child", trim: true).Should().Be("value");
        }

        [Test]
        public void ElementAsInt_should_return_int_value()
        {
            var xml = XElement.Parse("<parent><child>123</child></parent>");

            xml.ElementAsInt("child").Should().Be(123);
        }

        [Test]
        public void ElementAsInt_should_return_zero_when_missing()
        {
            var xml = XElement.Parse("<parent></parent>");

            xml.ElementAsInt("child").Should().Be(0);
        }

        [Test]
        public void ElementAsLong_should_return_long_value()
        {
            var xml = XElement.Parse("<parent><child>1234567890123</child></parent>");

            xml.ElementAsLong("child").Should().Be(1234567890123L);
        }

        [Test]
        public void ElementAsLong_should_return_zero_when_missing()
        {
            var xml = XElement.Parse("<parent></parent>");

            xml.ElementAsLong("child").Should().Be(0L);
        }

        [Test]
        public void GetIntResponse_should_extract_from_method_response()
        {
            var xml = XDocument.Parse(@"
                <methodResponse>
                    <params>
                        <param>
                            <value><i4>42</i4></value>
                        </param>
                    </params>
                </methodResponse>");

            xml.GetIntResponse().Should().Be(42);
        }

        [Test]
        public void GetStringResponse_should_extract_from_method_response()
        {
            var xml = XDocument.Parse(@"
                <methodResponse>
                    <params>
                        <param>
                            <value><string>success</string></value>
                        </param>
                    </params>
                </methodResponse>");

            xml.GetStringResponse().Should().Be("success");
        }
    }
}
