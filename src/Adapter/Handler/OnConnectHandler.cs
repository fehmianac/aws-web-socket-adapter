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

    public OnConnectHandler(IUserConnectionRepository userConnectionRepository)
    {
        _userConnectionRepository = userConnectionRepository;
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

        var userConnection = await _userConnectionRepository.GetAsync(userId) ?? new UserConnection
        {
            UserId = userId,
            Connections = new List<UserConnection.ConnectionInfo>()
        };

        userConnection.Connections.Add(new UserConnection.ConnectionInfo
        {
            Id = request.RequestContext.ConnectionId,
            Time = DateTime.UtcNow
        });
        await _userConnectionRepository.SaveAsync(userConnection);

        return new APIGatewayProxyResponse
        {
            Body = "Connected",
            StatusCode = (int) HttpStatusCode.OK
        };
    }
}