using System;
using System.Diagnostics;

namespace Pecan.Logging
{
    public class InMemoryLogger : ILogger
    {
        private readonly string[] buffer;
        private int writePtr;
        private int readPtr = 0;
        private bool isFull;

        public InMemoryLogger(int capacity)
        {
            buffer = new string[capacity];
        }

        public void Log(string text)
        {
            lock (buffer)
            {
                buffer[writePtr++] = text;

                if (writePtr >= buffer.Length)
                {
                    isFull = true;
                    writePtr = 0;
                }

                if (isFull)
                {
                    readPtr = writePtr;
                }
            }
        }

        public int Read(string[] destination, int count)
        {
            int i = 0;
            int max = isFull ? count : writePtr - 1;
            int read = readPtr;

            do
            {
                destination[i++] = buffer[read++];

                if (read >= buffer.Length)
                {
                    read = 0;
                }
            } while (i < max);

            return i;
        }
    }
}
