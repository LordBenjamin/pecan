using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Pecan.StaticFiles
{
    public static class WebBuilderExtensions
    {
        private static readonly Assembly EntryAssembly;

        static WebBuilderExtensions()
        {
            EntryAssembly = Assembly.GetEntryAssembly();
        }

        public static WebServerBuilder MapIndexFile(this WebServerBuilder builder, string physicalPath, string contentType = null)
        {
            return MapStaticFile(builder, physicalPath, "/", contentType);
        }

        public static WebServerBuilder MapStaticFiles(this WebServerBuilder builder, string physicalPath, string pattern = "*", string virtualPathRoot = "/", string contentType = null)
        {
            string[] files = Directory.GetFiles(physicalPath, pattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string virtualPath = (virtualPathRoot + Path.GetRelativePath(physicalPath, file))
                    .Replace('\\', '/');
                MapStaticFile(builder, file, virtualPath, contentType);
            }

            return builder;
        }

        public static WebServerBuilder MapStaticFile(this WebServerBuilder builder, string physicalPath, string virtualPath, string contentType = null)
        {
            // TODO: Customizable MIME mapping by file extension
            if (contentType == null && ".html".Equals(Path.GetExtension(physicalPath), StringComparison.OrdinalIgnoreCase))
            {
                contentType = "text/html";
            }

            return builder.MapGet(virtualPath, context => ServeFileAsync(context, physicalPath, contentType));
        }

        private static async Task ServeFileAsync(HttpListenerContext context, string physicalFileName, string contentType)
        {

            using (Stream stream = File.OpenRead(physicalFileName))
            {
                await ServeStreamAsync(context, stream, contentType);
            }
        }

        public static WebServerBuilder MapIndexResource(this WebServerBuilder builder, string resourceKey, string contentType = null)
        {
            return MapEmbeddedResource(builder, resourceKey, "/", contentType);
        }

        public static WebServerBuilder MapEmbeddedResourceFiles(this WebServerBuilder builder, string virtualPathRoot = "/", string contentType = null)
        {
            string[] resourceNames = Assembly.GetEntryAssembly().GetManifestResourceNames();

            foreach (var resourceKey in resourceNames)
            {
                string virtualPath = (virtualPathRoot + resourceKey)
                      .Replace('\\', '/');

                MapEmbeddedResource(builder, resourceKey, virtualPath, contentType);
            }


            return builder;
        }

        public static WebServerBuilder MapEmbeddedResource(this WebServerBuilder builder, string resourceKey, string virtualPath, string contentType = null)
        {
            // TODO: Customizable MIME mapping by file extension
            if (contentType == null && ".html".Equals(Path.GetExtension(resourceKey), StringComparison.OrdinalIgnoreCase))
            {
                contentType = "text/html";
            }

            return builder.MapGet(virtualPath, context => ServeEmbeddedResourceAsync(context, resourceKey, contentType));
        }

        private static async Task ServeEmbeddedResourceAsync(HttpListenerContext context, string resourceKey, string contentType)
        {
            using (Stream stream = EntryAssembly.GetManifestResourceStream(resourceKey))
            {
                await ServeStreamAsync(context, stream, contentType, stream.Length);
            }
        }

        private static async Task ServeStreamAsync(HttpListenerContext context, Stream stream, string contentType, long? length = null)
        {
            if (!string.IsNullOrEmpty(contentType))
            {
                context.Response.ContentType = contentType;

                if (length.HasValue)
                {
                    context.Response.ContentLength64 = length.Value;
                }
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                int bytesRead;
                do
                {
                    bytesRead = await stream.ReadAsync(buffer)
                        .ConfigureAwait(false);

                    await context.Response.OutputStream.WriteAsync(buffer, 0, bytesRead)
                        .ConfigureAwait(false);
                } while (bytesRead > 0);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
