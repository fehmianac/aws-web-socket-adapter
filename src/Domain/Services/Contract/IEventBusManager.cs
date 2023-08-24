namespace Domain.Services.Contract;

public interface IEventBusManager
{
    Task<bool> OnlineStatusChanged(string userId, bool onlineStatus, CancellationToken cancellationToken = default);
}