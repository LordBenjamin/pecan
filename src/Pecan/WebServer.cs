using Pecan.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            logger?.Log("Pecan Web Server started");

            ulong requestId = 0;
            while (httpListener.IsListening && !ct.IsCancellationRequested)
            {
                logger?.Log("Waiting for request");
                var context = await httpListener.GetContextAsync()
                    .ConfigureAwait(false);

                requestId = unchecked(requestId + 1); // it's okay to roll over this value, it's just for log correlation
                logger?.Log($"[{requestId}] {context.Request.HttpMethod} {context.Request.RawUrl}");

                Stopwatch sw = Stopwatch.StartNew();

                var key = (context.Request.Url.AbsolutePath.ToUpperInvariant(), context.Request.HttpMethod.ToUpperInvariant());

                if (!matchers.TryGetValue(key, out var handler))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    goto completeRequest;
                }

                object response = VoidHolder.AsObject;
                try
                {
                    response = await handler(context)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger?.Log($"[{requestId}] Error processing request: {ex}");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

                if (response is VoidHolder)
                {
                    // Void return, nothing to flush
                    goto completeRequest;
                }
                else if (response is string str)
                {
                    context.Response.ContentLength64 = str.Length;
                    await WriteStringBuffered(context.Response.OutputStream, str)
                        .ConfigureAwait(false);
                }

            completeRequest:
                context.Response.Close();

                logger?.Log($"[{requestId}] {context.Response.StatusCode} ({sw.ElapsedMilliseconds:n0} ms)");
            }

            if (ct.IsCancellationRequested)
            {
                httpListener.Stop();
            }

            logger?.Log($"Stopped");
        }

        private Task WriteStringBuffered(Stream stream, string str, int bufferSize = 4096)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(str.Length);

            try
            {
                return WriteStringBuffered(stream, str, buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task WriteStringBuffered(Stream stream, string str, byte[] buffer)
        {
            int readIndex = 0;
            int bytesRead;

            do
            {
                int bytesToRead = Math.Min(str.Length - readIndex, buffer.Length);
                bytesRead = Encoding.UTF8.GetBytes(str, readIndex, bytesToRead, buffer, 0);
                readIndex += bytesRead;

                await stream.WriteAsync(buffer, 0, bytesRead)
                    .ConfigureAwait(false);
            } while (bytesRead > 0);
        }

        public void Stop()
        {
            httpListener.Stop();
        }
    }
}
