using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Services.Contract;
using Infrastructure.Services;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Services
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _jwtTokenService;
        private readonly Mock<ISecretService> _secretServiceMock;
        private const string JwtSecret = "myJwtSecret1234KeyWebsocketAdapter";

        public JwtTokenServiceTests()
        {
            _secretServiceMock = new Mock<ISecretService>();
            _jwtTokenService = new JwtTokenService(_secretServiceMock.Object);
            
            _secretServiceMock.Setup(s => s.GetJwtSecret(It.IsAny<CancellationToken>()))
                .ReturnsAsync(JwtSecret);
        }

        [Fact]
        public async Task Verify_Should_Return_UserDomain_When_Token_Is_Valid()
        {
            // Arrange
            const string userId = "123";

            var token = GenerateValidToken(userId);
            // Act
            var result = await _jwtTokenService.Verify(token, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result?.Id);
            _secretServiceMock.Verify(s => s.GetJwtSecret(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task Verify_Should_Return_Null_When_Token_UserId_Claim_Is_Missing()
        {
            // Arrange
            const string userId = null;

            var token = GenerateValidToken(userId);
            // Act
            var result = await _jwtTokenService.Verify(token, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _secretServiceMock.Verify(s => s.GetJwtSecret(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        
        [Fact]
        public async Task Verify_Should_Return_Null_When_Token_UserId_Claim_Is_Empty()
        {
            // Arrange
            const string userId = "";

            var token = GenerateValidToken(userId);
            // Act
            var result = await _jwtTokenService.Verify(token, CancellationToken.None);

            // Assert
            Assert.Null(result);
            
            _secretServiceMock.Verify(s => s.GetJwtSecret(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Verify_Should_Return_Null_When_Token_Is_Invalid()
        {
            // Arrange
            const string invalidToken = "invalid_token";

            // Act
            var result = await _jwtTokenService.Verify(invalidToken, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _secretServiceMock.Verify(s => s.GetJwtSecret(It.IsAny<CancellationToken>()), Times.Once);
        }

        private static string GenerateValidToken(string? userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(JwtSecret);
            var claims = new ClaimsIdentity();
            if (userId != null)
            {
                claims.AddClaim(new Claim("userId", userId));
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            var jwtHandler = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(jwtHandler);

            return jwt;
        }
    }
}