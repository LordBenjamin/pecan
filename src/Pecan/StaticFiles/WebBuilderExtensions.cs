using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Pecan.StaticFiles
{
    public static class WebBuilderExtensions
    {
        public static WebServerBuilder MapIndex(this WebServerBuilder builder, string physicalPath, string contentType = null)
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
            if (!string.IsNullOrEmpty(contentType))
            {
                context.Response.ContentType = contentType;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                using (Stream fileStream = File.OpenRead(physicalFileName))
                {
                    int bytesRead;
                    do
                    {
                        bytesRead = await fileStream.ReadAsync(buffer)
                            .ConfigureAwait(false);

                        await context.Response.OutputStream.WriteAsync(buffer, 0, bytesRead)
                            .ConfigureAwait(false);
                    } while (bytesRead > 0);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
