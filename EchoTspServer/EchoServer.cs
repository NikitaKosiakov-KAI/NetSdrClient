using System;
using System.Threading;
using System.Threading.Tasks;
using EchoTspServer;


namespace EchoTspServer
{
    public class EchoServer : IDisposable
    {
        private readonly ITcpListener _listener;
        private readonly IClientHandler _clientHandler;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;

        private bool _disposed;

        public EchoServer(ITcpListener listener, IClientHandler clientHandler, ILogger logger)
        {
            _listener = listener;
            _clientHandler = clientHandler;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener.Start();
            _logger.Log("Server started.");

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _logger.Log("Client connected.");

                    _ = Task.Run(() => _clientHandler.HandleClientAsync(client, _cts.Token));
                }
                catch
                {
                    break;
                }
            }

            _logger.Log("Server stopped.");
        }

        public void Stop()
        {
            // Stop must be idempotent (safe to call many times)
            if (_cts.IsCancellationRequested)
                return;

            _cts.Cancel();
            _listener.Stop();
        }


        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                // managed resources cleanup
                Stop();
                _cts?.Dispose();

                if (_listener is IDisposable disposableListener)
                    disposableListener.Dispose();

                if (_clientHandler is IDisposable disposableHandler)
                    disposableHandler.Dispose();

                if (_logger is IDisposable disposableLogger)
                    disposableLogger.Dispose();
            }

            // no unmanaged resources -> nothing here
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}