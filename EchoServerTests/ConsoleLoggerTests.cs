using System;
using System.IO;
using EchoTspServer;
using NUnit.Framework;

namespace EchoServerTests
{
    [TestFixture]
    public class ConsoleLoggerTests
    {
        private StringWriter _stringWriter = null!;
        private TextWriter _originalConsoleOut = null!;
        private ConsoleLogger _logger = null!;

        [SetUp]
        public void SetUp()
        {
            _originalConsoleOut = Console.Out;  
            _stringWriter = new StringWriter();
            Console.SetOut(_stringWriter);

            _logger = new ConsoleLogger();
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalConsoleOut);
        }

        [Test]
        public void Log_WritesInfoMessage()
        {
            _logger.Log("Test message");

            string output = _stringWriter.ToString();

            Assert.IsTrue(output.Contains("[INFO] Test message"),
                "Log() should write an info message.");
        }

        [Test]
        public void LogError_WritesErrorMessage()
        {
            _logger.LogError("Critical failure");

            string output = _stringWriter.ToString();

            Assert.IsTrue(output.Contains("[ERROR] Critical failure"),
                "LogError() should write an error message.");
        }
    }
}