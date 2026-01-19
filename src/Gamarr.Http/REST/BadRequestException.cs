using System.Net;
using Gamarr.Http.Exceptions;

namespace Gamarr.Http.REST
{
    public class BadRequestException : ApiException
    {
        public BadRequestException(object content = null)
            : base(HttpStatusCode.BadRequest, content)
        {
        }
    }
}
