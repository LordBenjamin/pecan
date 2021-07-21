using Meadow;
using Meadow.Devices;
using Meadow.Gateway.WiFi;
using Meadow.Units;
using Pecan;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenjaminOwen.Meadow.Samples.DigitalThermometer.Web
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        private string ssid = "[SSID]";
        private string password = "[PASSWORD]";

        public MeadowApp()
        {
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Initialize WiFi...");

            // initialize the wifi adapter
            if (!Device.InitWiFiAdapter().Result)
            {
                throw new Exception("Could not initialize the WiFi adapter.");
            }

            // connnect to the wifi network.
            Console.WriteLine($"Connecting to WiFi Network {ssid}");

            ConnectionResult connectionResult = await Device.WiFiAdapter.Connect(ssid, password);
            if (connectionResult.ConnectionStatus != ConnectionStatus.Success)
            {
                throw new Exception($"Cannot connect to network: {connectionResult.ConnectionStatus}");
            }

            Console.WriteLine($"Connected. IP: {Device.WiFiAdapter.IpAddress}");

            // create a Pecan web server
            var server = new WebServerBuilder()
                .ListenOn(Device.WiFiAdapter.IpAddress)
                .MapGet("/hi", _ => "Hello, Pecan!")
                .Start();

            Console.WriteLine($"Pecan listening at: http://{Device.WiFiAdapter.IpAddress}/hi");

            await Task.Delay(Timeout.Infinite);
        }
    }
}

