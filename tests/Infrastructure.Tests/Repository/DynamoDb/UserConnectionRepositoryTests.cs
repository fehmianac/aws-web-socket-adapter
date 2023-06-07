using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Domain.Entities;
using Infrastructure.Repositories.DynamoDb;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Repository.DynamoDb
{
    public class UserConnectionRepositoryTests
    {
        private readonly Mock<IAmazonDynamoDB> _amazonDynamoDbMock;
        private readonly UserConnectionRepository _repository;

        public UserConnectionRepositoryTests()
        {
            _amazonDynamoDbMock = new Mock<IAmazonDynamoDB>();
            _repository = new UserConnectionRepository(_amazonDynamoDbMock.Object);
        }

        [Fact]
        public async Task GetAsync_ExistingUser_ReturnsUserConnection()
        {
            // Arrange
            var userId = "user1";
            var expectedConnections = new List<UserConnection.ConnectionInfo>
            {
                new UserConnection.ConnectionInfo { Id = "connection1", Time = DateTime.UtcNow }
            };
            var queryResponse = new QueryResponse
            {
                Items = new List<Dictionary<string, AttributeValue>>
                {
                    new Dictionary<string, AttributeValue>
                    {
                        { "sk", new AttributeValue { S = userId } },
                        { "connections", new AttributeValue { L = ToAttributeValueList(expectedConnections) } }
                    }
                }
            };
            _amazonDynamoDbMock
                .Setup(dynamoDb => dynamoDb.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResponse);

            // Act
            var result = await _repository.GetAsync(userId);

            // Assert
            _amazonDynamoDbMock.Verify(
                dynamoDb => dynamoDb.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(expectedConnections.Count, result.Connections.Count);
            Assert.Equal(expectedConnections[0].Id, result.Connections[0].Id);
            Assert.Equal(expectedConnections[0].Time, result.Connections[0].Time);
        }

        [Fact]
        public async Task GetAsync_NonExistingUser_ReturnsNull()
        {
            // Arrange
            var userId = "nonExistingUser";
            var queryResponse = new QueryResponse { Items = new List<Dictionary<string, AttributeValue>>() };
            _amazonDynamoDbMock
                .Setup(dynamoDb => dynamoDb.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(queryResponse);

            // Act
            var result = await _repository.GetAsync(userId);

            // Assert
            _amazonDynamoDbMock.Verify(
                dynamoDb => dynamoDb.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.Null(result);
        }

        [Fact]
        public async Task SaveAsync_ValidUserConnection_ReturnsTrue()
        {
            // Arrange
            var userConnection = new UserConnection
            {
                UserId = "user1",
                Connections = new List<UserConnection.ConnectionInfo>
                {
                    new UserConnection.ConnectionInfo { Id = "connection1", Time = DateTime.UtcNow }
                }
            };
            var putItemResponse = new PutItemResponse { HttpStatusCode = HttpStatusCode.OK };
            _amazonDynamoDbMock
                .Setup(dynamoDb => dynamoDb.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(putItemResponse);

            // Act
            var result = await _repository.SaveAsync(userConnection);

            // Assert
            _amazonDynamoDbMock.Verify(
                dynamoDb => dynamoDb.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var userId = "user1";
            var deleteItemResponse = new DeleteItemResponse { HttpStatusCode = HttpStatusCode.OK };
            _amazonDynamoDbMock
                .Setup(dynamoDb => dynamoDb.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteItemResponse);

            // Act
            var result = await _repository.DeleteAsync(userId);

            // Assert
            _amazonDynamoDbMock.Verify(
                dynamoDb => dynamoDb.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.True(result);
        }

        // Add more test methods to cover the other methods in UserConnectionRepository

        private List<AttributeValue> ToAttributeValueList(List<UserConnection.ConnectionInfo> connections)
        {
            var attributeValues = new List<AttributeValue>();
            foreach (var connection in connections)
            {
                attributeValues.Add(new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { S = connection.Id } },
                        { "time", new AttributeValue { S = connection.Time.ToString("O") } }
                    }
                });
            }
            return attributeValues;
        }
    }
}
