using Pecan;
using Pecan.Controllers;
using Pecan.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace PecanTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new WebServerBuilder()
                .ListenOn(IPAddress.Parse("127.0.0.1"), 1337)
                .WithConsoleLogger()
                .WithInMemoryLogger()
                .MapGet("/test-async", async context => await Task.FromResult("async"))
                .MapGet("/test", context => "not async")
                .MapGet("/gc", context =>
                {
                    GC.Collect(2, GCCollectionMode.Forced, true);
                    return GC.GetTotalMemory(true).ToString();
                })
                .MapController(new TestController())
                .RunAsync();
        }
    }
}
