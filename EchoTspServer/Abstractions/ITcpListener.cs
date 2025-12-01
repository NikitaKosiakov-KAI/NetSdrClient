using System.Net.Sockets;
using System.Threading.Tasks;

namespace EchoTspServer
{
    // Абстракція над TcpListener, щоб EchoServer можна було мокати в тестах.
    public interface ITcpListener
    {
        void Start();
        void Stop();
        Task<TcpClient> AcceptTcpClientAsync();
    }
}