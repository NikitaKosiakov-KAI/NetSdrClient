using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace EchoTspServer
{
    // Періодично надсилає UDP-повідомлення.
    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly UdpClient _udpClient;
        private Timer? _timer;
        private ushort _counter = 0;
        private bool _disposed;

        // 🔐 Криптографічно безпечний генератор (прибирає Sonar Warning)
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        // 📌 Статичний заголовок
        private static readonly byte[] Header = new byte[] { 0x04, 0x84 };

        public UdpTimedSender(string host, int port)
        {
            _host = host;
            _port = port;
            _udpClient = new UdpClient();
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        private void SendMessageCallback(object? state)
        {
            try
            {
                var samples = new byte[1024];
                _rng.GetBytes(samples);
                _counter++;

                byte[] data = Header
                    .Concat(BitConverter.GetBytes(_counter))
                    .Concat(samples)
                    .ToArray();

                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
                _udpClient.Send(data, data.Length, endpoint);
            }
            catch
            {
                // Фонові помилки ігноруються
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                StopSending();
                _udpClient.Dispose();
                _rng.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}