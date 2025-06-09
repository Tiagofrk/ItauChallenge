using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Contracts.Dtos;
using ItauChallenge.Application.Services; // Using Application Service Interface
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // For ILogger

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/brokerage")]
public class BrokerageController : ControllerBase
{
    private readonly ILogger<BrokerageController> _logger;
    private readonly IBrokerageApplicationService _brokerageApplicationService; // Changed dependency

    public BrokerageController(ILogger<BrokerageController> logger, IBrokerageApplicationService brokerageApplicationService) // Changed dependency
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _brokerageApplicationService = brokerageApplicationService ?? throw new ArgumentNullException(nameof(brokerageApplicationService));
    }

    // GET /api/v1/brokerage/earnings
    [HttpGet("earnings")]
    [ProducesResponseType(typeof(BrokerageEarningsDto), 200)]
    public async Task<IActionResult> GetBrokerageEarnings([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        _logger.LogInformation("API: Requesting brokerage earnings from {StartDate} to {EndDate}", startDate, endDate);
        try
        {
            var dto = await _brokerageApplicationService.GetBrokerageEarningsAsync(startDate, endDate).ConfigureAwait(false);
            _logger.LogInformation("API: Successfully processed brokerage earnings request.");
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching brokerage earnings.");
            return StatusCode(500, "An internal server error occurred.");
        }
    }
}
