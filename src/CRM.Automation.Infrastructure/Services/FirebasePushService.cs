using System.Net.Http.Json;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CRM.Automation.Infrastructure.Services;

/// <summary>
/// Tái sử dụng 100% cơ chế gửi Notification hiện hữu của Gigamall CRM:
/// 1. Đọc Device Token (AppID) từ [dbo].[mssMobileTracking]
/// 2. Ghi bản ghi vào Hộp thư thông báo hội viên [dbo].[mssNotification]
/// 3. Bắn HTTP POST JSON (chuẩn SendMessageToDeviceRequest) tới Notification Gateway API hiện hành
/// </summary>
public class FirebasePushService : IFirebasePushService
{
    private readonly HttpClient _httpClient;
    private readonly BurnPointRepository _repository;
    private readonly IConfiguration _config;
    private readonly ILogger<FirebasePushService> _logger;
    private readonly bool _simulationMode;

    public FirebasePushService(
        HttpClient httpClient, 
        BurnPointRepository repository,
        IConfiguration config, 
        ILogger<FirebasePushService> logger)
    {
        _httpClient = httpClient;
        _repository = repository;
        _config = config;
        _logger = logger;
        _simulationMode = config.GetValue<bool>("Notification:SimulatePush", true);
    }

    public async Task<bool> SendPushAsync(
        string memId, 
        string title, 
        string message, 
        string screen = "RewardHistoryDetail", 
        string function = "GetRewardHistories", 
        CancellationToken ct = default)
    {
        try
        {
            // 1. Lấy AppID (Device Token) từ mssMobileTracking (tương đương BaseMobileTracking.GetAppID ở code cũ)
            string? deviceToken = await _repository.GetMemberDeviceTokenAsync(memId, ct);
            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                _logger.LogWarning("Không thể gửi notification cho Member={MemId}: Hội viên chưa đăng nhập ứng dụng Mobile (MemberNotLogin)", memId);
                return false;
            }

            // 2. Lưu tin nhắn vào Hộp thư thông báo [dbo].[mssNotification] (tương đương Notification.Create ở code cũ)
            await _repository.CreateInAppNotificationAsync(memId, title, message, screen, function, ct: ct);

            // 3. Chuẩn bị payload khớp chuẩn SendMessageToDeviceRequest của Gateway hiện hữu
            var gatewayPayload = new
            {
                Token = deviceToken,
                Subject = title,
                Description = message,
                Content = message,
                ScreenName = screen,
                FunctionName = function,
                DisplayStyle = "Information",
                Language = "VN",
                EventName = "RemindExpiredPoint",
                ContentParameter = new
                {
                    Subject = title,
                    Content = message
                }
            };

            // 4. Nếu đang ở chế độ Simulation (kiểm thử UAT an toàn)
            if (_simulationMode)
            {
                _logger.LogInformation("[GATEWAY SIMULATION] Đã lưu mssNotification & mô phỏng gửi Gateway cho MemID={MemId}, Token={Token}, Title={Title}", 
                    memId, deviceToken, title);
                return true;
            }

            // 5. Gửi thật qua Gateway hiện hữu
            string sendEndpoint = _config["Notification:SendToDeviceUrl"] ?? "api/Notification/SendToDevice";
            var response = await _httpClient.PostAsJsonAsync(sendEndpoint, gatewayPayload, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Gửi notification thành công qua Gateway cho MemID={MemId}", memId);
                return true;
            }

            _logger.LogError("Gateway trả về lỗi HTTP {StatusCode} khi gửi cho MemID={MemId}", response.StatusCode, memId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thực thi quy trình gửi Notification cho Member={MemId}", memId);
            return false;
        }
    }
}
