using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Practical.Core.Interfaces;
using Practical.Infrastructure;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: false);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddMemoryCache();
        services.AddPracticalServices(context.Configuration); 
    })
    .Build();

var service = host.Services.GetRequiredService<IExternalUserService>();

// Fetch a Single user data 
var user = await service.GetUserByIdAsync(2);
Console.WriteLine("Single User Record");
Console.WriteLine($" {user.FirstName} {user.LastName}");

// Fetch all users data
var allUsers = await service.GetAllUsersAsync(2);
Console.WriteLine("Page 2 User Record");
foreach (var u in allUsers)
    Console.WriteLine($" {u.Id} {u.FirstName} {u.LastName}");
Console.ReadLine();