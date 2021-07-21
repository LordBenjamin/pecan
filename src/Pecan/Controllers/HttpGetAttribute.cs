using System;
using System.Net.Http;

namespace Pecan.Controllers
{
    public sealed class HttpGetAttribute : HttpMethodAttribute
    {
        public HttpGetAttribute()
            : base(HttpMethod.Get)
        {
        }
    }
}