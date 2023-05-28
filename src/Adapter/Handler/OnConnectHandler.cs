using System.Net;
using Adapter.Extensions;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Factory;

namespace Adapter.Handler;

public class OnConnectHandler
{
    private readonly IUserConnectionRepository _userConnectionRepository;

    public OnConnectHandler()
    {
        _userConnectionRepository = RepositoryFactory.CreateUserConnectionRepository();
    }

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var userId = request.RequestContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new APIGatewayProxyResponse
            {
                Body = "invalid authorization",
                StatusCode = (int) HttpStatusCode.BadRequest
            };
        }

        await _userConnectionRepository.SaveAsync(new UserConnection
        {
            ConnectionId = request.RequestContext.ConnectionId,
            UserId = userId
        });

        return new APIGatewayProxyResponse
        {
            Body = "Connected",
            StatusCode = (int) HttpStatusCode.OK
        };
    }
}