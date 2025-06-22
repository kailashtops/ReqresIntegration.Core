namespace Practical.Test
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
    using Moq;
    using Moq.Protected;
    using Practical.Core.Models;
    using Practical.Core.Service;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class ExternalUserServiceTests
    {
        [Test]
        public void Framework_Should_Run_BasicTest()
        {
            Assert.(true);
        }
        [Test]
        public async Task GetUserById()
        {
            // Arrange
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\": {\"id\": 1, \"email\": \"test@example.com\", \"first_name\": \"John\", \"last_name\": \"Doe\"}}")
            };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(fakeResponse);

            var httpClient = new HttpClient(handlerMock.Object);
            var options = Options.Create(new ReqresApiOptions { BaseUrl = "https://reqres.in/api/" });
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = new ExternalUserService(httpClient, (Microsoft.Extensions.Configuration.IConfiguration)options, memoryCache);

            // Act
            var user = await service.GetUserByIdAsync(1);

            // Assert
            Assert.are("John", user.FirstName);
        }
        [Test]
        public async Task GetUserByIdNotFound()
        {
            // Arrange
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(fakeResponse);

            var httpClient = new HttpClient(handlerMock.Object);
            var options = Options.Create(new ReqresApiOptions { BaseUrl = "https://reqres.in/api/" });
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = new ExternalUserService(httpClient, (Microsoft.Extensions.Configuration.IConfiguration)options, memoryCache);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetUserByIdAsync(999));
        }
        [Test]
        public async Task GetAllUsersAsync_Page2_ReturnsUsers()
        {
            // Arrange
            var fakeJson = @" {
                        ""page"": 2,
                        ""per_page"": 6,
                        ""total"": 12,
                        ""total_pages"": 2,
                        ""data"": [
                            {""id"": 7, ""email"": ""michael.lawson@reqres.in"", ""first_name"": ""Michael"", ""last_name"": ""Lawson""},
                            {""id"": 8, ""email"": ""lindsay.ferguson@reqres.in"", ""first_name"": ""Lindsay"", ""last_name"": ""Ferguson""}
                        ]
                    }";

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fakeJson)
            };

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)) // First attempt fails
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)                   // Second attempt succeeds
                {
                    Content = new StringContent("Success")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var options = Options.Create(new ReqresApiOptions { BaseUrl = "https://reqres.in/api/" });
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = new ExternalUserService(httpClient, (Microsoft.Extensions.Configuration.IConfiguration)options, memoryCache);

            // Act
            var users = (await service.GetAllUsersAsync(2)).ToList();

            // Assert
            Assert.NotEmpty(users);
            Assert.Equal(2, users.Count);
            Assert.Equal("Michael", users[0].FirstName);
        }

    }

}