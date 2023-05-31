using Amazon.DynamoDBv2;
using Infrastructure.Factory;
using Infrastructure.Repositories.DynamoDb;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Factory
{
    public class RepositoryFactoryTests
    {
        [Fact]
        public void CreateUserConnectionRepository_Should_Return_UserConnectionRepository_Instance()
        {
            // Arrange
            var dynamoDbMock = new Mock<IAmazonDynamoDB>();

            // Act
            var result = RepositoryFactory.CreateUserConnectionRepository();

            // Assert
            Assert.IsType<UserConnectionRepository>(result);
        }
    }
}