using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Runtime;
using Domain.Domain;
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
        _amazonApiGatewayManagementApi = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = serviceUrl,
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

            Console.WriteLine(JsonSerializer.Serialize(messageDomain));
            var userConnection = await _userConnectionRepository.GetAsync(messageDomain.UserId);
            if (userConnection == null || !userConnection.Connections.Any())
            {
                continue;
            }

            List<string> oldConnections = new();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(messageDomain.Body));
            foreach (var connection in userConnection.Connections)
            {
                var sendMessageResponse = await SendMessageToConnection(connection.Id, stream);
                if (sendMessageResponse)
                {
                    continue;
                }

                oldConnections.Add(connection.Id);
            }

            if (!userConnection.Connections.Any())
            {
                await _userConnectionRepository.DeleteAsync(messageDomain.UserId);
            }

            if (!oldConnections.Any())
            {
                continue;
            } 
                
            userConnection.Connections.RemoveAll(q => oldConnections.Contains(q.Id));
            await _userConnectionRepository.SaveAsync(userConnection);
        }
        return response;
    }

    private async Task<bool> SendMessageToConnection(string connectionId, MemoryStream body)
    {
        try
        {
            await _amazonApiGatewayManagementApi.PostToConnectionAsync(new PostToConnectionRequest
            {
                ConnectionId = connectionId,
                Data = body
            });
            return true;
        }
        catch (AmazonServiceException e)
        {
            if (e.StatusCode == HttpStatusCode.Gone)
            {
                return false;
            }
        }

        return true;
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