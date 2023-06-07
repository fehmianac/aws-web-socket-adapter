using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Services.Contract;
using Infrastructure.Factory;


[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Adapter.Handler;

public class AuthorizerHandler
{
    private readonly ITokenService _tokenService;

    public AuthorizerHandler()
    {
        _tokenService = ServiceFactory.CreateTokenService();
    }

    public AuthorizerHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<APIGatewayCustomAuthorizerResponse> Handler(APIGatewayCustomAuthorizerRequest request, ILambdaContext? lambdaContext)
    {
        var token = request.QueryStringParameters["Authorization"];
        Console.WriteLine("token: " + token);
        var user = await _tokenService.Verify(token);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Unauthorized");
        }


        return new APIGatewayCustomAuthorizerResponse
        {
            PrincipalID = user.Id,
            Context = new APIGatewayCustomAuthorizerContextOutput() {{"userId", user.Id}},
            PolicyDocument = new APIGatewayCustomAuthorizerPolicy
            {
                Statement = new List<APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement>
                {
                    new()
                    {
                        Effect = "Allow",
                        Resource = new HashSet<string>
                        {
                            request.MethodArn
                        },
                        Action = new HashSet<string> {"execute-api:Invoke"}
                    }
                }
            }
        };
    }
}