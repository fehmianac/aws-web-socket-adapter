using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Repositories;
using Infrastructure.Factory;

namespace Adapter.Handler.RestApi;

public class IsOnlineHandler
{
    private readonly IUserConnectionRepository _userConnectionRepository;

    public IsOnlineHandler()
    {
        _userConnectionRepository = RepositoryFactory.CreateUserConnectionRepository();
    }

    public IsOnlineHandler(IUserConnectionRepository userConnectionRepository)
    {
        _userConnectionRepository = userConnectionRepository;
    }

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        request.PathParameters.TryGetValue("userId", out var userId);

        if (string.IsNullOrEmpty(userId))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int) HttpStatusCode.NotFound
            };
        }

        var isOnline = await _userConnectionRepository.GetOnlineStatusAsync(userId);
        var response = new {status = "offline"};
        if (isOnline)
        {
            response = new {status = "online"};
        }

        return new APIGatewayProxyResponse
        {
            Body = JsonSerializer.Serialize(response),
            StatusCode = (int) HttpStatusCode.OK
        };
    }
}