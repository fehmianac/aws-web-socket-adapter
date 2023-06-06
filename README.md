# AWS WebSocket Adapter
Sure! Based on the information you provided, here's an example README file structure for your C# project:

A C# project that serves as an adapter to AWS API Gateway WebSockets.

## Introduction

This project provides a convenient one-click deployment solution for integrating AWS API Gateway WebSockets with your application. By leveraging this project, you can seamlessly connect your C# project to AWS API Gateway WebSockets without the need for extensive technical knowledge or manual setup. The project consists of three main layers: Domain, Infrastructure, and Adapter, which handle various aspects of the integration.

## Architecture Diagram

![infra.png](docs%2Finfra.png)

## Features

- Connects AWS API Gateway WebSockets to your C# project.
- Handles WebSocket events such as OnConnected, OnDisconnected, SendMessage, and Authorizer.
- Verifies JWT tokens during the OnConnected event using an authorizer.
- Sends messages to AWS API Gateway Management API via an SQS queue using the SendMessage function.

## Example Usage

Here are a few examples to help you get started with using this project:

1. **OnConnected**: This function is triggered when a client connects to the WebSocket. You can customize the behavior in the `OnConnected` Lambda function code.

2. **OnDisconnected**: This function is triggered when a client disconnects from the WebSocket. You can modify the `OnDisconnected` Lambda function code to handle the disconnection event.

3. **SendMessage**: This function consumes data from an SQS queue and sends a message to the AWS API Gateway Management API. You can customize the logic inside the `SendMessage` Lambda function to process the received data and send the appropriate message.

4. **Authorizer**: This function verifies JWT tokens during the OnConnected event. You can modify the `Authorizer` Lambda function code to implement your own JWT verification logic.

## Prerequisites

Before running the project, make sure you have the following prerequisites:

- [AWS Account](https://aws.amazon.com/) with appropriate permissions and an API Gateway WebSocket configured.
- [C#](https://docs.microsoft.com/en-us/dotnet/csharp/) environment set up on your machine.

## Getting Started

To get started with the project, follow these steps:

1. Clone the repository: `git clone https://github.com/fehmianac/aws-web-socket-adapter.git`
3. Configure the AWS credentials and region in the project.
4. Build and deploy the Lambda functions to your AWS account.
5. Set up the necessary event mappings and configurations in AWS API Gateway.
6. Run the project.

For detailed instructions on how to configure and deploy the project, refer to the [documentation](link-to-documentation).

## Contributors

- [Fehmi Anaç]([link-to-your-profile](https://github.com/fehmianac)) - Project Lead


## License

This project is licensed under the [MIT License](link-to-license-file).
