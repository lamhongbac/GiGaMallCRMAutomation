using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models.Settings;
using CRM.Automation.Infrastructure.Data;
using CRM.Automation.Infrastructure.Services;
using CRMMall.BillReview.BLL.Services;
using CRMMall.BillReview.DAL.Data;
using CRMMall.BillReview.DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Automation.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrmAutomationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Cấu hình Business Settings từ appsettings.json (với fallback defaults)
        services.Configure<BurnPointSettings>(configuration.GetSection(BurnPointSettings.SectionName));
        services.Configure<ResetPointSettings>(configuration.GetSection(ResetPointSettings.SectionName));
        services.Configure<MemberTierSettings>(configuration.GetSection(MemberTierSettings.SectionName));

        // 2. Data Access
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<BurnPointRepository>();
        services.AddScoped<MemberTierRepository>();
        services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IBillRepository, BillRepository>();

        // 3. HTTP Client for Push Notifications
        services.AddHttpClient<IFirebasePushService, FirebasePushService>(client =>
        {
            var baseUrl = configuration["Notification:GatewayUrl"] ?? "http://localhost:5000/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // 4. Shared BLL Domain Services
        services.AddScoped<ILoyaltyPointsService, LoyaltyPointsService>();
        services.AddScoped<IMembershipTierService, MembershipTierService>();
        services.AddScoped<ILotteryPromotionService, LotteryPromotionService>();
        services.AddScoped<INotificationService, NotificationService>();

        // 5. Automation Task Services
        services.AddScoped<IBurnPointService, BurnPointService>();
        services.AddScoped<IResetPointService, ResetPointService>();
        services.AddScoped<IMemberTierService, MemberTierService>();

        return services;
    }
}
