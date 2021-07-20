using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pecan
{
    public class WebServer
    {
        private readonly HttpListener httpListener;
        private readonly Dictionary<(string Path, string HttpMethod), Func<HttpListenerContext, Task<object>>> matchers =
            new Dictionary<(string, string), Func<HttpListenerContext, Task<object>>>();

        public WebServer() : this(new HttpListener())
        {
        }

        public WebServer(HttpListener httpListener)
        {
            this.httpListener = httpListener ?? throw new ArgumentNullException(nameof(httpListener));
        }

        public void Map(string path, HttpMethod httpMethod, Func<HttpListenerContext, Task<object>> handler)
        {
            this.matchers.Add((path.ToUpperInvariant(), httpMethod.Method.ToUpperInvariant()), handler);
        }

        public Task RunAsync()
        {
            return RunAsync(CancellationToken.None);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            httpListener.Start();
            await Task.Run(() => MainLoop(ct), ct)
                .ConfigureAwait(false);
        }

        public void Start()
        {
            httpListener.Start();

            Task.Run(() => MainLoop(CancellationToken.None));
        }

        private async Task MainLoop(CancellationToken ct)
        {
            while (httpListener.IsListening && !ct.IsCancellationRequested)
            {
                var context = await httpListener.GetContextAsync()
                    .ConfigureAwait(false);

                var key = (context.Request.Url.AbsolutePath.ToUpperInvariant(), context.Request.HttpMethod.ToUpperInvariant());

                if (matchers.TryGetValue(key, out var handler))
                {
                    object response = await handler(context)
                        .ConfigureAwait(false);

                    if (response is VoidHolder)
                    {
                        // Void return, nothing to flush
                        goto completeRequest;
                    }
                    else if (response is string str)
                    {
                        byte[] buffer = ArrayPool<byte>.Shared.Rent(str.Length);

                        try
                        {
                            int count = Encoding.UTF8.GetBytes(str, buffer);

                            await context.Response.OutputStream.WriteAsync(buffer, 0, count)
                                .ConfigureAwait(false);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }

            completeRequest:
                context.Response.Close();
            }
        }

        public void Stop()
        {
            httpListener.Stop();
        }
    }
}
