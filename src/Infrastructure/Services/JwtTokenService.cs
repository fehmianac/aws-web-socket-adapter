using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Domain.Domain;
using Domain.Services.Contract;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class JwtTokenService : ITokenService
{
    private readonly ISecretService _secretService;

    public JwtTokenService(ISecretService secretService)
    {
        _secretService = secretService;
    }

    public async Task<UserDomain?> Verify(string token, CancellationToken cancellationToken = default)
    {
        var jwtSecret = await _secretService.GetJwtSecret(cancellationToken);
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
        var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);
        if (!tokenValidationResult.IsValid)
        {
            return null;
        }

        if (!tokenValidationResult.Claims.ContainsKey("userId"))
            return null;

        var userId = tokenValidationResult.Claims["userId"].ToString();

        if (string.IsNullOrEmpty(userId))
            return null;

        return new UserDomain(userId);
    }
}