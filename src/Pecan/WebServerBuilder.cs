using Pecan.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Pecan
{
    public class WebServerBuilder
    {
        private HttpListener httpListener;
        private readonly List<(string Path, HttpMethod HttpMethod, Func<HttpListenerContext, Task<object>> Handler)> handlers =
            new List<(string, HttpMethod, Func<HttpListenerContext, Task<object>>)>();

        public List<ILogger> Loggers { get; } = new List<ILogger>();

        public WebServerBuilder()
        {
            httpListener = new HttpListener();
        }

        public WebServerBuilder MapGet(string path, Action<HttpListenerContext> handler)
        {
            return Map(path, HttpMethod.Get, handler);
        }

        public WebServerBuilder MapGet(string path, Func<HttpListenerContext, object> handler)
        {
            return Map(path, HttpMethod.Get, handler);
        }

        public WebServerBuilder MapGet(string path, Func<HttpListenerContext, Task> handler)
        {
            return Map(path, HttpMethod.Get, handler);
        }

        public WebServerBuilder MapGet(string path, Func<HttpListenerContext, Task<object>> handler)
        {
            return Map(path, HttpMethod.Get, handler);
        }

        public WebServerBuilder Map(string path, HttpMethod httpMethod, Action<HttpListenerContext> handler)
        {
            this.handlers.Add((path, httpMethod, context =>
            {
                handler(context);
                return Task.FromResult(VoidHolder.AsObject);
            }
            ));

            return this;
        }

        public WebServerBuilder Map(string path, HttpMethod httpMethod, Func<HttpListenerContext, object> handler)
        {
            this.handlers.Add((path, httpMethod, context => Task.FromResult(handler(context))));
            return this;
        }

        public WebServerBuilder Map(string path, HttpMethod httpMethod, Func<HttpListenerContext, Task> handler)
        {
            this.handlers.Add((path, httpMethod, async context =>
            {
                await handler(context)
                    .ConfigureAwait(false);

                return VoidHolder.AsObject;
            }
            ));

            return this;
        }

        public WebServerBuilder Map(string path, HttpMethod httpMethod, Func<HttpListenerContext, Task<object>> handler)
        {
            this.handlers.Add((path, httpMethod, handler));
            return this;
        }

        public WebServerBuilder ListenOn(IPAddress ipAddress, int port = 80)
        {
            httpListener.Prefixes.Add($"http://{ipAddress}:{port}/");
            return this;
        }

        public WebServer Build()
        {
            ILogger logger = null;
            if (Loggers.Count == 1)
            {
                logger = Loggers[0];
            }
            else if (Loggers.Count > 1)
            {
                logger = new AggregateLogger(Loggers);
            }

            var server = new WebServer(httpListener, logger);

            foreach (var handler in handlers)
            {
                server.Map(handler.Path, handler.HttpMethod, handler.Handler);
            }

            return server;
        }

        public Task RunAsync()
        {
            return Build().RunAsync();
        }

        public WebServer Start()
        {
            var server = Build();
            server.Start();

            return server;
        }
    }
}
