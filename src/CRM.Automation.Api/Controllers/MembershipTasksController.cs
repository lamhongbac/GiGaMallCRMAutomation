using Microsoft.AspNetCore.Mvc;
using CRM.Automation.Api.Filters;
using CRM.Automation.Core.Interfaces;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.Requests;

namespace CRM.Automation.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[TypeFilter(typeof(ApiKeyAuthFilter))]
public class MembershipTasksController : ControllerBase
{
    private readonly IMemberTierService _memberTierService;
    private readonly ILogger<MembershipTasksController> _logger;

    public MembershipTasksController(
        IMemberTierService memberTierService,
        ILogger<MembershipTasksController> logger)
    {
        _memberTierService = memberTierService;
        _logger = logger;
    }

    /// <summary>
    /// CheckPoint Task: Xét duyệt và hạ hạng thành viên định kỳ
    /// </summary>
    [HttpPost("downgrade-membership")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RunDowngradeMembership(
        [FromBody] DowngradeMembershipRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new DowngradeMembershipRequest();
        _logger.LogInformation("Received CheckPoint trigger for Downgrade Membership. EvaluationPeriodMonths: {Months}",
            request.EvaluationPeriodMonths);

        var result = await _memberTierService.ExecuteDowngradeAsync(request, cancellationToken);
        return Ok(result);
    }
}
