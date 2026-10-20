using Microsoft.AspNetCore.Mvc;
using CRM.Automation.Core.Models;

namespace CRM.Automation.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        var status = new
        {
            Service = "CRM.Automation.Api (.NET 8)",
            Status = "Healthy",
            ServerUtcTime = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            Framework = Environment.Version.ToString()
        };

        return Ok(BOProcessResult.Succeed(status, "Automation Service is online and operational."));
    }
}
