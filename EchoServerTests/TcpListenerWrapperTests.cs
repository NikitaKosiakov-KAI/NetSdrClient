using EchoTspServer;
using NUnit.Framework;

namespace EchoServerTests
{
    [TestFixture]
    public class TcpListenerWrapperTests
    {
        [Test, Timeout(2000)]

        public void DefaultTcpListener_StartStop_DoesNotThrow()
        {
            var listener = new DefaultTcpListener(0); // порт 0 → ОС вибере вільний

            Assert.DoesNotThrow(() =>
            {
                listener.Start();
                listener.Stop();
            });
        }
    }
    
}