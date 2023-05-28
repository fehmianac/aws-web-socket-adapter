using Domain.Entities;

namespace Domain.Repositories;

public interface IUserConnectionRepository
{
    
    Task<List<UserConnection>?> GetAsync(string userId,CancellationToken cancellationToken = default);
    
    Task<bool> SaveAsync(UserConnection userConnection,CancellationToken cancellationToken = default);
    
    Task<bool> DeleteAsync(UserConnection userConnection,CancellationToken cancellationToken = default);
    
}