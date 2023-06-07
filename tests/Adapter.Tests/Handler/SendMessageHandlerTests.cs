using System.Text;
using System.Text.Json;
using Adapter.Handler;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Domain.Domain;
using Domain.Entities;
using Domain.Repositories;
using Moq;
using Xunit;

namespace Adapter.Tests.Handler
{
    public class SendMessageHandlerTests
    {
        private readonly Mock<IAmazonApiGatewayManagementApi> _amazonApiGatewayManagementApiMock;
        private readonly Mock<IUserConnectionRepository> _userConnectionRepositoryMock;
        private readonly SendMessageHandler _handler;

        public SendMessageHandlerTests()
        {
            _amazonApiGatewayManagementApiMock = new Mock<IAmazonApiGatewayManagementApi>();
            _userConnectionRepositoryMock = new Mock<IUserConnectionRepository>();
            _handler = new SendMessageHandler(
                _amazonApiGatewayManagementApiMock.Object,
                _userConnectionRepositoryMock.Object
            );
        }

        [Fact]
        public async Task Handler_ValidMessageWithExistingUserConnections_SendsMessageToConnections()
        {
            // Arrange
            var userId = "user1";
            var connectionId1 = "connection1";
            var connectionId2 = "connection2";
            var messageBody = "Test message";
            var message = new SQSEvent.SQSMessage
            {
                Body = JsonSerializer.Serialize(new MessageDomain
                {
                    UserId = userId,
                    Body = messageBody
                })
            };
            var context = new Mock<ILambdaContext>();

            var userConnection = new UserConnection
            {
                UserId = userId,
                Connections = new List<UserConnection.ConnectionInfo>
                {
                    new UserConnection.ConnectionInfo
                    {
                        Id = connectionId1,
                        Time = DateTime.UtcNow
                    },
                    new UserConnection.ConnectionInfo
                    {
                        Id = connectionId2,
                        Time = DateTime.UtcNow
                    }
                }
            };

            _userConnectionRepositoryMock.Setup(repo => repo.GetAsync(userId,CancellationToken.None)).ReturnsAsync(userConnection);
            _amazonApiGatewayManagementApiMock
                .Setup(api => api.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(),CancellationToken.None))
                .Returns(Task.FromResult(new PostToConnectionResponse()));

            // Act
            var response = await _handler.Handler(new SQSEvent { Records = new List<SQSEvent.SQSMessage> { message } }, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<string>(),CancellationToken.None), Times.Never);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<UserConnection>(),CancellationToken.None), Times.Never);
            
            _amazonApiGatewayManagementApiMock.Verify(api =>
                api.PostToConnectionAsync(It.Is<PostToConnectionRequest>(req =>
                    req.ConnectionId == connectionId1 && req.Data.ToArray().SequenceEqual(Encoding.UTF8.GetBytes(messageBody)))
                ,CancellationToken.None), Times.Once);
            
            _amazonApiGatewayManagementApiMock.Verify(api =>
                api.PostToConnectionAsync(It.Is<PostToConnectionRequest>(req =>
                    req.ConnectionId == connectionId2 && req.Data.ToArray().SequenceEqual(Encoding.UTF8.GetBytes(messageBody)))
                ,CancellationToken.None), Times.Once);

        }

        [Fact]
        public async Task Handler_ValidMessageWithNoUserConnections_IgnoresMessage()
        {
            // Arrange
            var userId = "user1";
            var messageBody = "Test message";
            var message = new SQSEvent.SQSMessage
            {
                Body = JsonSerializer.Serialize(new MessageDomain
                {
                    UserId = userId,
                    Body = messageBody
                })
            };
            var context = new Mock<ILambdaContext>();

            var userConnection = new UserConnection
            {
                UserId = userId,
                Connections = new List<UserConnection.ConnectionInfo>()
            };

            _userConnectionRepositoryMock.Setup(repo => repo.GetAsync(userId,CancellationToken.None)).ReturnsAsync(userConnection);

            // Act
            var response = await _handler.Handler(new SQSEvent { Records = new List<SQSEvent.SQSMessage> { message } }, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<string>(),CancellationToken.None), Times.Never);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<UserConnection>(),CancellationToken.None), Times.Never);
            _amazonApiGatewayManagementApiMock.Verify(api =>
                api.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(),CancellationToken.None), Times.Never);

        }

        [Fact]
        public async Task Handler_InvalidMessage_IgnoresMessage()
        {
            // Arrange
            var message = new SQSEvent.SQSMessage
            {
                Body = "Invalid message"
            };
            var context = new Mock<ILambdaContext>();

            // Act
            var response = await _handler.Handler(new SQSEvent { Records = new List<SQSEvent.SQSMessage> { message } }, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<string>(),CancellationToken.None), Times.Never);
            _userConnectionRepositoryMock.Verify(repo => repo.SaveAsync(It.IsAny<UserConnection>(),CancellationToken.None), Times.Never);
            _amazonApiGatewayManagementApiMock.Verify(api =>
                api.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(),CancellationToken.None), Times.Never);

        }
        
        [Fact]
        public async Task Should_Valid_Default_Ctor()
        {
            Environment.SetEnvironmentVariable("API_GATEWAY_ENDPOINT","http://localhost:4566");
            var handler = new SendMessageHandler();
        }
    }
}
