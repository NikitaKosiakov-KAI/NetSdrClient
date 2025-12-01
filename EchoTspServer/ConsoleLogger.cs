using System;

namespace EchoTspServer
{
    // Проста реалізація логгера через Console.
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine("[INFO] " + message);
        }

        public void LogError(string message)
        {
            Console.WriteLine("[ERROR] " + message);
        }
    }
}