using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Contracts.Dtos;
using ItauChallenge.Application.Services; // Using Application Service Interface
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // For ILogger

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/assets")]
public class AssetsController : ControllerBase
{
    private readonly ILogger<AssetsController> _logger;
    private readonly IAssetApplicationService _assetApplicationService; // Changed dependency

    public AssetsController(ILogger<AssetsController> logger, IAssetApplicationService assetApplicationService) // Changed dependency
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _assetApplicationService = assetApplicationService ?? throw new ArgumentNullException(nameof(assetApplicationService));
    }

    // GET /api/v1/assets/{assetId}/quotes/latest
    [HttpGet("{assetId}/quotes/latest")]
    [ProducesResponseType(typeof(LatestQuoteDto), 200)]
    [ProducesResponseType(400)] // Bad Request for parsing errors
    [ProducesResponseType(404)] // Not Found
    public async Task<IActionResult> GetLatestQuote(string assetId)
    {
        _logger.LogInformation("API: Requesting latest quote for Asset ID {AssetId}", assetId);
        try
        {
            var latestQuoteDto = await _assetApplicationService.GetLatestQuoteAsync(assetId).ConfigureAwait(false);
            if (latestQuoteDto == null)
            {
                _logger.LogWarning("No quote found for Asset ID {AssetId} via application service.", assetId);
                return NotFound($"No quote found for Asset ID {assetId}.");
            }
            _logger.LogInformation("API: Successfully fetched latest quote for Asset ID {AssetId}", assetId);
            return Ok(latestQuoteDto);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid Asset ID format for {AssetId}", assetId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex) // Catch other potential exceptions from the service layer
        {
            _logger.LogError(ex, "An error occurred while fetching latest quote for Asset ID {AssetId}", assetId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }
}
