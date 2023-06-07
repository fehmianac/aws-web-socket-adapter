using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Domain.Entities;
using Domain.Repositories;

namespace Infrastructure.Repositories.DynamoDb;

public class UserConnectionRepository : IUserConnectionRepository
{
    private readonly string _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? "web_socket_adapter_table";
    private const string UserConnectionPk = "userConnections";
    private const string LastActivityPk = "lastActivity";
    private readonly IAmazonDynamoDB _amazonDynamoDb;

    public UserConnectionRepository(IAmazonDynamoDB amazonDynamoDb)
    {
        _amazonDynamoDb = amazonDynamoDb;
    }

    public async Task<UserConnection?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest()
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                {":pk", new AttributeValue {S = UserConnectionPk}},
                {":sk", new AttributeValue {S = userId}}
            }
        };

        try
        {
            var response = await _amazonDynamoDb.GetItemAsync(request, cancellationToken);

            var item = response.Item;
            if (item == null)
            {
                return null;
            }

            return new UserConnection
            {
                UserId = item["sk"].S,
                Connections = item["connections"].L.Select(q => new UserConnection.ConnectionInfo
                {
                    Id = q.M["id"].S,
                    Time = DateTime.Parse(q.M["time"].S)
                }).ToList()
            };
        }
        catch (AmazonDynamoDBException e)
        {
            return null;
        }
        
    }

    public async Task<bool> SaveAsync(UserConnection userConnection, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = UserConnectionPk}},
                {"sk", new AttributeValue {S = userConnection.UserId}},
                {
                    "connections", new AttributeValue
                    {
                        L = userConnection.Connections.Select(q => new AttributeValue
                        {
                            M = new Dictionary<string, AttributeValue>
                            {
                                {"id", new AttributeValue {S = q.Id}},
                                {"time", new AttributeValue {S = q.Time.ToString("O")}}
                            }
                        }).ToList()
                    }
                },
            }
        };

        var response = await _amazonDynamoDb.PutItemAsync(request, cancellationToken);
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = UserConnectionPk}},
                {"sk", new AttributeValue {S = userId}}
            }
        };
        var response = await _amazonDynamoDb.DeleteItemAsync(request, cancellationToken);
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public async Task<List<string>> GetOnlineListAsync(List<string> userIds, CancellationToken cancellationToken = default)
    {
        if (!userIds.Any())
        {
            return new List<string>();
        }

        var batchGetItemResponse = await _amazonDynamoDb.BatchGetItemAsync(new BatchGetItemRequest()
        {
            RequestItems = new Dictionary<string, KeysAndAttributes>
            {
                {
                    "requestedKeys", new KeysAndAttributes
                    {
                        Keys = userIds.Select(q => new Dictionary<string, AttributeValue>
                        {
                            {"pk", new AttributeValue {S = UserConnectionPk}},
                            {"sk", new AttributeValue {S = q}}
                        }).ToList(),
                        ProjectionExpression = "sk"
                    }
                }
            }
        }, cancellationToken);
        var items = batchGetItemResponse.Responses["requestedKeys"];
        return items.Select(q => q["sk"].S).ToList();
    }

    public async Task<List<string>> GetOnlineListAsync(CancellationToken cancellationToken = default)
    {
        var queryResponse = await _amazonDynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "pk = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":pk", new AttributeValue {S = UserConnectionPk}}
            },
        }, cancellationToken);
        return queryResponse.Items.Select(q => q["sk"].S).ToList();
    }

    public async Task<bool> SaveLastActivityAsync(string userId, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                {"pk", new AttributeValue {S = LastActivityPk}},
                {"sk", new AttributeValue {S = userId}},
                {"time", new AttributeValue {S = DateTime.UtcNow.ToString("O")}},
            }
        };

        var response = await _amazonDynamoDb.PutItemAsync(request, cancellationToken);

        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public async Task<List<UserLastActivity>> GetLastActivityAsync(List<string> userIds, CancellationToken cancellationToken = default)
    {
        if (!userIds.Any())
            return new List<UserLastActivity>();

        var batchGetItemResponse = await _amazonDynamoDb.BatchGetItemAsync(new BatchGetItemRequest()
        {
            RequestItems = new Dictionary<string, KeysAndAttributes>
            {
                {
                    "requestedKeys", new KeysAndAttributes
                    {
                        Keys = userIds.Select(q => new Dictionary<string, AttributeValue>
                        {
                            {"pk", new AttributeValue {S = LastActivityPk}},
                            {"sk", new AttributeValue {S = q}}
                        }).ToList()
                    }
                }
            }
        }, cancellationToken);

        var items = batchGetItemResponse.Responses["requestedKeys"];
        return items.Select(q => new UserLastActivity
        {
            Id = q["sk"].S,
            Time = DateTime.Parse(q["lastActivity"].S)
        }).ToList();
    }
}