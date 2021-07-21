using System;
using System.Collections.Generic;

namespace Pecan.Logging
{
    public class AggregateLogger : ILogger
    {
        public AggregateLogger(IEnumerable<ILogger> loggers)
        {
            Loggers = loggers ?? throw new ArgumentNullException(nameof(loggers));
        }

        public IEnumerable<ILogger> Loggers { get; }

        public void Log(string text)
        {
            foreach (var logger in Loggers)
            {
                logger.Log(text);
            }
        }
    }
}
