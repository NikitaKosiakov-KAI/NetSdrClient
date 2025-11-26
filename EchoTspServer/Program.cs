using System;
using System.Threading.Tasks;

namespace EchoTspServer
{
    public static class Program
    {

        public static async Task Main(string[] args)
        {
            var listener = new DefaultTcpListener(5000);
            var handler  = new EchoClientHandler();
            var logger   = new ConsoleLogger();

            var server = new EchoServer(listener, handler, logger);

            // Запускаємо сервер у фоні
            var serverTask = server.StartAsync();

            // Запускаємо UDP-відправник
            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(3000);

            Console.WriteLine("Press 'Q' to stop...");
            // Read keys until user presses Q. Use an explicit loop body to avoid an empty block.
            ConsoleKey key;
            do
            {
                key = Console.ReadKey(true).Key;
            } while (key != ConsoleKey.Q);

            sender.StopSending();
            server.Stop();
            await serverTask;
        }
    }
}