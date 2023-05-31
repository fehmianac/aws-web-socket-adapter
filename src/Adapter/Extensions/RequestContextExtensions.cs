using Amazon.Lambda.APIGatewayEvents;

namespace Adapter.Extensions;

public static class RequestContextExtensions
{
    public static string? GetUserId(this APIGatewayProxyRequest.ProxyRequestContext? context)
    {
        if (context == null || context.Authorizer == null)
            return null;

        return context.Authorizer.TryGetValue("userId", out var value) ? value?.ToString() : context.Authorizer["userId"]?.ToString();
    }
}