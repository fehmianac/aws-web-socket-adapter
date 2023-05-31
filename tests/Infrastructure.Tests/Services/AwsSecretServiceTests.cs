using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Infrastructure.Services;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Services;

public class AwsSecretServiceTests
{
    private readonly AwsSecretService _awsSecretService;
    private readonly Mock<IAmazonSimpleSystemsManagement> _amazonSsmMock;

    public AwsSecretServiceTests()
    {
        _amazonSsmMock = new Mock<IAmazonSimpleSystemsManagement>();
        _awsSecretService = new AwsSecretService(_amazonSsmMock.Object);
    }

    [Fact]
    public async Task GetJwtSecret_Should_Return_JwtSecret_From_AmazonSsm()
    {
        // Arrange
        var jwtSecretParameter = new Parameter
        {
            Value = "myJwtSecret"
        };

        _amazonSsmMock.Setup(s => s.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetParameterResponse
            {
                Parameter = jwtSecretParameter
            });

        // Act
        var result = await _awsSecretService.GetJwtSecret(CancellationToken.None);

        // Assert
        Assert.Equal(jwtSecretParameter.Value, result);
        _amazonSsmMock.Verify(s => s.GetParameterAsync(It.IsAny<GetParameterRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
