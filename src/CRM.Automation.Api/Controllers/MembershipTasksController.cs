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
public class MembershipTasksController : ControllerBase
{
    private readonly IMemberTierService _memberTierService;
    private readonly MemberTierSettings _memberTierSettings;
    private readonly ILogger<MembershipTasksController> _logger;

    public MembershipTasksController(
        IMemberTierService memberTierService,
        IOptions<MemberTierSettings> memberTierOptions,
        ILogger<MembershipTasksController> logger)
    {
        _memberTierService = memberTierService;
        _memberTierSettings = memberTierOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// CheckPoint Task: Xét duyệt và hạ hạng thành viên định kỳ.
    /// CheckPoint chỉ truyền các tham số kỹ thuật tĩnh/phi nghiệp vụ (ExecutionId, TaskCode, TriggerTime).
    /// </summary>
    [HttpPost("downgrade-membership")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RunDowngradeMembership(
        [FromBody] DowngradeMembershipRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new DowngradeMembershipRequest();
        string executionId = request.ExecutionId ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation("CheckPoint triggered DowngradeMembership. ExecutionId: {ExecutionId}, TaskCode: {TaskCode}, TriggerBy: {TriggerBy}",
            executionId, request.TaskCode, request.TriggerBy);

        // API đọc giá trị từ appsettings.json và truyền cho Service
        var result = await _memberTierService.ExecuteDowngradeAsync(
            executionId: executionId,
            evaluationPeriodMonths: _memberTierSettings.EvaluationPeriodMonths,
            ct: cancellationToken
        );

        return Ok(result);
    }
}
