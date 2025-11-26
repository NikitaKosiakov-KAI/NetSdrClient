namespace EchoTspServer
{
    // Простий логгер як абстракція.
    // У тестах замінюємо на мок.
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
    }
}