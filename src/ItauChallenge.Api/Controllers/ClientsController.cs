using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ItauChallenge.Application.Services; // Using Application Service Interface
using Microsoft.Extensions.Logging; // For ILogger

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/clients")]
public class ClientsController : ControllerBase
{
    private readonly ILogger<ClientsController> _logger;
    private readonly IClientApplicationService _clientApplicationService; // Changed dependency

    public ClientsController(ILogger<ClientsController> logger, IClientApplicationService clientApplicationService) // Changed dependency
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientApplicationService = clientApplicationService ?? throw new ArgumentNullException(nameof(clientApplicationService));
    }

    // GET /api/v1/clients/{clientId}/positions
    [HttpGet("{clientId}/positions")]
    [ProducesResponseType(typeof(ClientPositionDto), 200)]
    [ProducesResponseType(400)] // Bad Request for parsing errors
    [ProducesResponseType(404)] // Not Found for client or if client has no positions
    public async Task<IActionResult> GetClientPosition(string clientId)
    {
        _logger.LogInformation("API: Requesting positions for Client ID {ClientId}", clientId);
        if (!int.TryParse(clientId, out var parsedClientId))
        {
            _logger.LogWarning("Invalid Client ID format: {ClientId}", clientId);
            return BadRequest("Invalid Client ID format.");
        }

        try
        {
            var clientPositions = await _clientApplicationService.GetClientPositionsAsync(parsedClientId).ConfigureAwait(false);

            // The service returns IEnumerable<ClientPositionDto>. Assuming it returns one item for this specific client.
            var clientPositionDto = clientPositions.FirstOrDefault();

            if (clientPositionDto == null || !clientPositionDto.Assets.Any())
            {
                _logger.LogInformation("No positions found for Client ID {ParsedClientId}", parsedClientId);
                // Return an empty list in the DTO as per simplified requirement / original controller logic
                 var emptyDto = new ClientPositionDto
                {
                    ClientId = clientId,
                    Assets = new List<AssetPositionDto>(),
                    TotalPortfolioValue = 0,
                    AsOfDate = DateTime.UtcNow
                };
                return Ok(emptyDto);
            }

            _logger.LogInformation("API: Successfully fetched positions for Client ID {ParsedClientId}", parsedClientId);
            return Ok(clientPositionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching positions for Client ID {ClientId}", clientId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    // GET /api/v1/clients/{clientId}/assets/{assetTicker}/average-price
    [HttpGet("{clientId}/assets/{assetTicker}/average-price")]
    [ProducesResponseType(typeof(AveragePriceDto), 200)]
    [ProducesResponseType(400)] // Bad Request
    [ProducesResponseType(404)] // Not Found
    public async Task<IActionResult> GetAveragePurchasePrice(string clientId, string assetTicker)
    {
        _logger.LogInformation("API: Requesting average purchase price for Client {ClientId}, Asset {AssetTicker}", clientId, assetTicker);

        if (!int.TryParse(clientId, out var parsedClientId))
        {
            return BadRequest("Invalid Client ID format.");
        }
        if (string.IsNullOrWhiteSpace(assetTicker))
        {
            return BadRequest("Asset ticker cannot be empty.");
        }

        try
        {
            var averagePriceDto = await _clientApplicationService.GetAveragePurchasePriceAsync(parsedClientId, assetTicker).ConfigureAwait(false);
            if (averagePriceDto == null) // Service might return null if asset not found
            {
                _logger.LogWarning("Average purchase price not found for Client {ClientId}, Asset {AssetTicker} (asset might not exist).", clientId, assetTicker);
                return NotFound($"Asset with ticker {assetTicker} not found or no purchase operations by client {clientId}.");
            }
            if (averagePriceDto.TotalQuantity == 0 && averagePriceDto.AveragePrice == 0) // No purchase operations found
            {
                 _logger.LogInformation("No purchase operations found for Client {ClientId}, Asset {AssetTicker}.", clientId, assetTicker);
                return NotFound($"No purchase operations found for Client {clientId} and Asset {assetTicker}.");
            }
            _logger.LogInformation("API: Successfully fetched average purchase price for Client {ClientId}, Asset {AssetTicker}", clientId, assetTicker);
            return Ok(averagePriceDto);
        }
        catch (Exception ex) // Consider specific exceptions if service throws them (e.g., AssetNotFoundException)
        {
            _logger.LogError(ex, "An error occurred while fetching average purchase price for Client {ClientId}, Asset {AssetTicker}", clientId, assetTicker);
            return StatusCode(500, "An internal server error occurred.");
        }
    }


    // GET /api/v1/clients/top-by-position?count=10
    [HttpGet("top-by-position")]
    [ProducesResponseType(typeof(TopClientsDto), 200)]
    public async Task<IActionResult> GetTopClientsByPosition([FromQuery] int count = 10)
    {
        _logger.LogInformation("API: Requesting Top {Count} clients by position", count);
        // TODO: Implement when aggregation queries are defined in IDatabaseService
        await Task.Delay(10); // Simulate async work
        var clients = Enumerable.Range(1, count)
            .Select(i => new ClientRankingInfoDto { ClientId = $"Client{i:D3}", ClientName = $"Top Client {i} by Position (Placeholder)", Value = new Random().Next(100000, 10000000) })
            .ToList();
        var dto = new TopClientsDto { Criteria = "ByPosition (Placeholder)", Count = count, Clients = clients, AsOfDate = DateTime.UtcNow };
        return Ok(dto);
    }

    // GET /api/v1/clients/top-by-brokerage?count=10
    [HttpGet("top-by-brokerage")]
    [ProducesResponseType(typeof(TopClientsDto), 200)]
    public async Task<IActionResult> GetTopClientsByBrokerage([FromQuery] int count = 10)
    {
        _logger.LogInformation("API: Requesting Top {Count} clients by brokerage", count);
        // TODO: Implement once brokerage data is available in the schema and service layer.
        await Task.Delay(10); // Simulate async work
         var clients = Enumerable.Range(1, count)
            .Select(i => new ClientRankingInfoDto { ClientId = $"ClientB{i:D3}", ClientName = $"Top Client {i} by Brokerage (Placeholder)", Value = new Random().Next(1000, 50000) })
            .ToList();
        var dto = new TopClientsDto { Criteria = "ByBrokerage (Placeholder)", Count = count, Clients = clients, AsOfDate = DateTime.UtcNow };
        return Ok(dto);
    }
}
