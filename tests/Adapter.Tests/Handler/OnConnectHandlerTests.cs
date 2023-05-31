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
        private readonly OnConnectHandler _onConnectHandler;

        public OnConnectHandlerTests()
        {
            _userConnectionRepositoryMock = new Mock<IUserConnectionRepository>();
            _onConnectHandler = new OnConnectHandler(_userConnectionRepositoryMock.Object);
        }

        [Fact]
        public async Task Handler_Should_Return_BadRequest_Response_When_UserId_Is_NullOrEmpty()
        {
            // Arrange
            var request = new APIGatewayProxyRequest();
            var context = new Mock<ILambdaContext>().Object;

            // Act
            var response = await _onConnectHandler.Handler(request, context);

            // Assert
            Assert.Equal((int) HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("invalid authorization", response.Body);
            _userConnectionRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<UserConnection>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handler_Should_Save_UserConnection_And_Return_OK_Response_When_UserId_Is_Valid()
        {
            // Arrange
            var userId = "123";
            var connectionId = "456";
            var request = new APIGatewayProxyRequest
            {
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    ConnectionId = connectionId
                }
            };
            request.RequestContext.Authorizer = new APIGatewayCustomAuthorizerContext()
            {
                {"userId", userId}
            };
            var context = new Mock<ILambdaContext>().Object;

            // Act
            var response = await _onConnectHandler.Handler(request, context);

            // Assert
            Assert.Equal((int) HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Connected", response.Body);
            _userConnectionRepositoryMock.Verify(r => r.SaveAsync(
                It.Is<UserConnection>(uc => uc.UserId == userId && uc.ConnectionId == connectionId),It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task Should_Valid_Default_Ctor()
        {
            var handler = new OnConnectHandler();
        }
    }
    
}