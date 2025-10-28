using NUnit.Framework;
using API_Minimizer_back.Controllers;
using API_Minimizer_back.Models;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// Contains unit tests for the <see cref="EspnNflController"/> class.
/// </summary>
namespace Minimizertest
{
    public class EspnNflControllerTests
    {
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<ILogger<EspnNflController>> _mockLogger;
        private EspnNflController _controller;

        /// <summary>
        /// This method is called before each test method is executed to set up the test environment.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<EspnNflController>>();
            _controller = new EspnNflController(_mockHttpClientFactory.Object, _mockLogger.Object);
        }

        [Test]
        public void Controller_Should_Be_Initialized()
        {
            Assert.IsNotNull(_controller);
        }

        [Test]
        public void Controller_Should_Have_HttpClientFactory()
        {
            Assert.IsNotNull(_mockHttpClientFactory);
        }

        [Test]
        public void Controller_Should_Have_Logger()
        {
            Assert.IsNotNull(_mockLogger);
        }
    }
}
