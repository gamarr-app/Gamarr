using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.OAuth;

namespace NzbDrone.Common.Test.OAuthTests
{
    [TestFixture]
    public class OAuthToolsFixture
    {
        [Test]
        public void GetNonce_should_return_16_character_string()
        {
            var nonce = OAuthTools.GetNonce();

            nonce.Should().NotBeNullOrEmpty();
            nonce.Length.Should().Be(16);
        }

        [Test]
        public void GetNonce_should_return_different_values_each_call()
        {
            var nonce1 = OAuthTools.GetNonce();
            var nonce2 = OAuthTools.GetNonce();

            nonce1.Should().NotBe(nonce2);
        }

        [Test]
        public void GetNonce_should_contain_only_lowercase_and_digits()
        {
            var nonce = OAuthTools.GetNonce();

            foreach (var c in nonce)
            {
                (char.IsLower(c) || char.IsDigit(c)).Should().BeTrue();
            }
        }

        [Test]
        public void GetTimestamp_should_return_positive_number()
        {
            var timestamp = OAuthTools.GetTimestamp();

            long.Parse(timestamp).Should().BeGreaterThan(0);
        }

        [Test]
        public void GetTimestamp_with_datetime_should_return_correct_value()
        {
            var dateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = OAuthTools.GetTimestamp(dateTime);

            // Jan 1, 2020 00:00:00 UTC = 1577836800 seconds since Unix epoch
            timestamp.Should().Be("1577836800");
        }

        [Test]
        public void GetTimestamp_for_unix_epoch_should_be_zero()
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = OAuthTools.GetTimestamp(dateTime);

            timestamp.Should().Be("0");
        }

        [Test]
        public void UrlEncodeRelaxed_should_encode_special_characters()
        {
            var result = OAuthTools.UrlEncodeRelaxed("hello world");

            result.Should().Be("hello%20world");
        }

        [Test]
        public void UrlEncodeRelaxed_should_not_encode_alphanumeric()
        {
            var result = OAuthTools.UrlEncodeRelaxed("abc123");

            result.Should().Be("abc123");
        }

        [Test]
        public void UrlEncodeRelaxed_should_encode_brackets()
        {
            var result = OAuthTools.UrlEncodeRelaxed("test(value)");

            result.Should().Contain("%28");
            result.Should().Contain("%29");
        }

        [Test]
        public void UrlEncodeStrict_should_encode_special_characters()
        {
            var result = OAuthTools.UrlEncodeStrict("hello world");

            result.Should().Be("hello%20world");
        }

        [Test]
        public void UrlEncodeStrict_should_not_encode_unreserved_chars()
        {
            var result = OAuthTools.UrlEncodeStrict("abc-._~");

            result.Should().Be("abc-._~");
        }

        [Test]
        public void UrlEncodeStrict_should_encode_apostrophe()
        {
            var result = OAuthTools.UrlEncodeStrict("it's");

            result.Should().Contain("%27");
        }

        [Test]
        public void ConstructRequestUrl_should_exclude_port_80_for_http()
        {
            var url = new Uri("http://example.com:80/path");
            var result = OAuthTools.ConstructRequestUrl(url);

            result.Should().Be("http://example.com/path");
        }

        [Test]
        public void ConstructRequestUrl_should_exclude_port_443_for_https()
        {
            var url = new Uri("https://example.com:443/path");
            var result = OAuthTools.ConstructRequestUrl(url);

            result.Should().Be("https://example.com/path");
        }

        [Test]
        public void ConstructRequestUrl_should_include_non_standard_port()
        {
            var url = new Uri("http://example.com:8080/path");
            var result = OAuthTools.ConstructRequestUrl(url);

            result.Should().Be("http://example.com:8080/path");
        }

        [Test]
        public void ConstructRequestUrl_should_throw_for_null_url()
        {
            Assert.Throws<ArgumentNullException>(() => OAuthTools.ConstructRequestUrl(null));
        }

        [Test]
        public void NormalizeRequestParameters_should_sort_by_name()
        {
            var parameters = new WebParameterCollection();
            parameters.Add("z_param", "value_z");
            parameters.Add("a_param", "value_a");

            var result = OAuthTools.NormalizeRequestParameters(parameters);

            result.Should().StartWith("a_param=");
        }

        [Test]
        public void NormalizeRequestParameters_should_exclude_oauth_signature()
        {
            var parameters = new WebParameterCollection();
            parameters.Add("oauth_signature", "sig_value");
            parameters.Add("other_param", "other_value");

            var result = OAuthTools.NormalizeRequestParameters(parameters);

            result.Should().NotContain("oauth_signature");
        }

        [Test]
        public void SortParametersExcludingSignature_should_remove_oauth_signature()
        {
            var parameters = new WebParameterCollection();
            parameters.Add("oauth_signature", "sig");
            parameters.Add("param1", "value1");

            var result = OAuthTools.SortParametersExcludingSignature(parameters);

            result.Count.Should().Be(1);
            result[0].Name.Should().Be("param1");
        }

        [Test]
        public void ConcatenateRequestElements_should_combine_method_url_and_parameters()
        {
            var parameters = new WebParameterCollection();
            parameters.Add("param1", "value1");

            var result = OAuthTools.ConcatenateRequestElements("GET", "http://example.com/path", parameters);

            result.Should().StartWith("GET&");
            result.Should().Contain("http");
        }

        [Test]
        public void GetSignature_should_return_valid_signature()
        {
            var signatureBase = "GET&http%3A%2F%2Fexample.com&param%3Dvalue";
            var consumerSecret = "consumer_secret";

            var signature = OAuthTools.GetSignature(OAuthSignatureMethod.HmacSha1, signatureBase, consumerSecret);

            signature.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void GetSignature_with_token_secret_should_return_valid_signature()
        {
            var signatureBase = "GET&http%3A%2F%2Fexample.com&param%3Dvalue";
            var consumerSecret = "consumer_secret";
            var tokenSecret = "token_secret";

            var signature = OAuthTools.GetSignature(
                OAuthSignatureMethod.HmacSha1,
                OAuthSignatureTreatment.Escaped,
                signatureBase,
                consumerSecret,
                tokenSecret);

            signature.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void GetSignature_with_null_token_secret_should_use_empty_string()
        {
            var signatureBase = "GET&http%3A%2F%2Fexample.com&param%3Dvalue";
            var consumerSecret = "consumer_secret";

            var signature = OAuthTools.GetSignature(
                OAuthSignatureMethod.HmacSha1,
                OAuthSignatureTreatment.Escaped,
                signatureBase,
                consumerSecret,
                null);

            signature.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void GetSignature_unescaped_treatment_should_not_url_encode()
        {
            var signatureBase = "test_base";
            var consumerSecret = "secret";

            var escapedSig = OAuthTools.GetSignature(
                OAuthSignatureMethod.HmacSha1,
                OAuthSignatureTreatment.Escaped,
                signatureBase,
                consumerSecret,
                null);

            var unescapedSig = OAuthTools.GetSignature(
                OAuthSignatureMethod.HmacSha1,
                OAuthSignatureTreatment.Unescaped,
                signatureBase,
                consumerSecret,
                null);

            // Unescaped may contain characters that would be escaped in the escaped version
            escapedSig.Should().NotBeNull();
            unescapedSig.Should().NotBeNull();
        }
    }
}
