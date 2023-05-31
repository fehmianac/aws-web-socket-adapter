using Amazon.DynamoDBv2;
using Domain.Repositories;
using Infrastructure.Repositories.DynamoDb;

namespace Infrastructure.Factory;

public class RepositoryFactory
{
    public static IUserConnectionRepository CreateUserConnectionRepository()
    {
        var dynamoDb = new AmazonDynamoDBClient();
        return new UserConnectionRepository(dynamoDb);
    }
}