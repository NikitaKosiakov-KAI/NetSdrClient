using System.Threading;
using System.Threading.Tasks;
using EchoTspServer;
using NSubstitute;
using NUnit.Framework;
using System.Net.Sockets;

namespace EchoServerTests
{
    [TestFixture]
    public class EchoServerTests
    {
        [Test, Timeout(2000)]
        public async Task StartAsync_CallsHandler_WhenClientConnects()
        {
            var listener = Substitute.For<ITcpListener>();
            var handler = Substitute.For<IClientHandler>();
            var logger = Substitute.For<ILogger>();

            var fakeClient = new TcpClient();

            // 1-й клієнт → нормальний
            // 2-й зависає як реальний сервер (очікування наступних коннектів)
            listener.AcceptTcpClientAsync()
                .Returns(
                    Task.FromResult(fakeClient),
                    Task.Delay(Timeout.Infinite).ContinueWith(_ => new TcpClient())
                );

            var server = new EchoServer(listener, handler, logger);

            var serverTask = server.StartAsync();
            await Task.Delay(50);
            server.Stop();
            await Task.WhenAny(serverTask, Task.Delay(500));

            await handler.Received(1).HandleClientAsync(fakeClient, Arg.Any<CancellationToken>());
        }

        [Test, Timeout(2000)]
        public void Stop_DoesNotThrow_WhenCalledMultipleTimes()
        {
            var listener = Substitute.For<ITcpListener>();
            var handler = Substitute.For<IClientHandler>();
            var logger = Substitute.For<ILogger>();

            var server = new EchoServer(listener, handler, logger);

            Assert.DoesNotThrow(() => server.Stop());
            Assert.DoesNotThrow(() => server.Stop());
        }

        [Test, Timeout(2000)]
        public void Stop_CallsListenerStop_IfConnected()
        {
            var listener = Substitute.For<ITcpListener>();
            var handler = Substitute.For<IClientHandler>();
            var logger = Substitute.For<ILogger>();

            var server = new EchoServer(listener, handler, logger);
            server.Stop();

            listener.Received().Stop();
        }

        [Test, Timeout(2000)]
        public void Dispose_CallsStop_AndDisposesResources()
        {
            var listener = Substitute.For<ITcpListener, IDisposable>();
            var handler = Substitute.For<IClientHandler, IDisposable>();
            var logger = Substitute.For<ILogger, IDisposable>();

            var server = new EchoServer(listener, handler, logger);
            server.Dispose();

            listener.Received().Stop();
            (listener as IDisposable)!.Received().Dispose();
            (handler as IDisposable)!.Received().Dispose();
            (logger as IDisposable)!.Received().Dispose();
        }

        [Test, Timeout(2000)]
        public void Stop_Ignored_AfterDispose()
        {
            var listener = Substitute.For<ITcpListener>();
            var handler = Substitute.For<IClientHandler>();
            var logger = Substitute.For<ILogger>();

            var server = new EchoServer(listener, handler, logger);
            server.Dispose();
            server.Stop();

            listener.Received(1).Stop(); // тільки раз — через Dispose
        }

        [Test, Timeout(2000)]
        public async Task StartAsync_IgnoresExceptions_FromListener()
        {
            var listener = Substitute.For<ITcpListener>();
            var handler = Substitute.For<IClientHandler>();
            var logger = Substitute.For<ILogger>();

            listener.AcceptTcpClientAsync().Returns<Task<TcpClient>>(x => { throw new SocketException(); });

            var server = new EchoServer(listener, handler, logger);
            var task = server.StartAsync();

            await Task.Delay(50);
            server.Stop();

            Assert.That(task.IsCompleted, Is.True);
        }
        [Test]
        public async Task Program_RunLifecycle_NoExceptions()
        {
            var listener = new DefaultTcpListener(5000);
            var handler = new EchoClientHandler();
            var logger = new ConsoleLogger();
            var server = new EchoServer(listener, handler, logger);

            var serverTask = server.StartAsync();

            using var sender = new UdpTimedSender("127.0.0.1", 60000);
            sender.StartSending(50); // менший інтервал

            await Task.Delay(200); // імітуємо роботу

            Assert.That(() => sender.StopSending(), Throws.Nothing);
            Assert.That(() => server.Stop(), Throws.Nothing);

            await serverTask;
        }

    }
}