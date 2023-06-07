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
    public class OnDisconnectHandlerTests
    {
        private readonly Mock<IUserConnectionRepository> _userConnectionRepositoryMock;
        private readonly OnDisconnectHandler _handler;

        public OnDisconnectHandlerTests()
        {
            _userConnectionRepositoryMock = new Mock<IUserConnectionRepository>();
            _handler = new OnDisconnectHandler(_userConnectionRepositoryMock.Object);
        }

        [Fact]
        public async Task Handler_ValidUserIdWithExistingConnections_RemovesConnectionAndUpdateUserConnection()
        {
            // Arrange
            var userId = "user1";
            var connectionId = "connection1";
            var request = new APIGatewayProxyRequest
            {
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    Authorizer = new APIGatewayCustomAuthorizerContext{{"userId", userId}},
                    ConnectionId = connectionId
                }
            };
            var context = new Mock<ILambdaContext>();

            var userConnection = new UserConnection
            {
                UserId = userId,
                Connections = new List<UserConnection.ConnectionInfo>
                {
                    new UserConnection.ConnectionInfo
                    {
                        Id = connectionId,
                        Time = DateTime.UtcNow
                    },
                    new UserConnection.ConnectionInfo
                    {
                        Id = "connection2",
                        Time = DateTime.UtcNow
                    }
                }
            };

            _userConnectionRepositoryMock.Setup(repo => repo.GetAsync(userId, CancellationToken.None)).ReturnsAsync(userConnection);

            // Act
            var response = await _handler.Handler(request, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.DeleteAsync(userId, CancellationToken.None), Times.Never);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveLastActivityAsync(userId, CancellationToken.None), Times.Never);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveAsync(userConnection, CancellationToken.None), Times.Once);

            Assert.Equal("Disconnected", response.Body);
            Assert.Equal((int) HttpStatusCode.OK, response.StatusCode);
            Assert.Single(userConnection.Connections);
            Assert.Equal("connection2", userConnection.Connections[0].Id);
        }

        [Fact]
        public async Task Handler_ValidUserIdWithNoConnections_DeletesUserConnectionAndUpdatesLastActivity()
        {
            // Arrange
            var userId = "user1";
            var connectionId = "connection1";
            var request = new APIGatewayProxyRequest
            {
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    Authorizer = new APIGatewayCustomAuthorizerContext{{"userId", userId}},
                    ConnectionId = connectionId
                }
            };
            var context = new Mock<ILambdaContext>();

            var userConnection = new UserConnection
            {
                UserId = userId,
                Connections = new List<UserConnection.ConnectionInfo>
                {
                    new UserConnection.ConnectionInfo
                    {
                        Id = connectionId,
                        Time = DateTime.UtcNow
                    }
                }
            };

            _userConnectionRepositoryMock.Setup(repo => repo.GetAsync(userId, CancellationToken.None)).ReturnsAsync(userConnection);

            // Act
            var response = await _handler.Handler(request, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.DeleteAsync(userId, CancellationToken.None), Times.Once);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveLastActivityAsync(userId, CancellationToken.None), Times.Once);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<UserConnection>(), CancellationToken.None), Times.Never);

            Assert.Equal("Disconnected", response.Body);
            Assert.Equal((int) HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(userConnection.Connections);
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
            _userConnectionRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<string>(), CancellationToken.None), Times.Never);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveLastActivityAsync(It.IsAny<string>(), CancellationToken.None), Times.Never);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<UserConnection>(), CancellationToken.None), Times.Never);

            Assert.Equal("invalid authorization", response.Body);
            Assert.Equal((int) HttpStatusCode.BadRequest, response.StatusCode);
        }

        
        
        [Fact]
        public async Task Handler_ThereIsNoConnectionForUser_ReturnOkResponse()
        {
            // Arrange
            var request = new APIGatewayProxyRequest
            {
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    Authorizer = new APIGatewayCustomAuthorizerContext
                    {
                        {"userId", "user1"}
                    }
                }
            };
            var context = new Mock<ILambdaContext>();

            _userConnectionRepositoryMock.Setup(q=> q.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((UserConnection)null);
            // Act
            var response = await _handler.Handler(request, context.Object);


            Assert.Equal("Disconnected", response.Body);
            Assert.Equal((int) HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Should_Valid_Default_Ctor()
        {
            var handler = new OnDisconnectHandler();
        }
    }
}