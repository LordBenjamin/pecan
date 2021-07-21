using System;
using System.Net.Http;

namespace Pecan.Controllers
{
    public class HttpMethodAttribute : Attribute
    {
        public HttpMethodAttribute(HttpMethod httpMethod)
        {
            HttpMethod = httpMethod;
        }

        public HttpMethod HttpMethod { get; }
    }
}