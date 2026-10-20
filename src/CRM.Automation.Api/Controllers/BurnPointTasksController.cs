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
    private readonly ILogger<BurnPointTasksController> _logger;

    public BurnPointTasksController(
        IBurnPointService burnPointService,
        IResetPointService resetPointService,
        IOptions<BurnPointSettings> burnPointOptions,
        IOptions<ResetPointSettings> resetPointOptions,
        ILogger<BurnPointTasksController> logger)
    {
        _burnPointService = burnPointService;
        _resetPointService = resetPointService;
        _burnPointSettings = burnPointOptions.Value;
        _resetPointSettings = resetPointOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// CheckPoint Task: Gửi thông báo nhắc nhở cháy điểm cho hội viên.
    /// CheckPoint chỉ truyền các tham số kỹ thuật tĩnh/phi nghiệp vụ (ExecutionId, TaskCode, TriggerTime).
    /// Các tham số nghiệp vụ được API tự động đọc từ appsettings.json và truyền vào Service.
    /// </summary>
    [HttpPost("burn-point-reminder")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RunBurnPointReminder(
        [FromBody] BurnPointReminderRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new BurnPointReminderRequest();
        string executionId = request.ExecutionId ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation("CheckPoint triggered BurnPointReminder. ExecutionId: {ExecutionId}, TaskCode: {TaskCode}, TriggerBy: {TriggerBy}",
            executionId, request.TaskCode, request.TriggerBy);

        // API đọc giá trị nghiệp vụ từ appsettings.json và truyền cho Service
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
    /// CheckPoint chỉ truyền các tham số kỹ thuật tĩnh/phi nghiệp vụ (ExecutionId, TaskCode, TriggerTime).
    /// </summary>
    [HttpPost("reset-point")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RunResetPoint(
        [FromBody] ResetPointRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ResetPointRequest();
        string executionId = request.ExecutionId ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation("CheckPoint triggered ResetPoint. ExecutionId: {ExecutionId}, TaskCode: {TaskCode}, TriggerBy: {TriggerBy}",
            executionId, request.TaskCode, request.TriggerBy);

        // API đọc giá trị từ appsettings.json và truyền cho Service
        var result = await _resetPointService.ExecuteResetPointAsync(
            executionId: executionId,
            batchSize: _resetPointSettings.BatchSize,
            ct: cancellationToken
        );

        return Ok(result);
    }
}
