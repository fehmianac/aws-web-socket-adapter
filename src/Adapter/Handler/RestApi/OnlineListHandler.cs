using System.Net;
using System.Text.Json;
using Adapter.Models.Response;
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
        var userIds = request.QueryStringParameters?["userIds"]?.Split(",");

        if (userIds == null)
        {
            var onlineUsers = await _userConnectionRepository.GetOnlineListAsync();
            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(onlineUsers.Select(q => new OnlineStatusResponseModel
                {
                    UserId = q,
                    IsOnline = true,
                    LastActivity = DateTime.UtcNow
                })),
                StatusCode = (int) HttpStatusCode.OK
            };
        }
        
        var onlineList = await _userConnectionRepository.GetOnlineListAsync(userIds.ToList());
        var notOnlineUsers = userIds.Where(q => !onlineList.Contains(q)).ToList();
        var lastActivity = await _userConnectionRepository.GetLastActivityAsync(notOnlineUsers);
        
        var response = onlineList.Select(q => new OnlineStatusResponseModel
        {
            UserId = q,
            IsOnline = true,
            LastActivity = DateTime.UtcNow
        }).ToList();
        
        response.AddRange(lastActivity.Select(q => new OnlineStatusResponseModel
        {
            UserId = q.Id,
            IsOnline = false,
            LastActivity = q.Time
        }));
        
        return new APIGatewayProxyResponse
        {
            Body = JsonSerializer.Serialize(response),
            StatusCode = (int) HttpStatusCode.OK
        };
        
    }
}