using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Repositories;
using Infrastructure.Factory;

namespace Adapter.Handler.RestApi;

public class OnlineListHandler
{
    private readonly IUserConnectionRepository _userConnectionRepository;

    public OnlineListHandler()
    {
        _userConnectionRepository = RepositoryFactory.CreateUserConnectionRepository();
    }

    public OnlineListHandler(IUserConnectionRepository userConnectionRepository)
    {
        _userConnectionRepository = userConnectionRepository;
    }

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var onlineUsers = await _userConnectionRepository.GetOnlineStatusesAsync();
        return new APIGatewayProxyResponse
        {
            Body = JsonSerializer.Serialize(onlineUsers),
            StatusCode = (int) HttpStatusCode.OK
        };
    }
}