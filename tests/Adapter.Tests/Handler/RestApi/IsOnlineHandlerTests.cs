using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Adapter.Handler.RestApi;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Repositories;
using Moq;
using Xunit;

namespace Adapter.Handler.RestApi.Tests
{
    public class IsOnlineHandlerTests
    {
        private readonly LastActivityListHandler _handler;
        private readonly Mock<IUserConnectionRepository> _userConnectionRepositoryMock;

        public IsOnlineHandlerTests()
        {
            _userConnectionRepositoryMock = new Mock<IUserConnectionRepository>();
            _handler = new LastActivityListHandler(_userConnectionRepositoryMock.Object);
        }

        [Fact]
        public async Task Handler_WithEmptyUserId_ReturnsNotFoundResponse()
        {
            // Arrange
            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>()
            };
            var context = new Mock<ILambdaContext>().Object;

            // Act
            var response = await _handler.Handler(request, context);

            // Assert
            Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Handler_WithOfflineStatus_ReturnsOfflineResponse()
        {
            // Arrange
            var userId = "testUser";
            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "userId", userId }
                }
            };
            var context = new Mock<ILambdaContext>().Object;
            _userConnectionRepositoryMock.Setup(repo => repo.GetOnlineStatusAsync(userId,CancellationToken.None)).ReturnsAsync(false);

            // Act
            var response = await _handler.Handler(request, context);
            var responseBody = JsonSerializer.Deserialize<dynamic>(response.Body);

            // Assert
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("", responseBody.status);
        }

        [Fact]
        public async Task Handler_WithOnlineStatus_ReturnsOnlineResponse()
        {
            // Arrange
            var userId = "testUser";
            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "userId", userId }
                }
            };
            var context = new Mock<ILambdaContext>().Object;
            _userConnectionRepositoryMock.Setup(repo => repo.GetOnlineStatusAsync(userId,CancellationToken.None)).ReturnsAsync(true);

            // Act
            var response = await _handler.Handler(request, context);
            var responseBody = JsonSerializer.Deserialize<dynamic>(response.Body);

            // Assert
            Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("online", responseBody.status);
        }
    }
}
