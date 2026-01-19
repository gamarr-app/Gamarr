using System;
using Microsoft.AspNetCore.Mvc;

namespace Gamarr.Http.REST.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RestPostByIdAttribute : HttpPostAttribute
    {
    }
}
