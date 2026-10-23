using Microsoft.AspNetCore.Mvc;

namespace CRMMall.BillReview.GUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                service = "CRMMall.BillReview.Microservice",
                status = "Healthy",
                architecture = "3-Tier (DAL, BLL, GUI)",
                version = "1.0.0",
                timestamp = System.DateTime.UtcNow
            });
        }
    }
}
