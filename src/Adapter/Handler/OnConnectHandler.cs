using System.Net;
using System.Text.Json;
using Adapter.Extensions;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services.Contract;
using Infrastructure.Factory;

namespace Adapter.Handler;

public class OnConnectHandler
{
    private readonly IUserConnectionRepository _userConnectionRepository;
    private readonly IEventBusManager _eventBusManager;

    public OnConnectHandler()
    {
        _userConnectionRepository = RepositoryFactory.CreateUserConnectionRepository();
        _eventBusManager = ServiceFactory.CreateEventBusService();
    }

    public OnConnectHandler(IUserConnectionRepository userConnectionRepository, IEventBusManager eventBusManager)
    {
        _userConnectionRepository = userConnectionRepository;
        _eventBusManager = eventBusManager;
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

        Console.WriteLine(JsonSerializer.Serialize(userConnection));
        userConnection.Connections.Add(new UserConnection.ConnectionInfo
        {
            Id = request.RequestContext.ConnectionId,
            Time = DateTime.UtcNow
        });
        await _userConnectionRepository.SaveAsync(userConnection);
        await _eventBusManager.OnlineStatusChanged(userId, true);
        return new APIGatewayProxyResponse
        {
            Body = "Connected",
            StatusCode = (int) HttpStatusCode.OK
        };
    }
}