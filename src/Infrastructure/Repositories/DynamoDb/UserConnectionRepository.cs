using Amazon.DynamoDBv2;
using Domain.Entities;
using Domain.Repositories;

namespace Infrastructure.Repositories.DynamoDb;

public class UserConnectionRepository : IUserConnectionRepository
{

    private readonly IAmazonDynamoDB _amazonDynamoDb;
    public UserConnectionRepository(IAmazonDynamoDB amazonDynamoDb)
    {
        _amazonDynamoDb = amazonDynamoDb;
    }
    public Task<List<UserConnection>?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SaveAsync(UserConnection userConnection, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(UserConnection userConnection, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}