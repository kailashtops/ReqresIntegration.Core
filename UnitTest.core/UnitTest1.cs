using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Practical.Core.Service;
using System.Net;
using System.Text;

namespace UnitTest.core
{
    public class Tests
    {
        private IConfiguration _configuration;
        private IMemoryCache _cache;
        private HttpClient _httpClient;
        private Mock<HttpMessageHandler> _handlerMock;

        [SetUp]
        public void Setup()
        {
            var configDictionary = new Dictionary<string, string>
            {
                { "ReqresApi:BaseUrl", "https://reqres.in/api/" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDictionary)
                .Build();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        }
        [TearDown]
        public void Cleanup()
        {
            _httpClient?.Dispose();
            _cache?.Dispose();
        }
        [Test]
        public async Task GetUserById()
        {
            // Arrange
            var responseJson = "{\"data\": {\"id\": 1, \"email\": \"test@example.com\", \"first_name\": \"kailash\", \"last_name\": \"solanki\"}}";
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(fakeResponse);

            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("https://reqres.in/api/")
            };
            var service = new ExternalUserService(_httpClient, _configuration, _cache);
            // Act
            var user = await service.GetUserByIdAsync(1);
            // Assert
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Id, Is.EqualTo(1));
            Assert.That(user.FirstName, Is.EqualTo("kailash"));
            Assert.That(user.LastName, Is.EqualTo("solanki"));
            Assert.That(user.Email, Is.EqualTo("test@example.com"));
        }

        [Test]
        public async Task NotFoundExceptionTest()
        {
            // Arrange
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith("/users/999")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(fakeResponse);

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://reqres.in/api/")
            };
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new ExternalUserService(httpClient, _configuration, cache);
            // Act & Assert
            var ex =  Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetUserByIdAsync(999);
            });

            Assert.That(ex.Message, Is.EqualTo("User with ID 999 not found."));
        }


        [Test]
        public async Task GetAllUsersTest()
        {
            // Arrange
            var fakeJson = @"{
                    ""page"": 2,
                    ""per_page"": 6,
                    ""total"": 12,
                    ""total_pages"": 2,
                    ""data"": [
                        {""id"": 7, ""email"": ""test@gmail.com"", ""first_name"": ""kailash"", ""last_name"": ""solanki""},
                        {""id"": 8, ""email"": ""test1@gmail.com"", ""first_name"": ""ravi"", ""last_name"": ""solanki""}
                    ]
                }";
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
            { "ReqresApi:BaseUrl", "https://reqres.in/api/" }
                })
                .Build();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
            };
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(fakeResponse);
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://reqres.in/api/")
            };
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = new ExternalUserService(httpClient, config, memoryCache);
            // Act
            var users = (await service.GetAllUsersAsync(2)).ToList();
            // Assert
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count, Is.EqualTo(2));
            Assert.That(users[0].FirstName, Is.EqualTo("kailash"));
            Assert.That(users[1].FirstName, Is.EqualTo("ravi"));
        }

    }
}