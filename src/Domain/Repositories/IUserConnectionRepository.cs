using Domain.Entities;

namespace Domain.Repositories;

public interface IUserConnectionRepository
{
    Task<UserConnection?> GetAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> SaveAsync(UserConnection userConnection, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);

    Task<List<string>> GetOnlineListAsync(List<string> userIds, CancellationToken cancellationToken = default);
    Task<List<string>> GetOnlineListAsync(CancellationToken cancellationToken = default);
    Task<bool> SaveLastActivityAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<UserLastActivity>> GetLastActivityAsync(List<string> userIds, CancellationToken cancellationToken = default);
}