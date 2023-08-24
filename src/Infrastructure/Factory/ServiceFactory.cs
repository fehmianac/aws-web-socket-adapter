using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Domain.Services.Contract;
using Infrastructure.Services;

namespace Infrastructure.Factory;

public class ServiceFactory
{
    private static ISecretService CreteSecretService()
    {
        return new AwsSecretService(new AmazonSimpleSystemsManagementClient());
    }

    public static ITokenService CreateTokenService()
    {
        return new JwtTokenService(CreteSecretService());
    }

    public static IEventBusManager CreateEventBusService()
    {
        return new EventBusManager(new AmazonSimpleNotificationServiceClient(), CreteSecretService());
    }
}