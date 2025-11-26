using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoTspServer;
using NUnit.Framework;

namespace EchoServerTests
{
    [TestFixture]
    public class EchoClientHandlerTests
    {
        [Test, Timeout(2000)]
        public async Task HandleClientAsync_EchoesBackData_Fast()
        {
            using var s1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using var s2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var ep = new IPEndPoint(IPAddress.Loopback, 0);
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(ep);
            listener.Listen(1);

            int port = ((IPEndPoint)listener.LocalEndPoint!).Port;

            var connectTask = s1.ConnectAsync(IPAddress.Loopback, port);
            var acceptTask = listener.AcceptAsync();

            await Task.WhenAll(connectTask, acceptTask);

            var accepted = acceptTask.Result;
            Assert.NotNull(accepted, "AcceptAsync returned null");

            var serverClient = new TcpClient { Client = accepted! };

            var handler = new EchoClientHandler();

            var handlerTask = handler.HandleClientAsync(serverClient, CancellationToken.None);

            byte[] sendData = { 1, 2, 3, 4 };
            await s1.SendAsync(sendData, SocketFlags.None);

            byte[] buffer = new byte[4];
            int read = await s1.ReceiveAsync(buffer, SocketFlags.None);

            Assert.AreEqual(4, read);
            Assert.That(buffer, Is.EqualTo(sendData));

            listener.Close();
        }
        
    }
}