namespace EchoTspServer
{
    public interface ITimer : IDisposable
    {
        void Change(int dueTime, int period);
    }
    
    public delegate void TimerCallback(object? state);
}
