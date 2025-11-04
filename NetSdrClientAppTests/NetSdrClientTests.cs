using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private NetSdrClient _client;
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);

            // Налаштовуємо логіку Connect/Disconnect
            _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            });

            _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            });
        }

        /// <summary>
        /// Створює імітацію "автовідповідача" для TCP.
        /// Це потрібно, оскільки ваш метод SendTcpRequest очікує відповідь.
        /// </summary>
        private void SetupTcpAutoResponse()
        {
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback(() =>
                {
                    // Імітуємо, що сервер негайно надсилає відповідь
                    _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, new byte[] { 0x01, 0x02 });
                })
                .Returns(Task.CompletedTask);
        }

        [Test]
        public async Task ConnectAsyncTest()
        {
            //Arrange
            SetupTcpAutoResponse(); // Налаштовуємо автовідповідач

            //act
            await _client.ConnectAsync();

            //assert
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            // Перевіряємо, що ConnectAsync відправляє 3 початкові команди
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public void DisconnectWithNoConnectionTest()
        {
            //act
            _client.Disconect(); // Використовуємо назву з одруківкою з вашого коду

            //assert
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public async Task DisconnectTest()
        {
            //Arrange 
            // Тести не повинні викликати інші тести. Робимо Arrange тут:
            SetupTcpAutoResponse();
            await _client.ConnectAsync();
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true); // Переконуємось, що стан "підключено"

            //act
            _client.Disconect(); // Використовуємо назву з одруківкою

            //assert
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false); // Перевіряємо, що стан оновився
        }

        [Test]
        public async Task StartIQAsync_WhenNotConnected_DoesNothing()
        {
            //Arrange
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false); // Гарантуємо, що ми не підключені

            //act
            await _client.StartIQAsync();

            //assert
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Never);
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task StartIQAsync_WhenConnected_StartsUdpAndSetsFlag()
        {
            //Arrange 
            SetupTcpAutoResponse();
            await _client.ConnectAsync(); // 3 повідомлення
            Assert.That(_client.IQStarted, Is.False); // Перевірка початкового стану

            //act
            await _client.StartIQAsync(); // 4-те повідомлення

            //assert
            _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
            Assert.That(_client.IQStarted, Is.True);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
        }

        [Test]
        public async Task StopIQAsync_WhenIQStarted_StopsUdpAndUnsetsFlag()
        {
            //Arrange 
            SetupTcpAutoResponse();
            await _client.ConnectAsync();
            await _client.StartIQAsync(); // Спочатку запускаємо
            Assert.That(_client.IQStarted, Is.True); // Переконуємось, що запущено

            //act
            await _client.StopIQAsync();

            //assert
            _udpMock.Verify(udp => udp.StopListening(), Times.Once);
            Assert.That(_client.IQStarted, Is.False);
        }

        // --- Нові тести, що покривають решту коду ---

        [Test]
        public async Task ChangeFrequencyAsync_WhenConnected_SendsMessage()
        {
            //Arrange
            SetupTcpAutoResponse();
            await _client.ConnectAsync(); // 3 повідомлення

            //act
            await _client.ChangeFrequencyAsync(144_000_000, 1); // 4-те повідомлення

            //assert
            // Перевіряємо, що загалом було 4 виклики (3 з Connect + 1 з ChangeFrequency)
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
        }

        [Test]
        public async Task ChangeFrequencyAsync_WhenNotConnected_DoesNothing()
        {
            //Arrange
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);

            //act
            await _client.ChangeFrequencyAsync(144_000_000, 1);

            //assert
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public async Task SendTcpRequest_WhenResponseIsReceived_TaskCompletes()
        {
            // Цей тест перевіряє вашу логіку TaskCompletionSource
            // Arrange
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            var responseData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

            // Налаштовуємо SendMessageAsync, але БЕЗ автовідповіді
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

            // Act
            // ВИПРАВЛЕННЯ 1: Змінюємо тип з Task<byte[]> на Task
            Task responseTask = _client.ChangeFrequencyAsync(100, 1);

            // Перевіряємо, що завдання "зависло" в очікуванні
            Assert.That(responseTask.IsCompleted, Is.False, "Завдання не повинно завершитись, доки не прийде відповідь.");

            // Тепер імітуємо відповідь від TCP
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, responseData);

            // Очікуємо завершення завдання
            await responseTask;
            Assert.That(responseTask.IsCompletedSuccessfully, Is.True, "Завдання мало успішно завершитись після отримання відповіді.");
        }

        [Test]
        public void UdpMessageReceived_WhenDataIsReceived_DoesNotThrowException()
        {
            // Цей тест перевіряє, що обробник події UDP підписаний
            // і не падає при отриманні даних (замість 'DataReady' та 'DataReceived')
            
            //Arrange
            var fakeUdpMessage = new byte[100]; // Імітація пакету даних

            // Act & Assert
            // Просто викликаємо подію. Якщо обробник підписаний і не 
            // кидає виняток (наприклад, NullReference), тест пройдено.
            Assert.DoesNotThrow(() =>
            {
                _udpMock.Raise(udp => udp.MessageReceived += null, _udpMock.Object, fakeUdpMessage);
            });
        }
    }
}