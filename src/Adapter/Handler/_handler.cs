using Amazon.Lambda.Core;

namespace Adapter.Handler;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

public class Handler
{
    
}