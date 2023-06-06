using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using Domain.Entities;
using Domain.Repositories;

namespace Infrastructure.Repositories.DynamoDb;

public class UserConnectionRepository : IUserConnectionRepository
{
    private string TableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? "web_socket_adapter_table";
    private readonly IAmazonDynamoDB _amazonDynamoDb;

    public UserConnectionRepository(IAmazonDynamoDB amazonDynamoDb)
    {
        _amazonDynamoDb = amazonDynamoDb;
    }

    public async Task<List<UserConnection>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var request = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":pk", new AttributeValue {S = userId}}
            }
        };

        var response = await _amazonDynamoDb.QueryAsync(request, cancellationToken);

        var userConnections = new List<UserConnection>();
        foreach (var item in response.Items)
        {
            userConnections.Add(new UserConnection
            {
                UserId = item["pk"].S,
                ConnectionId = item["sk"].S
            });
        }

        return userConnections;
    }

    public async Task<bool> SaveAsync(UserConnection userConnection, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = userConnection.UserId}},
                {"sk", new AttributeValue {S = userConnection.ConnectionId}}
            }
        };

        var response = await _amazonDynamoDb.PutItemAsync(request, cancellationToken);

        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public async Task<bool> DeleteAsync(UserConnection userConnection, CancellationToken cancellationToken = default)
    {
        var request = new DeleteItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = userConnection.UserId}},
                {"sk", new AttributeValue {S = userConnection.ConnectionId}}
            }
        };
        var response = await _amazonDynamoDb.DeleteItemAsync(request, cancellationToken);
        return response.HttpStatusCode == HttpStatusCode.OK;
    }
}