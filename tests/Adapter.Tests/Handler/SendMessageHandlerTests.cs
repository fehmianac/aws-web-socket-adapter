using System.Net;
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

        [Fact]
        public async Task Handler_Should_Remove_Gone_Connection()
        {
            var message1 = new MessageDomain {UserId = "user1", Body = "Message 1"};
            var message2 = new MessageDomain {UserId = "user2", Body = "Message 2"};
            var message3 = new MessageDomain {UserId = "user3", Body = "Message 3"};

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
            var connection4 = new UserConnection {UserId = "user3", ConnectionId = "connection4"};

            _userConnectionRepositoryMock.Setup(q => q.DeleteAsync(It.Is<UserConnection>(q => q.ConnectionId == "connection4"), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection1, connection2});
            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user2", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection3});
            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user3", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection4});

            _amazonApiGatewayManagementApiMock.Setup(a => a.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PostToConnectionResponse());
            _amazonApiGatewayManagementApiMock.Setup(a => a.PostToConnectionAsync(It.Is<PostToConnectionRequest>(q => q.ConnectionId == "connection4"), It.IsAny<CancellationToken>())).ThrowsAsync(new GoneException("Gone")
            {
                StatusCode = HttpStatusCode.Gone
            });

            // Act
            var response = await _sendMessageHandler.Handler(sqsEvent, new Mock<ILambdaContext>().Object);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.BatchItemFailures);

            _amazonApiGatewayManagementApiMock.Verify(a =>
                a.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
            _userConnectionRepositoryMock.Verify(q => q.DeleteAsync(It.Is<UserConnection>(q => q.ConnectionId == "connection4" && q.UserId == "user3"), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task Should_Ignore_When_MessageDomain_Not_Deserialize()
        {
            var message1 = new MessageDomain {UserId = "user1", Body = "Message 1"};
            var message3 = new MessageDomain {UserId = "user3", Body = "Message 3"};

            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new() {Body = JsonSerializer.Serialize(message1)},
                    new() {Body = JsonSerializer.Serialize("message2")},
                    new() {Body = JsonSerializer.Serialize(message3)}
                }
            };

            var connection1 = new UserConnection {UserId = "user1", ConnectionId = "connection1"};
            var connection2 = new UserConnection {UserId = "user1", ConnectionId = "connection2"};
            var connection3 = new UserConnection {UserId = "user2", ConnectionId = "connection3"};
            var connection4 = new UserConnection {UserId = "user3", ConnectionId = "connection4"};

            _userConnectionRepositoryMock.Setup(q => q.DeleteAsync(It.Is<UserConnection>(q => q.ConnectionId == "connection4"), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection1, connection2});
            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user2", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection3});
            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user3", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection4});

            _amazonApiGatewayManagementApiMock.Setup(a => a.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PostToConnectionResponse());
            _amazonApiGatewayManagementApiMock.Setup(a => a.PostToConnectionAsync(It.Is<PostToConnectionRequest>(q => q.ConnectionId == "connection4"), It.IsAny<CancellationToken>())).ThrowsAsync(new GoneException("Gone")
            {
                StatusCode = HttpStatusCode.Gone
            });

            // Act
            var response = await _sendMessageHandler.Handler(sqsEvent, new Mock<ILambdaContext>().Object);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.BatchItemFailures);

            _amazonApiGatewayManagementApiMock.Verify(a =>
                a.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _userConnectionRepositoryMock.Verify(q => q.DeleteAsync(It.Is<UserConnection>(q => q.ConnectionId == "connection4" && q.UserId == "user3"), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        
              
        [Fact]
        public async Task Should_Ignore_When_There_Is_No_Connection_Id()
        {
            var message1 = new MessageDomain {UserId = "user1", Body = "Message 1"};
            var message3 = new MessageDomain {UserId = "user3", Body = "Message 3"};

            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new() {Body = JsonSerializer.Serialize(message1)},
                    new() {Body = JsonSerializer.Serialize(message3)}
                }
            };

            var connection1 = new UserConnection {UserId = "user1", ConnectionId = "connection1"};
            var connection2 = new UserConnection {UserId = "user1", ConnectionId = "connection2"};
            var connection3 = new UserConnection {UserId = "user2", ConnectionId = "connection3"};

            _userConnectionRepositoryMock.Setup(q => q.DeleteAsync(It.Is<UserConnection>(q => q.ConnectionId == "connection4"), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user1", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection1, connection2});
            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user2", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection> {connection3});
            _userConnectionRepositoryMock.Setup(r => r.GetAsync("user3", It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConnection>());
            

            _amazonApiGatewayManagementApiMock.Setup(a => a.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PostToConnectionResponse());

            // Act
            var response = await _sendMessageHandler.Handler(sqsEvent, new Mock<ILambdaContext>().Object);

            // Assert
            Assert.NotNull(response);
            Assert.Empty(response.BatchItemFailures);

            _amazonApiGatewayManagementApiMock.Verify(a =>
                a.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            
        }
        
        [Fact]
        public async Task Should_Valid_Default_Ctor()
        {
            var handler = new SendMessageHandler();
        }
    }
}