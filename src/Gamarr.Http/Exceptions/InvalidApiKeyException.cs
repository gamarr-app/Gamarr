using System.Net;

namespace Gamarr.Http.Exceptions
{
    public class InvalidApiKeyException : ApiException
    {
        public InvalidApiKeyException()
            : base(HttpStatusCode.Unauthorized)
        {
        }

        public InvalidApiKeyException(string message)
            : base(HttpStatusCode.Unauthorized, message)
        {
        }
    }
}
