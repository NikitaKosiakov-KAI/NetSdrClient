using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(actualCode, Is.EqualTo((short)code));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void TranslateMessage_WithInvalidControlItemCode_ReturnsFalse()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem; 
            ushort length = 2; // Довжина тіла (2 байти для коду)
            
            ushort headerVal = (ushort)(length + ((int)type << 13));
            byte[] header = BitConverter.GetBytes(headerVal);

            ushort invalidCode = ushort.MaxValue; 
            byte[] body = BitConverter.GetBytes(invalidCode);

            byte[] message = header.Concat(body).ToArray();

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(message, 
                out var outType, out var outCode, out var outSeq, out var outBody);

            // Assert
            Assert.IsFalse(success, "Метод має повернути false для невідомого ControlItemCode");
        }

        [Test]
        public void TranslateMessage_WithDataItem_ParsesSequenceNumber()
        {
            // Arrange
            // Використовуємо DataItem0, щоб умову (type < MsgTypes.DataItem0) стала false.
            // Це перекине виконання на рядок 97.
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            
            // Тестовий номер послідовності (Sequence Number)
            ushort expectedSeqNum = 12345;
            byte[] seqBytes = BitConverter.GetBytes(expectedSeqNum);
            
            // Якісь дані (тіло повідомлення)
            byte[] dataBody = new byte[] { 0xAA, 0xBB, 0xCC }; 

            // Довжина повідомлення = 2 байти (Sequence) + 3 байти (Data) = 5
            ushort msgLength = (ushort)(seqBytes.Length + dataBody.Length);

            // Формуємо заголовок
            ushort headerVal = (ushort)(msgLength + ((int)type << 13));
            byte[] header = BitConverter.GetBytes(headerVal);

            // Збираємо повне повідомлення: Header + Sequence + Data
            byte[] message = header.Concat(seqBytes).Concat(dataBody).ToArray();

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(message, 
                out var outType, out var outCode, out var outSeq, out var outBody);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(NetSdrMessageHelper.MsgTypes.DataItem0, outType);
            // Це покриває рядок 98: sequenceNumber = ...
            Assert.AreEqual(expectedSeqNum, outSeq, "Sequence number має бути розпаршений коректно");
            Assert.AreEqual(dataBody, outBody, "Тіло повідомлення має збігатися");
        }
    }
}