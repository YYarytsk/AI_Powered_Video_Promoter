using Microsoft.AspNetCore.Mvc;

namespace PianoPromoCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "PianoPromoCopilot API",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
