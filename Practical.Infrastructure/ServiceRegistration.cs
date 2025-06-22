using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Practical.Core.Interfaces;
using Practical.Core.Models;
using Practical.Core.Service;

namespace Practical.Infrastructure
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPracticalServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<APIConfiguration>(config.GetSection("ApiEndPoint"));

            // HttpClient with retry logic
            services.AddHttpClient<IExternalUserService, ExternalUserService>()
                .AddPolicyHandler(GetRetryPolicy());

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // Exponential backoff
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        Console.WriteLine($"[Retry] Attempt #{retryAttempt}, Waiting {timespan.TotalSeconds} sec");
                    });
        }
    }
}
