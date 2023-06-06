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
        var serviceUrl = Environment.GetEnvironmentVariable("API_GATEWAY_ENDPOINT");
        Console.WriteLine("API_GATEWAY_ENDPOINT: " + serviceUrl);
        _amazonApiGatewayManagementApi = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = serviceUrl
        });
        _userConnectionRepository = RepositoryFactory.CreateUserConnectionRepository();
    }

    public SendMessageHandler(IAmazonApiGatewayManagementApi amazonApiGatewayManagementApi, IUserConnectionRepository userConnectionRepository)
    {
        _amazonApiGatewayManagementApi = amazonApiGatewayManagementApi;
        _userConnectionRepository = userConnectionRepository;
    }

    public async Task<SQSBatchResponse> Handler(SQSEvent @event, ILambdaContext context)
    {
        var response = new SQSBatchResponse();

        foreach (var message in @event.Records)
        {
            var messageDomain = JsonDeserialize(message.Body);
            if (messageDomain == null)
            {
                continue;
            }

            var connectionIds = await _userConnectionRepository.GetAsync(messageDomain.UserId);
            if (!connectionIds.Any())
            {
                continue;
            }

            Console.WriteLine("connectionIds: " + JsonSerializer.Serialize(connectionIds));

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
            Console.WriteLine("SendMessageToConnection: " + connectionId + " - " + userId + " - " + body.Length + " bytes");
            await _amazonApiGatewayManagementApi.PostToConnectionAsync(new PostToConnectionRequest
            {
                ConnectionId = connectionId,
                Data = body
            });
            Console.WriteLine("SendMessageToConnection: " + connectionId + " - " + userId + " - " + body.Length + " bytes - success");
        }
        catch (AmazonServiceException e)
        {
            Console.WriteLine(e.Message);
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

    private static MessageDomain? JsonDeserialize(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<MessageDomain>(body);
        }
        catch (Exception)
        {
            return null;
        }
    }
}