using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Api.Dtos;
using System;
using System.Threading.Tasks;

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    // In a real app, you'd inject a service that uses ItauChallenge.Domain.FinancialCalculations

    public UsersController(ILogger<UsersController> logger)
    {
        _logger = logger;
    }

    // GET /api/v1/users/{userId}/assets/{assetId}/average-price
    [HttpGet("{userId}/assets/{assetId}/average-price")]
    [ProducesResponseType(typeof(AveragePriceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAveragePrice(string userId, string assetId)
    {
        _logger.LogInformation("API: Requesting average price for User {UserId}, Asset {AssetId}", userId, assetId);
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(assetId))
        {
            return BadRequest("User ID and Asset ID cannot be empty.");
        }

        // Placeholder: In a real app, call domain logic here
        await Task.Delay(50); // Simulate async work
        var dto = new AveragePriceDto
        {
            UserId = userId,
            AssetId = assetId,
            AveragePrice = new Random().Next(10,1000) / 10.0m, // Random decimal
            TotalQuantity = new Random().Next(100, 10000),
            CalculationDate = DateTime.UtcNow
        };
        return Ok(dto);
    }
}
