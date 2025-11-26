public interface ITimerFactory
{
    ITimer Create(TimerCallback callback, object? state, int dueTime, int period);
}