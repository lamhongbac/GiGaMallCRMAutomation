using CRM.Automation.Core.Interfaces;
using CRM.Automation.Infrastructure.Data;
using CRM.Automation.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Automation.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrmAutomationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<BurnPointRepository>();

        services.AddHttpClient<IFirebasePushService, FirebasePushService>(client =>
        {
            var baseUrl = configuration["Notification:GatewayUrl"] ?? "http://localhost:5000/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IBurnPointService, BurnPointService>();
        services.AddScoped<IResetPointService, ResetPointService>();
        services.AddScoped<IMemberTierService, MemberTierService>();

        return services;
    }
}
