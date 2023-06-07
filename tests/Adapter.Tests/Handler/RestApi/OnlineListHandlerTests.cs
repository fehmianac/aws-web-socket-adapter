using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Adapter.Handler.RestApi;
using Adapter.Models.Response;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Factory;
using Moq;
using Xunit;

namespace Adapter.Tests.Handler.RestApi
{
    public class OnlineListHandlerTests
    {
        private readonly Mock<IUserConnectionRepository> _userConnectionRepositoryMock;
        private readonly OnlineListHandler _handler;

        public OnlineListHandlerTests()
        {
            _userConnectionRepositoryMock = new Mock<IUserConnectionRepository>();
            _handler = new OnlineListHandler(_userConnectionRepositoryMock.Object);
        }

        [Fact]
        public async Task Handler_WithoutUserIds_ReturnsOnlineUsers()
        {
            // Arrange
            var onlineUsers = new List<string> {"user1", "user2", "user3"};
            var request = new APIGatewayProxyRequest();
            var context = new Mock<ILambdaContext>();

            _userConnectionRepositoryMock
                .Setup(repo => repo.GetOnlineListAsync(CancellationToken.None))
                .ReturnsAsync(onlineUsers);

            // Act
            var response = await _handler.Handler(request, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.GetOnlineListAsync(CancellationToken.None), Times.Once);

            Assert.Equal((int) HttpStatusCode.OK, response.StatusCode);
            var onlineStatusResponseModels = JsonSerializer.Deserialize<List<OnlineStatusResponseModel>>(response.Body);
            Assert.NotNull(onlineStatusResponseModels);
            Assert.Equal(onlineUsers.Count, onlineStatusResponseModels.Count);
            Assert.All(onlineStatusResponseModels, model =>
            {
                Assert.True(model.IsOnline);
                Assert.Equal(DateTime.UtcNow, model.LastActivity, TimeSpan.FromSeconds(1));
            });
        }

        [Fact]
        public async Task Handler_WithUserIds_ReturnsOnlineAndOfflineUsers()
        {
            // Arrange
            var onlineUsers = new List<string> {"user1", "user2"};
            var offlineUsers = new List<string> {"user3", "user4"};
            var userIds = onlineUsers.Concat(offlineUsers).ToList();
            var lastActivity = offlineUsers.Select(userId => new UserLastActivity()
            {
                Id = userId,
                Time = DateTime.UtcNow.AddMinutes(-10)
            }).ToList();
            var request = new APIGatewayProxyRequest
            {
                QueryStringParameters = new Dictionary<string, string>
                {
                    {"userIds", string.Join(",", userIds)}
                }
            };
            var context = new Mock<ILambdaContext>();

            _userConnectionRepositoryMock
                .Setup(repo => repo.GetOnlineListAsync(userIds, CancellationToken.None))
                .ReturnsAsync(onlineUsers);
            _userConnectionRepositoryMock
                .Setup(repo => repo.GetLastActivityAsync(offlineUsers, CancellationToken.None))
                .ReturnsAsync(lastActivity);

            // Act
            var response = await _handler.Handler(request, context.Object);

            // Assert
            _userConnectionRepositoryMock.Verify(repo => repo.GetOnlineListAsync(userIds, CancellationToken.None), Times.Once);
            _userConnectionRepositoryMock.Verify(repo => repo.GetLastActivityAsync(offlineUsers, CancellationToken.None), Times.Once);

            Assert.Equal((int) HttpStatusCode.OK, response.StatusCode);
            var onlineStatusResponseModels = JsonSerializer.Deserialize<List<OnlineStatusResponseModel>>(response.Body);
            Assert.NotNull(onlineStatusResponseModels);
            Assert.Equal(userIds.Count, onlineStatusResponseModels.Count);
            Assert.Equal(onlineUsers.Count, onlineStatusResponseModels.Count(model => model.IsOnline));
            Assert.Equal(offlineUsers.Count, onlineStatusResponseModels.Count(model => !model.IsOnline));
            Assert.All(onlineStatusResponseModels, model =>
            {
                if (model.IsOnline)
                {
                    Assert.Equal(DateTime.UtcNow, model.LastActivity, TimeSpan.FromSeconds(1));
                }
                else
                {
                    var offlineUser = lastActivity.First(info => info.Id == model.UserId);
                    Assert.Equal(offlineUser.Time, model.LastActivity, TimeSpan.FromSeconds(1));
                }
            });
        }
        [Fact]
        public async Task Should_Valid_Default_Ctor()
        {
            var handler = new OnlineListHandler();
        }
    }
}