using NetArchTest.Rules;
using NUnit.Framework;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class ArchitectureTests
    {
        [Test]
        public void Messages_ShouldNot_DependOn_Networking()
        {
            var assembly = typeof(NetSdrClient).Assembly;

            var messagesNamespace = "NetSdrClientApp.Messages";
            var networkingNamespace = "NetSdrClientApp.Networking";

            // Act
            var result = Types.InAssembly(assembly)
                              .That()
                              .ResideInNamespace(messagesNamespace)
                              .ShouldNot()
                              .HaveDependencyOn(networkingNamespace)
                              .GetResult();

            // Assert
            Assert.IsTrue(result.IsSuccessful, "Domain logic (Messages) should not depend on Infrastructure (Networking)!");
        }
    }
}