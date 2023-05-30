using Amazon.SimpleSystemsManagement;
using Domain.Services.Contract;
using Infrastructure.Services;

namespace Infrastructure.Factory;

public class ServiceFactory
{
    public static ITokenService CreateTokenService()
    {
        return new JwtTokenService(new AwsSecretService(new AmazonSimpleSystemsManagementClient()));
    }
}