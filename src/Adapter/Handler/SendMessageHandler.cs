using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Runtime;
using Domain.Domain;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Factory;

namespace Adapter.Handler;

public class SendMessageHandler
{
    private readonly IAmazonApiGatewayManagementApi _amazonApiGatewayManagementApi;
    private readonly IUserConnectionRepository _userConnectionRepository;

    public SendMessageHandler()
    {
        _amazonApiGatewayManagementApi = new AmazonApiGatewayManagementApiClient();
        _userConnectionRepository = RepositoryFactory.CreateUserConnectionRepository();
    }

    public async Task<SQSBatchResponse> Handler(SQSEvent @event, ILambdaContext context)
    {
        var response = new SQSBatchResponse();

        foreach (var message in @event.Records)
        {
            var messageDomain = JsonSerializer.Deserialize<MessageDomain>(message.Body);
            if (messageDomain == null)
            {
                continue;
            }

            var connectionIds = await _userConnectionRepository.GetAsync(messageDomain.UserId);
            if (connectionIds == null || !connectionIds.Any())
            {
                continue;
            }

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(messageDomain.Body));
            foreach (var connection in connectionIds)
            {
                await SendMessageToConnection(connection.UserId, connection.ConnectionId, stream);
            }
        }


        return response;
    }

    private async Task SendMessageToConnection(string userId, string connectionId, MemoryStream body)
    {
        try
        {
            await _amazonApiGatewayManagementApi.PostToConnectionAsync(new PostToConnectionRequest
            {
                ConnectionId = connectionId,
                Data = body
            });
        }
        catch (AmazonServiceException e)
        {
            if (e.StatusCode == HttpStatusCode.Gone)
            {
                await _userConnectionRepository.DeleteAsync(new UserConnection
                {
                    UserId = userId,
                    ConnectionId = connectionId
                });
            }
        }
    }
}