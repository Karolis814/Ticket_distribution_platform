using Microsoft.AspNetCore.Mvc;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/platform")]
public class PlatformController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("fee")]
    public IActionResult GetFee() =>
        Ok(new { feePercent = configuration.GetValue<decimal?>("Stripe:PlatformFeePercent") ?? 5m });
}