using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Practical.Core.Interfaces;
using Practical.Core.Models;
using System.Net;
using System.Text.Json;

namespace Practical.Core.Service
{
    public class ExternalUserService : IExternalUserService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IMemoryCache _cache;

        public ExternalUserService(HttpClient httpClient, IConfiguration config, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _baseUrl = config["ReqresApi:BaseUrl"] ?? "https://reqres.in/api/";
            _cache = cache;
        }
        
        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                string cacheKey = $"User_{userId}";
               
                if (_cache.TryGetValue(cacheKey, out User cachedUser))
                {
                    return cachedUser;
                }
                var response = await _httpClient.GetAsync($"{_baseUrl}users/{userId}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new KeyNotFoundException($"User with ID {userId} not found.");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(content);

                var user = JsonSerializer.Deserialize<User>(json.GetProperty("data").ToString());
                _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
                return user!;
            }
            catch (TaskCanceledException ex)
            {
                if (!ex.CancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Error: Request timed out.");
                }
                else
                {
                    Console.WriteLine("Request was cancelled.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Deserialization error: {ex.Message}");
            }
           
            return new();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(int page)
        {

            if (_cache.TryGetValue("AllUsers", out IEnumerable<User> cachedUsers))
            {
                return cachedUsers;
            }

        
            int totalPages = 1;
            var allUsers = new List<User>();
            try
            {
                do
                {
                    var response = await _httpClient.GetAsync($"{_baseUrl}users?page={page}");
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<User>>(content);

                    if (apiResponse?.Data == null)
                        throw new Exception("Failed to deserialize users.");

                    allUsers.AddRange(apiResponse.Data);
                    totalPages = apiResponse.TotalPages;
                    page++;
                } while (page <= totalPages);

                _cache.Set("AllUsers", allUsers, TimeSpan.FromMinutes(5));
                return allUsers;
            }
            catch (TaskCanceledException ex)
            {
                if (!ex.CancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Error: Request timed out.");
                }
                else
                {
                    Console.WriteLine("Request was cancelled.");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Deserialization error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
            return null;
        }
    }

}
