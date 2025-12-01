using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTspServer
{
    // Обробляє одного TCP-клієнта: читає байти і відправляє їх назад (echo).
    public class EchoClientHandler : IClientHandler
    {
        public async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];

            while (!token.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                if (read == 0)
                    break;

                await stream.WriteAsync(buffer.AsMemory(0, read), token);
            }

            client.Close();
        }
    }
}