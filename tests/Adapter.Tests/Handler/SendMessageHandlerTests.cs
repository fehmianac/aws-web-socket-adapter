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
        private readonly SendMessageHandler _sendMessageHandler;

        public SendMessageHandlerTests()
        {
            _amazonApiGatewayManagementApiMock = new Mock<IAmazonApiGatewayManagementApi>();
            _userConnectionRepositoryMock = new Mock<IUserConnectionRepository>();
            _sendMessageHandler = new SendMessageHandler(_amazonApiGatewayManagementApiMock.Object, _userConnectionRepositoryMock.Object);
        }

        [Fact]
        public async Task Handler_Should_Send_Messages_To_Connections()
        {
            // Arrange
            var message1 = new MessageDomain {UserId = "user1", Body = "Message 1"};
            var message2 = new MessageDomain {UserId = "user2", Body = "Message 2"};
            var message3 = new MessageDomain {UserId = "user1", Body = "Message 3"};

            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new() {Body = JsonSerializer.Serialize(message1)},
                    new() {Body = JsonSerializer.Serialize(message2)},
                    new() {Body = JsonSerializer.Serialize(message3)}
                }
            };

            var connection1 = new UserConnection {UserId = "user1", ConnectionId = "connection1"};
            var connection2 = new UserConnection {UserId = "user1", ConnectionId = "connection2"};
            var connection3 = new UserConnection {UserId = "user2", ConnectionId = "connection3"};

            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection1, connection2});
            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user2", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection3});

            _amazonApiGatewayManagementApiMock
                .Setup(a => a.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PostToConnectionResponse());

            // Act
            var response = await _sendMessageHandler.Handler(sqsEvent, new Mock<ILambdaContext>().Object);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.BatchItemFailures);
            
            _amazonApiGatewayManagementApiMock.Verify(a =>
                a.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        }
    }
}