using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Domain.Services.Contract;

namespace Infrastructure.Services;

public class AwsSecretService : ISecretService
{
    private readonly IAmazonSimpleSystemsManagement _amazonSimpleSystemsManagement;

    public AwsSecretService(IAmazonSimpleSystemsManagement amazonSimpleSystemsManagement)
    {
        _amazonSimpleSystemsManagement = amazonSimpleSystemsManagement;
    }

    public async Task<string> GetJwtSecret(CancellationToken cancellationToken = default)
    {
        var jwtSecretParameters = await _amazonSimpleSystemsManagement.GetParameterAsync(new GetParameterRequest
        {
            Name = "/WebSocketAdapter/JwtSecret",
        }, cancellationToken);
        return jwtSecretParameters.Parameter.Value;
    }
}