using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Api.Dtos;
using ItauChallenge.Application; // For IResilientQuoteService (can be kept or removed if not used)
using ItauChallenge.Infra; // For IDatabaseService
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // For ILogger

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/assets")]
public class AssetsController : ControllerBase
{
    private readonly ILogger<AssetsController> _logger;
    private readonly IDatabaseService _databaseService;
    // private readonly IResilientQuoteService _resilientQuoteService; // Keep if needed for other purposes or remove

    public AssetsController(ILogger<AssetsController> logger, IDatabaseService databaseService /*, IResilientQuoteService resilientQuoteService */)
    {
        _logger = logger;
        _databaseService = databaseService;
        // _resilientQuoteService = resilientQuoteService;
    }

    // GET /api/v1/assets/{assetId}/quotes/latest
    [HttpGet("{assetId}/quotes/latest")]
    [ProducesResponseType(typeof(LatestQuoteDto), 200)]
    [ProducesResponseType(400)] // Bad Request for parsing errors
    [ProducesResponseType(404)] // Not Found
    public async Task<IActionResult> GetLatestQuote(string assetId)
    {
        _logger.LogInformation("API: Requesting latest quote for Asset ID {AssetId} from database", assetId);

        if (!int.TryParse(assetId, out var parsedAssetId))
        {
            return BadRequest("Invalid Asset ID format.");
        }

        var quote = await _databaseService.GetLatestQuoteAsync(parsedAssetId);

        if (quote == null)
        {
            _logger.LogWarning("No quote found in database for Asset ID {AssetId}", parsedAssetId);
            return NotFound($"No quote found for Asset ID {parsedAssetId}.");
        }

        // Fetch asset details (ticker) for the DTO
        var assetDetails = await _databaseService.GetAssetByIdAsync(parsedAssetId);
        string ticker = assetDetails?.Ticker ?? assetId; // Use ticker if available, else the original assetId string

        var dto = new LatestQuoteDto
        {
            AssetId = ticker, // Display ticker in DTO
            Price = quote.Price,
            Timestamp = quote.QuoteDth, // Use the actual quote timestamp
            Source = "Database"
        };
        _logger.LogInformation("API: Successfully fetched latest quote for Asset ID {AssetId} (Ticker: {Ticker}) from database", parsedAssetId, ticker);
        return Ok(dto);
    }
}
