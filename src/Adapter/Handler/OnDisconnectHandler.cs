using System.Net;
using Adapter.Extensions;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Factory;

namespace Adapter.Handler;

public class OnDisconnectHandler
{
    private readonly IUserConnectionRepository _userConnectionRepository;

    public OnDisconnectHandler()
    {
        _userConnectionRepository = RepositoryFactory.CreateUserConnectionRepository();
    }

    public OnDisconnectHandler(IUserConnectionRepository userConnectionRepository)
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

        return new APIGatewayProxyResponse
        {
            Body = "Disconnected",
            StatusCode = (int) HttpStatusCode.OK
        };
    }
}