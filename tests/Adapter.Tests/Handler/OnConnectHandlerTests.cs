using System.Net;
using Adapter.Handler;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Entities;
using Domain.Repositories;
using Moq;
using Xunit;

namespace Adapter.Tests.Handler
{
    public class OnConnectHandlerTests
    {
        private readonly Mock<IUserConnectionRepository> _userConnectionRepositoryMock;
        private readonly OnConnectHandler _handler;

        public OnConnectHandlerTests()
        {
            _userConnectionRepositoryMock = new Mock<IUserConnectionRepository>();
            _handler = new OnConnectHandler(_userConnectionRepositoryMock.Object);
        }

        [Fact]
        public async Task Handler_ValidUserId_ReturnsConnectedResponse()
        {
            // Arrange
            var userId = "user1";
            var connectionId = "connection1";
            var request = new APIGatewayProxyRequest
            {
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    Authorizer  = new APIGatewayCustomAuthorizerContext{{"userId", userId}},
                    ConnectionId = connectionId
                }
            };
            var context = new Mock<ILambdaContext>();

            _userConnectionRepositoryMock.Setup(repo => repo.GetAsync(userId,CancellationToken.None)).ReturnsAsync((UserConnection)null);

            // Act
            var response = await _handler.Handler(request, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<UserConnection>(),CancellationToken.None), Times.Once);
            _userConnectionRepositoryMock.Verify(repo => repo.GetAsync(userId,CancellationToken.None), Times.Once);

            Assert.Equal("Connected", response.Body);
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Handler_InvalidUserId_ReturnsBadRequestResponse()
        {
            // Arrange
            var request = new APIGatewayProxyRequest();
            var context = new Mock<ILambdaContext>();

            // Act
            var response = await _handler.Handler(request, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<UserConnection>(),CancellationToken.None), Times.Never);

            Assert.Equal("invalid authorization", response.Body);
            Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        }
        
        [Fact]
        public async Task Should_Valid_Default_Ctor()
        {
            var handler = new OnConnectHandler();
        }
    }
}

