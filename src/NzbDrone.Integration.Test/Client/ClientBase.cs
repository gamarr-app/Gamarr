using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using NLog;
using NzbDrone.Common.Serializer;
using Gamarr.Http;
using Gamarr.Http.REST;
using RestSharp;

namespace NzbDrone.Integration.Test.Client
{
    public class ClientBase
    {
        protected readonly RestClient _restClient;
        protected readonly string _resource;
        protected readonly string _apiKey;
        protected readonly Logger _logger;

        public ClientBase(RestClient restClient, string apiKey, string resource)
        {
            _restClient = restClient;
            _resource = resource;
            _apiKey = apiKey;

            _logger = LogManager.GetLogger("REST");
        }

        public RestRequest BuildRequest(string command = "")
        {
            var request = new RestRequest(_resource + "/" + command.Trim('/'));

            request.AddHeader("Authorization", _apiKey);
            request.AddHeader("X-Api-Key", _apiKey);

            return request;
        }

        public string Execute(RestRequest request, HttpStatusCode statusCode)
        {
            _logger.Info("{0}: {1}", request.Method, _restClient.BuildUri(request));

            var response = _restClient.Execute(request);
            _logger.Info("Response: {0}", response.Content);

            // In RestSharp v107+, ErrorException is set for HTTP error status codes too.
            // Only throw for transport/network errors, not HTTP status code errors.
            if (response.ErrorException != null && response.ErrorException is not System.Net.Http.HttpRequestException)
            {
                throw response.ErrorException;
            }

            AssertDisableCache(response);

            response.ErrorMessage.Should().BeNullOrWhiteSpace();

            response.StatusCode.Should().Be(statusCode, response.Content ?? string.Empty);

            return response.Content;
        }

        public T Execute<T>(RestRequest request, HttpStatusCode statusCode)
            where T : class, new()
        {
            var content = Execute(request, statusCode);

            return Json.Deserialize<T>(content);
        }

        private static void AssertDisableCache(RestResponse response)
        {
            // cache control header gets reordered on net core
            // In RestSharp v107+, headers are split between Headers (response) and ContentHeaders (content)
            var allHeaders = (response.Headers ?? Enumerable.Empty<HeaderParameter>())
                .Concat(response.ContentHeaders ?? Enumerable.Empty<HeaderParameter>())
                .ToList();
            ((string)allHeaders.SingleOrDefault(c => c.Name == "Cache-Control")?.Value ?? string.Empty).Split(',').Select(x => x.Trim())
                .Should().BeEquivalentTo("no-store, no-cache".Split(',').Select(x => x.Trim()));
            allHeaders.Single(c => c.Name == "Pragma").Value.Should().Be("no-cache");
            allHeaders.Single(c => c.Name == "Expires").Value.Should().Be("-1");
        }
    }

    public class ClientBase<TResource> : ClientBase
        where TResource : RestResource, new()
    {
        public ClientBase(RestClient restClient, string apiKey, string resource = null)
            : base(restClient, apiKey, resource ?? new TResource().ResourceName)
        {
        }

        public List<TResource> All(Dictionary<string, object> queryParams = null)
        {
            var request = BuildRequest();

            if (queryParams != null)
            {
                foreach (var param in queryParams)
                {
                    request.AddQueryParameter(param.Key, param.Value.ToString());
                }
            }

            return Get<List<TResource>>(request);
        }

        public PagingResource<TResource> GetPaged(int pageNumber, int pageSize, string sortKey, string sortDir, string filterKey = null, object filterValue = null)
        {
            var request = BuildRequest();
            request.AddQueryParameter("page", pageNumber.ToString());
            request.AddQueryParameter("pageSize", pageSize.ToString());
            request.AddQueryParameter("sortKey", sortKey);
            request.AddQueryParameter("sortDir", sortDir);

            if (filterKey != null && filterValue != null)
            {
                request.AddQueryParameter(filterKey, filterValue.ToString());
            }

            return Get<PagingResource<TResource>>(request);
        }

        public TResource Post(TResource body, HttpStatusCode statusCode = HttpStatusCode.Created)
        {
            var request = BuildRequest();
            request.AddJsonBody(body);
            return Post<TResource>(request, statusCode);
        }

        public TResource Put(TResource body, HttpStatusCode statusCode = HttpStatusCode.Accepted)
        {
            var request = BuildRequest(body.Id.ToString());
            request.AddJsonBody(body);
            return Put<TResource>(request, statusCode);
        }

        public TResource Get(int id, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var request = BuildRequest(id.ToString());
            return Get<TResource>(request, statusCode);
        }

        public TResource GetSingle(HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var request = BuildRequest();
            return Get<TResource>(request, statusCode);
        }

        public void Delete(int id)
        {
            var request = BuildRequest(id.ToString());
            Delete(request);
        }

        public object InvalidGet(int id, HttpStatusCode statusCode = HttpStatusCode.NotFound)
        {
            var request = BuildRequest(id.ToString());
            return Get<object>(request, statusCode);
        }

        public object InvalidPost(TResource body, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var request = BuildRequest();
            request.AddJsonBody(body);
            return Post<object>(request, statusCode);
        }

        public object InvalidPut(TResource body, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var request = BuildRequest();
            request.AddJsonBody(body);
            return Put<object>(request, statusCode);
        }

        public T Get<T>(RestRequest request, HttpStatusCode statusCode = HttpStatusCode.OK)
            where T : class, new()
        {
            request.Method = Method.Get;
            return Execute<T>(request, statusCode);
        }

        public T Post<T>(RestRequest request, HttpStatusCode statusCode = HttpStatusCode.Created)
            where T : class, new()
        {
            request.Method = Method.Post;
            return Execute<T>(request, statusCode);
        }

        public T Put<T>(RestRequest request, HttpStatusCode statusCode = HttpStatusCode.Accepted)
            where T : class, new()
        {
            request.Method = Method.Put;
            return Execute<T>(request, statusCode);
        }

        public void Delete(RestRequest request, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            request.Method = Method.Delete;
            Execute<object>(request, statusCode);
        }
    }
}
