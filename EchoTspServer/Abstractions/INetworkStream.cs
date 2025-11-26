namespace EchoTspServer
{
    public interface INetworkStream
    {
        Task WriteAsync(Byte[] buffer);
        void Close();
    }
}