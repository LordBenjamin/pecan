using BenjaminOwen.Meadow.Displays;
using DigitalThermometer.Web;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Temperature;
using Meadow.Gateway.WiFi;
using Meadow.Units;
using Pecan;
using Pecan.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenjaminOwen.Meadow.Samples.DigitalThermometer.Web
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        private readonly MAX7219 display;
        private readonly AnalogTemperature tmp36;

        public MeadowApp()
        {
            Console.WriteLine("Creating devices...");

            display = new MAX7219(
                Device,
                din: Device.Pins.D00,
                cs: Device.Pins.D01,
                clk: Device.Pins.D02,
                displayCount: 1);

            tmp36 = new AnalogTemperature(Device, Device.Pins.A00, AnalogTemperature.KnownSensorType.TMP36);
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
            if (!Device.WiFiAdapter.StartWiFiInterface())
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
                .WithConsoleLogger()
                .WithInMemoryLogger()
                .MapGet("/temperature", context => Math.Round(CurrentTemperature.Celsius, 2).ToString())
                .Start();

            Console.WriteLine($"Pecan listening at: http://{Device.WiFiAdapter.IpAddress}/temperature");

            // Enter main loop
            int lastTemperature = 0;

            // Poll TMP36 and update display when temperature changes
            while (true)
            {
                CurrentTemperature = await tmp36.Read()
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

