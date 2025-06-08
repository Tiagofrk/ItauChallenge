using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Api.Dtos;
using System;
using System.Threading.Tasks;

using ItauChallenge.Infra; // For IDatabaseService
using Microsoft.Extensions.Logging; // For ILogger

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/brokerage")]
public class BrokerageController : ControllerBase
{
    private readonly ILogger<BrokerageController> _logger;
    private readonly IDatabaseService _databaseService;

    public BrokerageController(ILogger<BrokerageController> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    // GET /api/v1/brokerage/earnings
    [HttpGet("earnings")]
    [ProducesResponseType(typeof(BrokerageEarningsDto), 200)]
    public async Task<IActionResult> GetBrokerageEarnings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        _logger.LogInformation("API: Requesting brokerage earnings from {StartDate} to {EndDate}", startDate, endDate);
        // TODO: Implement once brokerage data is available in the schema and service layer.
        // This would require _databaseService to have a method like GetBrokerageDataAsync(startDate, endDate)
        // and then summing up relevant fields. The 'op' table currently doesn't have a 'brokerage_fee' column.
        // If 'op_brokerage' from the original prompt was added to 'op' table, it could be used here.
        await Task.Delay(10); // Simulate async work
        var dto = new BrokerageEarningsDto
        {
            StartDate = startDate ?? DateTime.UtcNow.AddMonths(-1),
            EndDate = endDate ?? DateTime.UtcNow,
            TotalEarnings = new Random().Next(10000, 200000) / 100.0m, // Placeholder
            Currency = "BRL (Placeholder)"
        };
        return Ok(dto);
    }
}
