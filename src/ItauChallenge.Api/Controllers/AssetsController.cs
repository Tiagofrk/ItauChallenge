using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Api.Dtos;
using ItauChallenge.Application; // For IResilientQuoteService
using System;
using System.Threading.Tasks;

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/assets")]
public class AssetsController : ControllerBase
{
    private readonly ILogger<AssetsController> _logger;
    private readonly IResilientQuoteService _resilientQuoteService; // Assuming this is registered

    public AssetsController(ILogger<AssetsController> logger, IResilientQuoteService resilientQuoteService)
    {
        _logger = logger;
        _resilientQuoteService = resilientQuoteService;
    }

    // GET /api/v1/assets/{assetId}/quotes/latest
    [HttpGet("{assetId}/quotes/latest")]
    [ProducesResponseType(typeof(LatestQuoteDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(503)] // Service Unavailable (e.g. fallback from circuit breaker)
    public async Task<IActionResult> GetLatestQuote(string assetId)
    {
        _logger.LogInformation("API: Requesting latest quote for {AssetId}", assetId);
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return BadRequest("Asset ID cannot be empty.");
        }

        // Use the resilient service created in a previous step
        var quoteString = await _resilientQuoteService.GetLatestQuoteAsync(assetId);

        if (quoteString.StartsWith("Fallback:"))
        {
            // Simulate a DTO for fallback - real implementation would be more robust
            var fallbackDto = new LatestQuoteDto
            {
                AssetId = assetId,
                Price = 0, // Or a cached value if available
                Timestamp = DateTime.UtcNow,
                Source = quoteString
            };
            // For fallbacks indicating service unavailability, 503 is appropriate.
            // If it's a fallback with stale data, 200 might still be okay with a flag.
            return StatusCode(503, fallbackDto);
        }

        // Simulate creating a DTO from the quoteString.
        // In a real scenario, _resilientQuoteService would return a structured object.
        var dto = new LatestQuoteDto
        {
            AssetId = assetId,
            Price = new Random().Next(10, 500), // Placeholder - extract from quoteString
            Timestamp = DateTime.UtcNow,
            Source = "Live API Call" // Placeholder
        };
        _logger.LogInformation("API: Successfully fetched latest quote for {AssetId}", assetId);
        return Ok(dto);
    }
}
