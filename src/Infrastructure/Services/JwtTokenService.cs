using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
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
        Console.WriteLine(jwtSecret);
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false
        };
        var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);
        if (!tokenValidationResult.IsValid)
        {
            Console.WriteLine("Token is invalid");
            return null;
        }

        Console.WriteLine(JsonSerializer.Serialize(tokenValidationResult.Claims));
        if (!tokenValidationResult.Claims.ContainsKey("userId"))
        {
            return null;
        }


        var userId = tokenValidationResult.Claims["userId"].ToString();

        if (string.IsNullOrEmpty(userId))
            return null;

        return new UserDomain(userId);
    }
}