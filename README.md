# AWS API Gateway WebSocket Integration Manager

The AWS API Gateway WebSocket Integration Manager is a C# project that facilitates the management of AWS API Gateway WebSocket integrations for other projects. It provides a one-click deployment process, allowing users to quickly set up and use the project in their own AWS accounts.

## Installation

To install the AWS API Gateway WebSocket Integration Manager, follow these steps:

1. Create an S3 bucket in your AWS account to store the deployment package.
2. Upload the `deploy.zip` file to the S3 bucket. This package contains the necessary files for deployment.
3. Deploy the dummy source code to AWS Lambda by executing the CloudFormation template provided in `template.yaml`. This sets up the required Lambda functions for the project.

## Usage

The AWS API Gateway WebSocket Integration Manager consists of the following Lambda functions:

1. **WebSocketAdaptorOnConnectFunction**: This function is triggered when a client attempts to connect to the WebSocket. It stores the connection ID and user ID in DynamoDB.

2. **WebSocketAdaptorOnDisconnectFunction**: This function is triggered when a client disconnects from the WebSocket. It deletes the active connection ID for the user and logs the last activity date.

3. **WebSocketAdaptorSendMessageFunction**: This function listens to an SQS queue for sending socket messages to clients. The message structure is defined as follows:

```csharp
public class MessageDomain
{
    public string UserId { get; set; }
    
    public string Body { get; set; }
}
```

The `WebSocketAdaptorSendMessageFunction` requires the `SendMessageQueueUrl` parameter to be set. You can configure the `SendMessageQueueUrl` by providing the parameter name `/WebSocketAdapter/SendMessageQueueUrl`.

4. **WebSocketAdaptorAuthorizerFunction**: This function handles the authorization process for connecting to the WebSocket using JSON Web Tokens (JWT). The JWT secret is stored in the Parameter Store with the name `/WebSocketAdapter/JwtSecret`. The JWT must contain a `userId` claim for successful authorization.

5. **WebSocketAdaptorRestOnlineListFunction**: This function provides online status or last activity date for users. When called without any parameters, it returns a list of online users. When `userIds` parameter is provided via the query string, the function returns the online status for the requested user IDs.

After deploying the project, two API Gateway instances will be created: one for WebSocket communication and another for HTTP API. You can obtain the URLs for these instances from the AWS Management Console.

## Infrastructure Setup

The infrastructure setup for the AWS API Gateway WebSocket Integration Manager is provided as a CloudFormation template (`template.yaml`). You can use this template for the initial setup of the infrastructure.

## Infrastructure Diagram

![infra.png](docs%2Finfra.png)

## Deployment Pipelines

Example deployment pipelines using GitHub Actions are available in this repository. You can refer to these pipelines to set up your own CI/CD workflows for the project.

## Example API Requests

Here are some example cURL commands for making API requests to the deployed endpoints:

1. **Get online status for specific users**:

```shell
curl -X GET "https://your-api-gateway-url/user/online?userIds=1234" -H "Authorization: Bearer <your_jwt_token>"
```

Replace `https://your-api-gateway-url` with the actual URL of your deployed API Gateway WebSocket instance.

2. **WebSocket Connection**:

```shell
wscat -c "wss://your-socket-url?Authorization=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwidXNlcklkIjoiMTIzNCIsImlhdCI6MjAxNjIzOTAyMiwiZXhwIjoyMDE2MjM5MDIyfQ.6C7xpaC9_vmadA72tABVkqXH9HWcPY4RE8dGALnT-Hw"
```

Make sure to replace `your-socket-url` with the actual WebSocket URL of your deployed API Gateway instance and `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwidXNlcklkIjoiMTIzNCIsImlhdCI6MjAxNjIzOTAyMiwiZXhwIjoyMDE2MjM5MDIyfQ.6C7xpaC9_vmadA72tABVkqXH9HWcPY4RE8dGALnT-Hw` with your actual JWT token.

Please note that the `wscat` tool is used to establish the WebSocket connection, and you may need to install it separately if you haven't already.

Let me know if you have any further questions!

Ensure you have the `wscat` tool installed to establish a WebSocket connection.


## Contributors

- [Fehmi Anaç]([link-to-your-profile](https://github.com/fehmianac)) - Project Lead
---
