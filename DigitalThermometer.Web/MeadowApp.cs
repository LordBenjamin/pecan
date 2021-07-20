using BenjaminOwen.Meadow.Displays;
using BenjaminOwen.Meadow.Sensors.Temperature;
using DigitalThermometer.Web;
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
        private readonly MAX7219 display;
        private readonly TMP36 tmp36;

        public MeadowApp()
        {
            Console.WriteLine("Creating devices...");

            display = new MAX7219(
                Device,
                din: Device.Pins.D00,
                cs: Device.Pins.D01,
                clk: Device.Pins.D02,
                displayCount: 1);

            tmp36 = new TMP36(Device, Device.Pins.A00);
        }

        public Temperature CurrentTemperature { get; private set; }

        public async Task RunAsync()
        {
            Console.WriteLine("Run...");
            // Init display
            display.StopDisplayTest();
            display.SetIntensity(0xF);
            display.SetScanLimit(0x7);
            display.SetDecodeModeOn();
            display.ClearDisplay();
            display.Wake();

            Console.WriteLine("Initialize WiFi...");

            // initialize the wifi adapter
            if (!Device.InitWiFiAdapter().Result)
            {
                throw new Exception("Could not initialize the WiFi adapter.");
            }

            // connnect to the wifi network.
            Console.WriteLine($"Connecting to WiFi Network {Secrets.WifiSsid}");

            var connectionResult = await Device.WiFiAdapter.Connect(Secrets.WifiSsid, Secrets.WifiPassword);
            if (connectionResult.ConnectionStatus != ConnectionStatus.Success)
            {
                throw new Exception($"Cannot connect to network: {connectionResult.ConnectionStatus}");
            }

            Console.WriteLine($"Connected. IP: {Device.WiFiAdapter.IpAddress}");

            // create a Pecan web server
            var server = new WebServerBuilder()
                .ListenOn(Device.WiFiAdapter.IpAddress)
                .MapGet("/temperature", context => Math.Round(CurrentTemperature.Celsius, 2).ToString())
                .Start();

            Console.WriteLine($"Pecan listening at: http://{Device.WiFiAdapter.IpAddress}/temperature");

            // Enter main loop
            int lastTemperature = 0;

            // Poll TMP36 and update display when temperature changes
            while (true)
            {
                CurrentTemperature = await tmp36.ReadAsync()
                    .ConfigureAwait(false);

                int roundedTemperature = (int)Math.Round(CurrentTemperature.Celsius, 0);

                if (roundedTemperature != lastTemperature)
                {
                    display.SetValue(roundedTemperature);
                    lastTemperature = roundedTemperature;
                    Thread.Sleep(500);
                }
            }
        }
    }
}

