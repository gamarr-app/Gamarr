using System.Net;
using Gamarr.Http.Exceptions;

namespace Gamarr.Http.REST
{
    public class MethodNotAllowedException : ApiException
    {
        public MethodNotAllowedException(object content = null)
            : base(HttpStatusCode.MethodNotAllowed, content)
        {
        }
    }
}
