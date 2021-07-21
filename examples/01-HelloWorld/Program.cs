using System.Threading.Tasks;

namespace BenjaminOwen.Meadow.Samples.DigitalThermometer.Web
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--exitOnDebug") return;

            // instantiate and run new meadow app
            var app = new MeadowApp();
            await app.RunAsync();
        }
    }
}
