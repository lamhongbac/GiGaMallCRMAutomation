using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CRM.Automation.Api.Filters;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.Requests;
using CRM.Automation.Core.Models.Settings;

namespace CRM.Automation.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[TypeFilter(typeof(ApiKeyAuthFilter))]
public class BurnPointTasksController : ControllerBase
{
    private readonly IBurnPointService _burnPointService;
    private readonly IResetPointService _resetPointService;
    private readonly BurnPointSettings _burnPointSettings;
    private readonly ResetPointSettings _resetPointSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BurnPointTasksController> _logger;

    public BurnPointTasksController(
        IBurnPointService burnPointService,
        IResetPointService resetPointService,
        IOptions<BurnPointSettings> burnPointOptions,
        IOptions<ResetPointSettings> resetPointOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<BurnPointTasksController> logger)
    {
        _burnPointService = burnPointService;
        _resetPointService = resetPointService;
        _burnPointSettings = burnPointOptions.Value;
        _resetPointSettings = resetPointOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// CheckPoint Task: Gửi thông báo nhắc nhở cháy điểm cho hội viên.
    /// Hỗ trợ cả Task Type 1 (WebAPI Đồng bộ) và Task Type 3 (WebApiAsync Webhook Callback).
    /// </summary>
    [HttpPost("burn-point-reminder")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RunBurnPointReminder(
        [FromBody] BurnPointReminderRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new BurnPointReminderRequest();
        string executionId = request.ExecutionId ?? Guid.NewGuid().ToString("N");

        // Kiểm tra xem CheckPoint có gọi theo cơ chế Task Type 3 (WebApiAsync) không
        string? callbackUrl = Request.Headers["X-CheckPoint-CallbackUrl"].FirstOrDefault();
        string? checkpointLogIdStr = Request.Headers["X-CheckPoint-LogId"].FirstOrDefault();
        long.TryParse(checkpointLogIdStr, out long checkpointLogId);

        _logger.LogInformation("CheckPoint triggered BurnPointReminder. ExecutionId: {ExecutionId}, TaskCode: {TaskCode}, Mode: {Mode}",
            executionId, request.TaskCode, string.IsNullOrEmpty(callbackUrl) ? "Type 1 (Sync)" : "Type 3 (Async Callback)");

        // 1. NẾU LÀ TASK TYPE 3 (WebApiAsync): Trả về 202 Accepted ngay, xử lý ngầm và bắn callback
        if (!string.IsNullOrWhiteSpace(callbackUrl))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var asyncResult = await _burnPointService.ExecuteReminderAsync(
                        executionId: executionId,
                        resetPointYears: _burnPointSettings.ResetPointYears,
                        remindDay: _burnPointSettings.RemindDay,
                        batchSize: _burnPointSettings.BatchSize,
                        ct: CancellationToken.None
                    );

                    // Bắn Webhook Callback về CheckPoint
                    using var client = _httpClientFactory.CreateClient();
                    var callbackPayload = new
                    {
                        logId = checkpointLogId,
                        success = asyncResult.Ok,
                        resultMessage = asyncResult.Message,
                        data = asyncResult.Data
                    };

                    var cbResponse = await client.PostAsJsonAsync(callbackUrl, callbackPayload);
                    _logger.LogInformation("Đã gửi Webhook Callback về CheckPoint ({Url}) cho LogId={LogId}. Status: {Status}", 
                        callbackUrl, checkpointLogId, cbResponse.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý ngầm hoặc bắn Webhook Callback cho LogId={LogId}", checkpointLogId);
                }
            });

            return Accepted(BOProcessResult.Success(
                new { logId = checkpointLogId, executionId }, 
                "Tác vụ đã được tiếp nhận và đang xử lý ngầm (Task Type 3 WebApiAsync)."
            ));
        }

        // 2. NẾU LÀ TASK TYPE 1 (WebAPI đồng bộ): Chờ xử lý xong và trả về kết quả ngay
        var result = await _burnPointService.ExecuteReminderAsync(
            executionId: executionId,
            resetPointYears: _burnPointSettings.ResetPointYears,
            remindDay: _burnPointSettings.RemindDay,
            batchSize: _burnPointSettings.BatchSize,
            ct: cancellationToken
        );

        return Ok(result);
    }

    /// <summary>
    /// CheckPoint Task: Chốt & hủy điểm hết hạn của hội viên.
    /// Hỗ trợ cả Task Type 1 (WebAPI Đồng bộ) và Task Type 3 (WebApiAsync Webhook Callback).
    /// </summary>
    [HttpPost("reset-point")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RunResetPoint(
        [FromBody] ResetPointRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ResetPointRequest();
        string executionId = request.ExecutionId ?? Guid.NewGuid().ToString("N");

        string? callbackUrl = Request.Headers["X-CheckPoint-CallbackUrl"].FirstOrDefault();
        string? checkpointLogIdStr = Request.Headers["X-CheckPoint-LogId"].FirstOrDefault();
        long.TryParse(checkpointLogIdStr, out long checkpointLogId);

        _logger.LogInformation("CheckPoint triggered ResetPoint. ExecutionId: {ExecutionId}, TaskCode: {TaskCode}, Mode: {Mode}",
            executionId, request.TaskCode, string.IsNullOrEmpty(callbackUrl) ? "Type 1 (Sync)" : "Type 3 (Async Callback)");

        // 1. Task Type 3 (WebApiAsync)
        if (!string.IsNullOrWhiteSpace(callbackUrl))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var asyncResult = await _resetPointService.ExecuteResetPointAsync(
                        executionId: executionId,
                        batchSize: _resetPointSettings.BatchSize,
                        ct: CancellationToken.None
                    );

                    using var client = _httpClientFactory.CreateClient();
                    var callbackPayload = new
                    {
                        logId = checkpointLogId,
                        success = asyncResult.Ok,
                        resultMessage = asyncResult.Message,
                        data = asyncResult.Data
                    };

                    await client.PostAsJsonAsync(callbackUrl, callbackPayload);
                    _logger.LogInformation("Đã gửi Webhook Callback về CheckPoint cho ResetPoint LogId={LogId}", checkpointLogId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý ngầm ResetPoint cho LogId={LogId}", checkpointLogId);
                }
            });

            return Accepted(BOProcessResult.Success(
                new { logId = checkpointLogId, executionId }, 
                "Tác vụ đã được tiếp nhận và đang xử lý ngầm (Task Type 3 WebApiAsync)."
            ));
        }

        // 2. Task Type 1 (Sync)
        var result = await _resetPointService.ExecuteResetPointAsync(
            executionId: executionId,
            batchSize: _resetPointSettings.BatchSize,
            ct: cancellationToken
        );

        return Ok(result);
    }
}
