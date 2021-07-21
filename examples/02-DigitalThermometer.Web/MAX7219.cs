using System;
using Meadow.Hardware;

namespace BenjaminOwen.Meadow.Displays
{
    public class MAX7219
    {
        private const byte DecodeModeOpCode = 0x9;
        private const byte IntensityOpCode = 0xA;
        private const byte ScanLimitOpCode = 0xB;
        private const byte ShutdownOpCode = 0xC;
        private const byte DisplayTestOpCode = 0xF;

        private IDigitalOutputPort dinPort;
        private IDigitalOutputPort csPort;
        private IDigitalOutputPort clkPort;

        // 2 bytes (opcode + data) * 8 segments
        private byte[] commandBuffer;

        // 8 segments
        byte[] valueBuffer = new byte[8];

        public MAX7219(IDigitalOutputController device, IPin din, IPin cs, IPin clk, int displayCount = 1)
        {
            Device = device;
            DisplayCount = displayCount < 0 || displayCount > 8 ? 8 : displayCount;

            commandBuffer = new byte[DisplayCount * 2];

            dinPort = device.CreateDigitalOutputPort(din);
            csPort = device.CreateDigitalOutputPort(cs);
            clkPort = device.CreateDigitalOutputPort(clk);

            csPort.State = true;
        }

        public IDigitalOutputController Device { get; }
        public int DisplayCount { get; }

        public void Wake()
        {
            SendCommand(ShutdownOpCode, 1); // Essentially enable normal operation by telling it not to be shut down
        }

        internal void SetScanLimit(byte value)
        {
            SendCommand(ScanLimitOpCode, value);
        }

        internal void StartDisplayTest()
        {
            SendCommand(DisplayTestOpCode, 1);
        }

        internal void StopDisplayTest()
        {
            SendCommand(DisplayTestOpCode, 0);
        }

        internal void Shutdown()
        {
            SendCommand(ShutdownOpCode, 0);
        }

        internal void SetIntensity(byte value)
        {
            SendCommand(IntensityOpCode, value);
        }

        internal void SetDecodeModeOn(int addr = 0)
        {
            SendCommand(DecodeModeOpCode, 0xFF);
        }

        internal void SetDecodeModeOff(int addr = 0)
        {
            SendCommand(DecodeModeOpCode, 0x0);
        }

        public void ClearDisplay(int addr = 0)
        {
            for (int i = 0; i < 8; i++)
            {
                SendCommand(addr, (byte)(i + 1), 0);
            }
        }

        public void SetDigit(byte digit, byte value, int addr = 0, bool decimalPoint = false)
        {
            if (decimalPoint)
            {
                value |= 0B10000000;
            }

            SendCommand(addr, (byte)(digit + 1), value);
        }

        public void SetValue(int num, int addr = 0)
        {
            // divides each digit into its own element within an array
            int i = 7;
            while (num > 0)
            {
                valueBuffer[i--] = (byte)(num % 10);
                num /= 10;
            }

            for(; i < valueBuffer.Length; i++)
            {
                SetDigit((byte)(7 - i), valueBuffer[i]);
            }
        }

        private void SendCommand(byte opCode, byte data)
        {
            for (int i = 0; i < DisplayCount; i++)
            {
                SendCommand(i, opCode, data);
            }
        }

        private void SendCommand(int addr, byte opCode, byte data)
        {
            int offset = addr * 2;

            Array.Clear(commandBuffer, 0, commandBuffer.Length);

            commandBuffer[offset] = data;
            commandBuffer[offset + 1] = opCode;

            csPort.State = false;

            for (int i = commandBuffer.Length; i > 0; i--)
            {
                ShiftOut(commandBuffer[i - 1]);
            }

            csPort.State = true;
            clkPort.State = false;
        }

        private void ShiftOut(byte value)
        {
            for (int i = 0; i < 8; i++)
            {
                clkPort.State = false;

                dinPort.State = GetBit(value, i);

                clkPort.State = true;
            }
        }

        // Assume 0 is the MSB andd 7 is the LSB.
        public static bool GetBit(byte value, int index)
        {
            if (index < 0 || index > 7)
                throw new ArgumentOutOfRangeException();

            int shift = 7 - index;

            // Get a single bit in the proper position.
            byte bitMask = (byte)(1 << shift);

            // Mask out the appropriate bit.
            byte masked = (byte)(value & bitMask);

            // If masked != 0, then the masked out bit is 1.
            // Otherwise, masked will be 0.
            return masked != 0;
        }
    }
}
