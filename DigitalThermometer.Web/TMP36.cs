using System.Threading.Tasks;
using Meadow.Hardware;
using Meadow.Units;
using TemperatureUnit = Meadow.Units.Temperature;

namespace BenjaminOwen.Meadow.Sensors.Temperature
{
    internal class TMP36
    {
        private const float yIntercept = 500;
        private const float millivoltsPerDegreeC = 10;

        private IAnalogInputPort analogPort;

        public TMP36(IAnalogInputController device, IPin pin)
        {
            analogPort = device.CreateAnalogInputPort(pin, voltageReference: 3.27f);
        }

        public async Task<TemperatureUnit> ReadAsync()
        {
            double millivolts = (await analogPort.Read()).Millivolts;
            return new TemperatureUnit(
                (millivolts - yIntercept) / millivoltsPerDegreeC,
                TemperatureUnit.UnitType.Celsius);
        }
    }
}
