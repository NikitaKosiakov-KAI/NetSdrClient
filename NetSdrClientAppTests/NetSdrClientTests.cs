using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Linq; 
using NetSdrClientApp.Messages; 

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private NetSdrClient _client;
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock; 

        private const int _maxDataMessageLength = 8192; 

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>(); 

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
            
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
        /// </summary>
        private void SetupTcpAutoResponse()
        {
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback(() =>
                {
                    _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, new byte[] { 0x01, 0x02 });
                })
                .Returns(Task.CompletedTask);
        }

        // --- ТЕСТИ ДЛЯ NetSdrClient (Вони вже проходили) ---
        
        [Test]
        public async Task ConnectAsyncTest()
        {
            SetupTcpAutoResponse();
            await _client.ConnectAsync();
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public void DisconnectWithNoConnectionTest()
        {
            _client.Disconect(); 
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public async Task DisconnectTest()
        {
            SetupTcpAutoResponse();
            await _client.ConnectAsync();
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            _client.Disconect(); 
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        }

        [Test]
        public async Task StartIQAsync_WhenNotConnected_DoesNothing()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            await _client.StartIQAsync();
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Never);
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task StartIQAsync_WhenConnected_StartsUdpAndSetsFlag()
        {
            SetupTcpAutoResponse();
            await _client.ConnectAsync();
            Assert.That(_client.IQStarted, Is.False);
            await _client.StartIQAsync();
            _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
            Assert.That(_client.IQStarted, Is.True);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
        }

        [Test]
        public async Task StopIQAsync_WhenIQStarted_StopsUdpAndUnsetsFlag()
        {
            SetupTcpAutoResponse();
            await _client.ConnectAsync();
            await _client.StartIQAsync();
            Assert.That(_client.IQStarted, Is.True);
            await _client.StopIQAsync();
            _udpMock.Verify(udp => udp.StopListening(), Times.Once);
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task ChangeFrequencyAsync_WhenConnected_SendsMessage()
        {
            SetupTcpAutoResponse();
            await _client.ConnectAsync(); 
            await _client.ChangeFrequencyAsync(144_000_000, 1); 
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
        }

        [Test]
        public async Task ChangeFrequencyAsync_WhenNotConnected_DoesNothing()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            await _client.ChangeFrequencyAsync(144_000_000, 1);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }
        
        [Test]
        public async Task SendTcpRequest_WhenResponseIsReceived_TaskCompletes()
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            var responseData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

            Task responseTask = _client.ChangeFrequencyAsync(100, 1);
            
            Assert.That(responseTask.IsCompleted, Is.False, "Завдання не повинно завершитись, доки не прийде відповідь.");
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, responseData);
            await responseTask;
            
            Assert.That(responseTask.IsCompletedSuccessfully, Is.True, "Завдання мало успішно завершитись після отримання відповіді.");
        }

        [Test]
        public void UdpMessageReceived_WithMalformedPacket_DoesNotThrowTypeMismatchException()
        {
            var fakeUdpMessage = new byte[100]; 
            Assert.DoesNotThrow(() =>
            {
                _udpMock.Raise(udp => udp.MessageReceived += null, _udpMock.Object, fakeUdpMessage);
            }, "Обробник UDP не повинен падати з ArgumentException на 'сміттєвих' даних після виправлення.");
        }

        // --- ТЕСТИ ДЛЯ NetSdrMessageHelper (З ВИПРАВЛЕННЯМИ) ---

        [Test]
        public void TranslateMessage_WithInvalidControlItemCode_ReturnsFalse()
        {
            // Arrange
            // ВИПРАВЛЕННЯ: Додано 'NetSdrMessageHelper.' перед 'MsgTypes'
            int typeInt = (int)NetSdrMessageHelper.MsgTypes.SetControlItem; 
            int length = 2;
            ushort headerNum = (ushort)(length + (typeInt << 13)); 
            byte[] header = BitConverter.GetBytes(headerNum);
            
            byte[] itemCodeBytes = BitConverter.GetBytes((ushort)65535); 
            byte[] message = header.Concat(itemCodeBytes).ToArray();

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(message, 
                out var type, out var code, out var seq, out var body);

            // Assert
            Assert.IsFalse(success);
        }
        
        [Test]
        public void GetSamples_WhenSampleSizeIsOver4_ThrowsArgumentOutOfRange()
        {
            // Arrange
            var body = new byte[100];
            int sampleSize = 5; 

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
            {
                // ВИПРАВЛЕННЯ: (CS1503) Приводимо 'int' до 'ushort', як того вимагає метод
                NetSdrMessageHelper.GetSamples((ushort)sampleSize, body).ToList();
            });
        }
        
        [Test]
        public void GetHeader_WhenMsgLengthIsNegative_ThrowsArgumentException()
        {
            // Arrange
            // ВИПРАВЛЕННЯ: Додано 'NetSdrMessageHelper.' перед 'MsgTypes'
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            int negativeLength = -1;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
            {
                // ВИПРАВЛЕННЯ: (CS0117) Тепер цей 'internal' метод буде видно
                NetSdrMessageHelper.GetHeader(type, negativeLength);
            }, "Message length exceeds allowed value");
        }
        
        [Test]
        public void TranslateHeader_WhenDataMessageLengthIsZero_SetsMaxDataLength()
        {
            // Arrange
            int msgLengthForMax = _maxDataMessageLength - 2; 
            
            // ВИПРАВЛЕННЯ: (CS0117) + (CS0103)
            byte[] header = NetSdrMessageHelper.GetHeader(NetSdrMessageHelper.MsgTypes.DataItem0, msgLengthForMax);

            // Act
            // ВИПРАВЛЕННЯ: (CS0117)
            NetSdrMessageHelper.TranslateHeader(header, out var outType, out var outLength);

            // Assert
            // ВИПРАВЛЕННЯ: (CS0103)
            Assert.AreEqual(NetSdrMessageHelper.MsgTypes.DataItem0, outType);
            Assert.AreEqual(_maxDataMessageLength, outLength);
        }
    }
}