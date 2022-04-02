using System;
using System.Buffers;
using System.Text;

namespace Pecan.Logging
{
    public static class WebServerBuilderExtensions
    {
        // Common strings pre-computed for speed
        private static readonly byte[] lineBreakBytes = Encoding.UTF8.GetBytes("\n");
        private static readonly byte[] bodyStartTagBytes = Encoding.UTF8.GetBytes("<html><body><pre>");
        private static readonly byte[] bodyEndTagBytes = Encoding.UTF8.GetBytes("</pre></body></html>");

        public static WebServerBuilder WithConsoleLogger(this WebServerBuilder builder)
        {
            builder.Loggers.Add(new ConsoleLogger());
            return builder;
        }

        public static WebServerBuilder WithInMemoryLogger(this WebServerBuilder builder, int capacity = 100, string route = "/log")
        {
            var logger = new InMemoryLogger(capacity);
            builder.Loggers.Add(logger);

            builder.MapGet(route, context =>
            {

                string[] entries = ArrayPool<string>.Shared.Rent(capacity);
                try
                {
                    context.Response.OutputStream.Write(bodyStartTagBytes);

                    int count = logger.Read(entries, capacity);

                    for (int i = 0; i < count; i++)
                    {
                        string str = entries[i];

                        byte[] buffer = ArrayPool<byte>.Shared.Rent(str.Length);
                        try
                        {
                            int byteCount = Encoding.UTF8.GetBytes(str, buffer);
                            context.Response.OutputStream.Write(buffer, 0, byteCount);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }

                        context.Response.OutputStream.Write(lineBreakBytes);
                    }

                    context.Response.OutputStream.Write(bodyEndTagBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw;
                }
                finally
                {
                    ArrayPool<string>.Shared.Return(entries);
                }
            });

            return builder;
        }
    }
}
