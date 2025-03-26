using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Kamaq.Finsights.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly ILogger<HealthCheckController> _logger;

    public HealthCheckController(ILogger<HealthCheckController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Health check called");
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
} 