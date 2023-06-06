using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
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

    public async Task<bool> SaveAsync(OnlineStatus userConnection, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = "userOnlineStatus"}},
                {"sk", new AttributeValue {S = userConnection.UserId}},
                {"connectedDate", new AttributeValue {S = userConnection.ConnectedDate.ToString("O")}}
            }
        };

        var response = await _amazonDynamoDb.PutItemAsync(request, cancellationToken);

        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public async Task<bool> DeleteAsync(OnlineStatus userConnection, CancellationToken cancellationToken = default)
    {
        var request = new DeleteItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = "userOnlineStatus"}},
                {"sk", new AttributeValue {S = userConnection.UserId}}
            }
        };
        var response = await _amazonDynamoDb.DeleteItemAsync(request, cancellationToken);
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public async Task<bool> GetOnlineStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest()
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = "userOnlineStatus"}},
                {"sk", new AttributeValue {S = userId}},
            }
        };

        var response = await _amazonDynamoDb.GetItemAsync(request, cancellationToken);
        return response.HttpStatusCode == HttpStatusCode.OK && response.Item.Count > 0;
    }

    public async Task<List<OnlineStatus>> GetOnlineStatusesAsync(CancellationToken cancellationToken = default)
    {
        var request = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "pk = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":pk", new AttributeValue {S = "userOnlineStatus"}}
            },
            ExclusiveStartKey = new Dictionary<string, AttributeValue>()
        };

        var userOnlineStatus = new List<OnlineStatus>();
        do
        {
            var response = await _amazonDynamoDb.QueryAsync(request, cancellationToken);
            if (!response.Items.Any())
                break;
            
            foreach (var item in response.Items)
            {
                userOnlineStatus.Add(new OnlineStatus
                {
                    UserId = item["sk"].S,
                    ConnectedDate = DateTime.Parse(item["connectedDate"].S)
                });
            }

            request.ExclusiveStartKey = response.LastEvaluatedKey;
        } while (request.ExclusiveStartKey.Count > 0);

        return userOnlineStatus;
    }
}