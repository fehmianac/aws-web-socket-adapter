using Adapter.Handler;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Domain;
using Domain.Services.Contract;
using Moq;
using Xunit;

namespace Adapter.Tests.Handler
{
    public class AuthorizerHandlerTests
    {
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly AuthorizerHandler _authorizerHandler;

        public AuthorizerHandlerTests()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _authorizerHandler = new AuthorizerHandler(_tokenServiceMock.Object);
        }

        [Fact]
        public async Task Handler_Should_Return_CustomAuthorizerResponse_With_PolicyDocument()
        {
            // Arrange
            var request = new APIGatewayCustomAuthorizerRequest
            {
                AuthorizationToken = "valid-token",
                MethodArn = "arn:aws:execute-api:region:account-id:api-id/stage/HTTP_METHOD/resource-path"
            };

            var user = new UserDomain("user1");
            _tokenServiceMock.Setup(t => t.Verify("valid-token",It.IsAny<CancellationToken>())).ReturnsAsync(user);

            // Act
            var response = await _authorizerHandler.Handler(request, new Mock<ILambdaContext>().Object);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("user1", response.PrincipalID);
            Assert.NotNull(response.Context);
            Assert.Equal("user1", response.Context["userId"]);
            Assert.NotNull(response.PolicyDocument);
            Assert.NotNull(response.PolicyDocument.Statement);
            Assert.Single(response.PolicyDocument.Statement);

            var statement = response.PolicyDocument.Statement[0];
            Assert.Equal("Allow", statement.Effect);
            Assert.NotNull(statement.Resource);
            Assert.Single(statement.Resource);
            Assert.NotNull(statement.Action);
            Assert.Single(statement.Action);

            _tokenServiceMock.Verify(t => t.Verify("valid-token",It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handler_Should_Throw_UnauthorizedAccessException_When_Invalid_Token()
        {
            // Arrange
            var request = new APIGatewayCustomAuthorizerRequest
            {
                AuthorizationToken = "invalid-token",
                MethodArn = "arn:aws:execute-api:region:account-id:api-id/stage/HTTP_METHOD/resource-path"
            };

            _tokenServiceMock.Setup(t => t.Verify("invalid-token",It.IsAny<CancellationToken>())).ReturnsAsync((UserDomain)null);

            // Act and Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authorizerHandler.Handler(request, new Mock<ILambdaContext>().Object));

            _tokenServiceMock.Verify(t => t.Verify("invalid-token",It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Should_Valid_Default_Ctor()
        {
            var authorizerHandler = new AuthorizerHandler();
        }
    }
}
