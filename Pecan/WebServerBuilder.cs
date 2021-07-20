using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Pecan
{
    public class WebServerBuilder
    {
        private HttpListener httpListener;
        private readonly List<(string Path, Func<HttpListenerContext, Task<object>> Handler)> handlers =
            new List<(string, Func<HttpListenerContext, Task<object>>)>();

        public WebServerBuilder()
        {
            httpListener = new HttpListener();
        }

        public WebServerBuilder MapGet(string path, Action<HttpListenerContext> handler)
        {
            this.handlers.Add((path, context =>
            {
                handler(context);
                return Task.FromResult(VoidHolder.AsObject);
            }
            ));

            return this;
        }

        public WebServerBuilder MapGet(string path, Func<HttpListenerContext, object> handler)
        {
            this.handlers.Add((path, context => Task.FromResult(handler(context))));
            return this;
        }

        public WebServerBuilder MapGet(string path, Func<HttpListenerContext, Task> handler)
        {
            this.handlers.Add((path, async context =>
            {
                await handler(context)
                    .ConfigureAwait(false);

                return VoidHolder.AsObject;
            }
            ));

            return this;
        }

        public WebServerBuilder MapGet(string path, Func<HttpListenerContext, Task<object>> handler)
        {
            this.handlers.Add((path, handler));
            return this;
        }

        public WebServerBuilder ListenOn(IPAddress ipAddress, int port = 80)
        {
            httpListener.Prefixes.Add($"http://{ipAddress}:{port}/");
            return this;
        }

        public WebServer Build()
        {
            var server = new WebServer(httpListener);

            foreach (var handler in handlers)
            {
                server.MapGet(handler.Path, handler.Handler);
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
