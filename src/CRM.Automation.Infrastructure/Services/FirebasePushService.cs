using System.Net.Http.Json;
using CRM.Automation.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CRM.Automation.Infrastructure.Services;

public class FirebasePushService : IFirebasePushService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FirebasePushService> _logger;
    private readonly bool _simulationMode;

    public FirebasePushService(HttpClient httpClient, IConfiguration config, ILogger<FirebasePushService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _simulationMode = config.GetValue<bool>("Notification:SimulationMode", false);
    }

    public async Task<bool> SendPushAsync(string memId, string title, string message, string screen = "RewardHistoryDetail", string function = "GetRewardHistories", CancellationToken ct = default)
    {
        if (_simulationMode)
        {
            _logger.LogInformation("[SIMULATION NOTIFICATION] MemID={MemId} | Title={Title} | Body={Message}", memId, title, message);
            return true;
        }

        try
        {
            var payload = new
            {
                MemID = memId,
                Title = title,
                Content = message,
                ScreenName = screen,
                FunctionName = function,
                Timestamp = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync("api/v1/notifications/push", payload, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi Push Notification cho Member={MemId}", memId);
            return false;
        }
    }
}
