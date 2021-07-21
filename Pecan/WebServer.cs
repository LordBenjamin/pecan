using Pecan.Logging;
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
        private readonly ILogger logger;
        private readonly HttpListener httpListener;
        private readonly Dictionary<(string Path, string HttpMethod), Func<HttpListenerContext, Task<object>>> matchers =
            new Dictionary<(string, string), Func<HttpListenerContext, Task<object>>>();

        public WebServer()
            : this(new HttpListener(), null)
        {
        }

        public WebServer(HttpListener httpListener, ILogger logger)
        {
            this.httpListener = httpListener ?? throw new ArgumentNullException(nameof(httpListener));
            this.logger = logger;
        }

        public void Map(string path, HttpMethod httpMethod, Func<HttpListenerContext, Task<object>> handler)
        {
            logger?.Log($"Mapped {httpMethod.Method} {path}");
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
            logger?.Log($"Started");

            while (httpListener.IsListening && !ct.IsCancellationRequested)
            {
                logger?.Log($"Waiting for request");
                var context = await httpListener.GetContextAsync()
                    .ConfigureAwait(false);

                Stopwatch sw = Stopwatch.StartNew();

                var key = (context.Request.Url.AbsolutePath.ToUpperInvariant(), context.Request.HttpMethod.ToUpperInvariant());

                logger?.Log($"{context.Request.HttpMethod} {context.Request.RawUrl}");

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
                        catch (Exception ex)
                        {
                            logger?.Log($"Error processing request: {ex}");
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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

                logger?.Log($"{context.Response.StatusCode} ({sw.ElapsedMilliseconds:n0} ms)");
            }

            logger?.Log($"Stopped");
        }

        public void Stop()
        {
            httpListener.Stop();
        }
    }
}
