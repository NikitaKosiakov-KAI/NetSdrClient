using System.Net.Sockets;
using System.Threading.Channels;

namespace EchoTspServer
{
    public interface ITcpClient : IDisposable
    {
        Boolean Connected { get; }
        Task ConnectAsync(String host, Int32 port);
        NetworkStream GetStream();
        void Close();
    }
}