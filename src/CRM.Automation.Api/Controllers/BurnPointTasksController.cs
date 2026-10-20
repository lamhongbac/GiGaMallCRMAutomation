using Microsoft.AspNetCore.Mvc;
using CRM.Automation.Api.Filters;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.Requests;

namespace CRM.Automation.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[TypeFilter(typeof(ApiKeyAuthFilter))]
public class BurnPointTasksController : ControllerBase
{
    private readonly IBurnPointService _burnPointService;
    private readonly IResetPointService _resetPointService;
    private readonly ILogger<BurnPointTasksController> _logger;

    public BurnPointTasksController(
        IBurnPointService burnPointService,
        IResetPointService resetPointService,
        ILogger<BurnPointTasksController> logger)
    {
        _burnPointService = burnPointService;
        _resetPointService = resetPointService;
        _logger = logger;
    }

    /// <summary>
    /// CheckPoint Task: Gửi thông báo nhắc nhở cháy điểm cho hội viên (Task Type 1 WebAPI / Type 3 Async)
    /// </summary>
    [HttpPost("burn-point-reminder")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RunBurnPointReminder(
        [FromBody] BurnPointReminderRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new BurnPointReminderRequest();
        _logger.LogInformation("Received CheckPoint trigger for Burn Point Reminder. ResetPointYears: {Years}, RemindDay: {Days}, BatchSize: {BatchSize}",
            request.ResetPointYears, request.RemindDay, request.BatchSize);

        var result = await _burnPointService.ExecuteReminderAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// CheckPoint Task: Chốt & hủy điểm hết hạn của hội viên (Task Type 1 WebAPI / Type 3 Async)
    /// </summary>
    [HttpPost("reset-point")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RunResetPoint(
        [FromBody] ResetPointRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ResetPointRequest();
        _logger.LogInformation("Received CheckPoint trigger for Reset Point. BatchSize: {BatchSize}",
            request.BatchSize);

        var result = await _resetPointService.ExecuteResetPointAsync(request, cancellationToken);
        return Ok(result);
    }
}
