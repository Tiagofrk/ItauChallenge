using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Api.Dtos;
using System;
using System.Threading.Tasks;

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/brokerage")]
public class BrokerageController : ControllerBase
{
    private readonly ILogger<BrokerageController> _logger;

    public BrokerageController(ILogger<BrokerageController> logger)
    {
        _logger = logger;
    }

    // GET /api/v1/brokerage/earnings
    [HttpGet("earnings")]
    [ProducesResponseType(typeof(BrokerageEarningsDto), 200)]
    public async Task<IActionResult> GetBrokerageEarnings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        _logger.LogInformation("API: Requesting brokerage earnings from {StartDate} to {EndDate}", startDate, endDate);
        await Task.Delay(50);
        var dto = new BrokerageEarningsDto
        {
            StartDate = startDate ?? DateTime.UtcNow.AddMonths(-1),
            EndDate = endDate ?? DateTime.UtcNow,
            TotalEarnings = new Random().Next(100000, 2000000) / 100.0m,
            Currency = "BRL"
        };
        return Ok(dto);
    }
}
