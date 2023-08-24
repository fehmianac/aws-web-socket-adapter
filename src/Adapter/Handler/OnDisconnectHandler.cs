using System.Net;
using Adapter.Extensions;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services.Contract;
using Infrastructure.Factory;

namespace Adapter.Handler;

public class OnDisconnectHandler
{
    private readonly IUserConnectionRepository _userConnectionRepository;
    private readonly IEventBusManager _eventBusManager;

    public OnDisconnectHandler()
    {
        _userConnectionRepository = RepositoryFactory.CreateUserConnectionRepository();
        _eventBusManager = ServiceFactory.CreateEventBusService();
    }

    public OnDisconnectHandler(IUserConnectionRepository userConnectionRepository, IEventBusManager eventBusManager)
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

        var userConnection = await _userConnectionRepository.GetAsync(userId);
        if (userConnection == null)
        {
            return new APIGatewayProxyResponse
            {
                Body = "Disconnected",
                StatusCode = (int) HttpStatusCode.OK
            };
        }

        userConnection.Connections.RemoveAll(x => x.Id == request.RequestContext.ConnectionId);
        if (userConnection.Connections.Any())
        {
            await _userConnectionRepository.SaveAsync(userConnection);
        }
        else
        {
            await _userConnectionRepository.DeleteAsync(userId);
            await _userConnectionRepository.SaveLastActivityAsync(userId);
        }

        await _eventBusManager.OnlineStatusChanged(userId, false);

        return new APIGatewayProxyResponse
        {
            Body = "Disconnected",
            StatusCode = (int) HttpStatusCode.OK
        };
    }
}