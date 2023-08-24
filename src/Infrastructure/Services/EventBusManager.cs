using System.Net;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Domain.Domain;
using Domain.Domain.Event;
using Domain.Services.Contract;

namespace Infrastructure.Services;

public class EventBusManager : IEventBusManager
{
    private readonly IAmazonSimpleNotificationService _amazonSimpleNotificationService;
    private readonly ISecretService _secretService;

    public EventBusManager(IAmazonSimpleNotificationService amazonSimpleNotificationService, ISecretService secretService)
    {
        _amazonSimpleNotificationService = amazonSimpleNotificationService;
        _secretService = secretService;
    }

    public async Task<bool> OnlineStatusChanged(string userId, bool onlineStatus, CancellationToken cancellationToken = default)
    {
        var eventBusArn = await _secretService.GetEventBusEndpoint(cancellationToken);
        var eventModel = new EventModel<OnlineStatusChangedEvent>
        {
            EventName = "OnlineStatusChanged",
            Data = new OnlineStatusChangedEvent
            {
                UserId = userId,
                IsOnline = onlineStatus
            }
        };
        var snsResponse = await _amazonSimpleNotificationService.PublishAsync(eventBusArn, JsonSerializer.Serialize(eventModel), cancellationToken);
        return snsResponse.HttpStatusCode is HttpStatusCode.OK or HttpStatusCode.Created;
    }
}