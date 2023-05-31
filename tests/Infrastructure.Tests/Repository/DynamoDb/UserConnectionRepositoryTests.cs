using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Autofac.Extras.Moq;
using AutoFixture;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Repositories.DynamoDb;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Repository.DynamoDb;

public class UserConnectionRepositoryTests
{
    private readonly IFixture _fixture;

    private readonly AutoMock _mock = AutoMock.GetStrict();
    private readonly Mock<IAmazonDynamoDB> _amazonDynamoDbMock;
    private readonly UserConnectionRepository _userConnectionRepository;

    public UserConnectionRepositoryTests()
    {
        _fixture = new Fixture();
        _amazonDynamoDbMock = _mock.Mock<IAmazonDynamoDB>();
        _userConnectionRepository = _mock.Create<UserConnectionRepository>();
    }

    [Fact]
    public async Task Should_GetAsync_Empty_When_There_Is_No_Record()
    {
        _amazonDynamoDbMock.Setup(q => q.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new QueryResponse {Items = new List<Dictionary<string, AttributeValue>>()});


        var userId = Guid.NewGuid().ToString();
        var userConnections = await _userConnectionRepository.GetAsync(userId, CancellationToken.None);

        _amazonDynamoDbMock.Verify(q => q.QueryAsync(It.Is<QueryRequest>(
                x => x.TableName == "web_socket_adapter_table" &&
                     x.KeyConditionExpression == "pk = :pk" &&
                     x.ExpressionAttributeValues.ContainsKey(":pk"))
            , It.IsAny<CancellationToken>()), Times.Once);

        Assert.Empty(userConnections);
    }

    [Fact]
    public async Task Should_Return_GetAsync_UserConnections()
    {
        // Arrange
        string userId = Guid.NewGuid().ToString();
        var queryResponse = new QueryResponse
        {
            Items = new List<Dictionary<string, AttributeValue>>
            {
                new()
                {
                    {"pk", new AttributeValue {S = userId}},
                    {"sk", new AttributeValue {S = "connection1"}}
                },
                new()
                {
                    {"pk", new AttributeValue {S = userId}},
                    {"sk", new AttributeValue {S = "connection2"}}
                }
            }
        };

        _amazonDynamoDbMock.Setup(q => q.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        // Act
        var userConnections = await _userConnectionRepository.GetAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(userConnections);
        Assert.Equal(2, userConnections.Count);

        Assert.Equal(userId, userConnections[0].UserId);
        Assert.Equal("connection1", userConnections[0].ConnectionId);

        Assert.Equal(userId, userConnections[1].UserId);
        Assert.Equal("connection2", userConnections[1].ConnectionId);

        _amazonDynamoDbMock.Verify(q => q.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    
    [Fact]
    public async Task SaveAsync_Should_Return_True_When_Save_Succeeds()
    {
        // Arrange
        var userConnection = new UserConnection
        {
            UserId = "123",
            ConnectionId = "connection1"
        };

        _amazonDynamoDbMock.Setup(p => p.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutItemResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        var result = await _userConnectionRepository.SaveAsync(userConnection, CancellationToken.None);

        // Assert
        Assert.True(result);
        _amazonDynamoDbMock.Verify(p => p.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_True_When_Delete_Succeeds()
    {
        // Arrange
        var userConnection = new UserConnection
        {
            UserId = "123",
            ConnectionId = "connection1"
        };

        _amazonDynamoDbMock.Setup(p => p.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteItemResponse {HttpStatusCode = HttpStatusCode.OK});

        // Act
        var result = await _userConnectionRepository.DeleteAsync(userConnection, CancellationToken.None);

        // Assert
        Assert.True(result);
        _amazonDynamoDbMock.Verify(p => p.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}