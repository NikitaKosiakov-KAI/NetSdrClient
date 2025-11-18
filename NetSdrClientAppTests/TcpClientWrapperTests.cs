using NUnit.Framework;
using Moq;
using NetSdrClientApp.Networking;
using System.Text;
using System.Threading.Tasks;
using System.Linq; // Для SequenceEqual

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        [Test]
        public async Task StringOverload_DelegatesToByteArrayMethod_WithCorrectEncoding()
        {
            // Arrange
            string testMessage = "Hello World";
            // Очікуємо, що рядок буде закодовано в UTF8
            byte[] expectedBytes = Encoding.UTF8.GetBytes(testMessage);

            // Створюємо Mock реального класу TcpClientWrapper. 
            // Потрібно вказати аргументи конструктора, навіть якщо вони не використовуються.
            var mockWrapper = new Mock<TcpClientWrapper>("127.0.0.1", 5000); 

            // Act
            // Викликаємо перевантаження для рядка
            await mockWrapper.Object.SendMessageAsync(testMessage);

            // Assert
            // Перевіряємо, що метод SendMessageAsync(byte[]) був викликаний
            // рівно один раз і з коректно закодованими байтами.
            mockWrapper.Verify(
                w => w.SendMessageAsync(
                    It.Is<byte[]>(bytes => bytes.SequenceEqual(expectedBytes))), 
                Times.Once(), 
                "Перевантаження для рядка не змогло коректно конвертувати рядок і делегувати виклик.");
        }
    }
}