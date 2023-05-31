using Amazon.SimpleSystemsManagement;
using Infrastructure.Factory;
using Infrastructure.Services;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Factory
{
    public class ServiceFactoryTests
    {
        [Fact]
        public void CreateTokenService_Should_Return_JwtTokenService_Instance()
        {
            // Arrange
            var ssmClientMock = new Mock<IAmazonSimpleSystemsManagement>();

            // Act
            var result = ServiceFactory.CreateTokenService();

            // Assert
            Assert.IsType<JwtTokenService>(result);
        }
    }
}