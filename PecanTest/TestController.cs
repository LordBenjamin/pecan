using Pecan;
using Pecan.Controllers;
using System.Net;
using System.Threading.Tasks;

namespace PecanTest
{
    internal class TestController
    {
        private int i = 0;

        public TestController()
        {
        }

        [HttpGet]
        public void Increment()
        {
            i = i + 1;
        }

        [HttpGet]
        public async Task Decrement(HttpListenerContext context)
        {
            string? quantity = context.Request.QueryString["quantity"];
            i = i - (quantity == null ? 1 : int.Parse(quantity));
            await Task.CompletedTask;
        }

        [HttpGet]
        public async Task<int> ReadAsync()
        {
            return await Task.FromResult(i);
        }

        [HttpGet]
        public int Read()
        {
            return i;
        }
    }
}