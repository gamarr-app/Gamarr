using System.Net.Http;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;

namespace NzbDrone.Common.Test.Http
{
    [TestFixture]
    public class JsonRpcRequestBuilderFixture
    {
        [Test]
        public void constructor_should_set_post_method()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com");

            builder.Method.Should().Be(HttpMethod.Post);
        }

        [Test]
        public void constructor_should_initialize_empty_parameters()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com");

            builder.JsonParameters.Should().NotBeNull();
            builder.JsonParameters.Should().BeEmpty();
        }

        [Test]
        public void constructor_with_method_should_set_json_method()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com", "test.method", new object[] { "param1" });

            builder.JsonMethod.Should().Be("test.method");
            builder.JsonParameters.Should().HaveCount(1);
        }

        [Test]
        public void Call_should_return_new_builder_with_method()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com");

            var result = builder.Call("new.method", "arg1", "arg2");

            result.JsonMethod.Should().Be("new.method");
            result.JsonParameters.Should().HaveCount(2);
        }

        [Test]
        public void Call_should_not_modify_original_builder()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com");

            builder.Call("new.method", "arg1");

            builder.JsonMethod.Should().BeNull();
            builder.JsonParameters.Should().BeEmpty();
        }

        [Test]
        public void Clone_should_create_copy()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com", "test.method", new object[] { "param1" });

            var clone = builder.Clone() as JsonRpcRequestBuilder;

            clone.Should().NotBeNull();
            clone.JsonMethod.Should().Be("test.method");
            clone.JsonParameters.Should().HaveCount(1);
        }

        [Test]
        public void Clone_should_create_independent_parameters_list()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com", "test.method", new object[] { "param1" });

            var clone = builder.Clone() as JsonRpcRequestBuilder;
            clone.JsonParameters.Add("param2");

            builder.JsonParameters.Should().HaveCount(1);
            clone.JsonParameters.Should().HaveCount(2);
        }

        [Test]
        public void CreateNextId_should_return_8_character_string()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com");

            var id = builder.CreateNextId();

            id.Length.Should().Be(8);
        }

        [Test]
        public void CreateNextId_should_return_different_ids()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com");

            var id1 = builder.CreateNextId();
            var id2 = builder.CreateNextId();

            id1.Should().NotBe(id2);
        }

        [Test]
        public void Build_should_create_request_with_json_content()
        {
            var builder = new JsonRpcRequestBuilder("http://example.com", "test.method", new object[] { "param1" });

            var request = builder.Build();

            request.ContentData.Should().NotBeNull();
            request.Headers.ContentType.Should().Be("application/json");
        }

        [Test]
        public void Build_with_byte_array_parameter_should_convert_to_base64()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var builder = new JsonRpcRequestBuilder("http://example.com", "test.method", new object[] { bytes });

            var request = builder.Build();

            var content = System.Text.Encoding.UTF8.GetString(request.ContentData);
            content.Should().Contain("AQID"); // Base64 of [1,2,3]
        }

        [Test]
        public void Build_with_array_parameter_should_include_in_content()
        {
            var array = new[] { "a", "b", "c" };
            var builder = new JsonRpcRequestBuilder("http://example.com", "test.method", new object[] { array });

            var request = builder.Build();

            request.ContentSummary.Should().Contain("[...]");
        }

        [Test]
        public void JsonRpcHttpAccept_should_be_correct_value()
        {
            JsonRpcRequestBuilder.JsonRpcHttpAccept.Value.Should().Contain("application/json-rpc");
            JsonRpcRequestBuilder.JsonRpcHttpAccept.Value.Should().Contain("application/json");
        }

        [Test]
        public void JsonRpcContentType_should_be_application_json()
        {
            JsonRpcRequestBuilder.JsonRpcContentType.Should().Be("application/json");
        }
    }
}
