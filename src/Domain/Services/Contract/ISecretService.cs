namespace Domain.Services.Contract;

public interface ISecretService
{
    Task<string> GetJwtSecret(CancellationToken cancellationToken = default);
    
    Task<string> GetEventBusEndpoint(CancellationToken cancellationToken = default);
}