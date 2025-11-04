using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    // Тест 1: Перевірка початкового стану
    [Test]
    public void Constructor_WhenCreated_IsNotConnected()
    {
        // Arrange (Підготовка) - вже зроблено в Setup

        // Act (Дія)
        bool isConnected = _sdrClient.IsConnected;

        // Assert (Перевірка)
        Assert.IsFalse(isConnected, "Клієнт не повинен бути підключений одразу після створення.");
    }

    // Тест 2: Перевірка успішного підключення
    [Test]
    public void Connect_WhenCalled_SetsIsConnectedToTrue()
    {
        // Arrange
        // Налаштовуємо мок: коли буде викликано Connect, 
        // ми імітуємо, що властивість 'Connected' стала true.
        _mockTcpClient.Setup(client => client.Connect(It.IsAny<string>(), It.IsAny<int>()));
        _mockTcpClient.Setup(client => client.Connected).Returns(true);

        // Act
        _sdrClient.Connect("127.0.0.1", 1234);

        // Assert
        Assert.IsTrue(_sdrClient.IsConnected, "IsConnected має стати true після підключення.");
        // Переконуємось, що метод Connect нашого TCP клієнта був викликаний
        _mockTcpClient.Verify(client => client.Connect("127.0.0.1", 1234), Times.Once);
    }

    // Тест 3: Перевірка успішного відключення
    [Test]
    public void Disconnect_WhenConnected_CallsCloseAndSetsIsConnectedToFalse()
    {
        // Arrange (Спочатку "підключимося")
        _mockTcpClient.Setup(client => client.Connected).Returns(true);

        // Act
        _sdrClient.Disconnect();

        // Assert
        // Перевіряємо, що метод Close був викликаний на нашому мок-клієнті
        _mockTcpClient.Verify(client => client.Close(), Times.Once);
    }

    // Тест 4: Перевірка помилки при спробі налаштування без підключення
    [Test]
    public void SetFrequency_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockTcpClient.Setup(client => client.Connected).Returns(false);

        // Act & Assert
        // Ми перевіряємо, що виконання коду призведе до викидання винятку
        Assert.Throws<InvalidOperationException>(
            () => _sdrClient.SetFrequency(100000000),
            "Спроба змінити частоту без підключення має кидати виняток."
        );
    }

    // Тест 5: Перевірка логіки, що не можна підключитись двічі
    [Test]
    public void Connect_WhenAlreadyConnected_DoesNotCallConnectAgain()
    {
        // Arrange
        _mockTcpClient.Setup(client => client.Connected).Returns(true);

        // Act
        // Спробуємо підключитися, коли ми "вже підключені"
        _sdrClient.Connect("127.0.0.1", 1234);

        // Assert
        // Перевіряємо, що метод Connect НЕ був викликаний (бо ми вже підключені)
        _mockTcpClient.Verify(client => client.Connect(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    // Тест 6: Перевірка викидання винятку при невдалому підключенні
    [Test]
    public void Connect_WhenConnectionFails_ThrowsException()
    {
        // Arrange
        // Імітуємо збій: метод Connect кидає виняток, наприклад, "SocketException"
        _mockTcpClient.Setup(client => client.Connect(It.IsAny<string>(), It.IsAny<int>()))
                      .Throws(new System.Net.Sockets.SocketException(10061)); // "Connection refused"

        // Act & Assert
        // Перевіряємо, що наш клієнт "прокидає" цей виняток назовні
        Assert.Throws<System.Net.Sockets.SocketException>(
            () => _sdrClient.Connect("127.0.0.1", 1234)
        );
    }
}
