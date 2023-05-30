using Domain.Domain;

namespace Domain.Services.Contract;

public interface ITokenService
{
    Task<UserDomain?> Verify(string token, CancellationToken cancellationToken = default!);
}