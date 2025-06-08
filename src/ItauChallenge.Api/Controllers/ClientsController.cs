using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Api.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ItauChallenge.Infra; // For IDatabaseService
using ItauChallenge.Domain; // For Position
using Microsoft.Extensions.Logging; // For ILogger

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/clients")]
public class ClientsController : ControllerBase
{
    private readonly ILogger<ClientsController> _logger;
    private readonly IDatabaseService _databaseService;

    public ClientsController(ILogger<ClientsController> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    // GET /api/v1/clients/{clientId}/positions
    [HttpGet("{clientId}/positions")]
    [ProducesResponseType(typeof(ClientPositionDto), 200)]
    [ProducesResponseType(400)] // Bad Request for parsing errors
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetClientPosition(string clientId)
    {
        _logger.LogInformation("API: Requesting position for Client {ClientId}", clientId);
        if (!int.TryParse(clientId, out var parsedClientId))
        {
            return BadRequest("Invalid Client ID format.");
        }

        var positions = await _databaseService.GetClientPositionsAsync(parsedClientId);

        if (positions == null || !positions.Any())
        {
            // Return an empty list in the DTO as per simplified requirement, or NotFound
            var emptyDto = new ClientPositionDto
            {
                ClientId = clientId,
                Assets = new List<AssetPositionDto>(),
                TotalPortfolioValue = 0,
                AsOfDate = DateTime.UtcNow
            };
            return Ok(emptyDto); // Or NotFound($"No positions found for Client ID {parsedClientId}.");
        }

        var assetPositionsDto = new List<AssetPositionDto>();
        decimal totalPortfolioValue = 0;

        foreach (var pos in positions)
        {
            var assetDetails = await _databaseService.GetAssetByIdAsync(pos.AssetId);
            string assetTicker = assetDetails?.Ticker ?? pos.AssetId.ToString(); // Fallback to AssetId if no ticker

            var latestQuote = await _databaseService.GetLatestQuoteAsync(pos.AssetId);
            decimal currentMarketPrice = latestQuote?.Price ?? 0; // Fallback to 0 if no quote

            var assetPosition = new AssetPositionDto
            {
                AssetId = assetTicker,
                Quantity = pos.Quantity,
                AverageAcquisitionPrice = pos.AveragePrice,
                CurrentMarketPrice = currentMarketPrice,
                TotalValue = pos.Quantity * currentMarketPrice
            };
            assetPositionsDto.Add(assetPosition);
            totalPortfolioValue += assetPosition.TotalValue;
        }

        var dto = new ClientPositionDto
        {
            ClientId = clientId,
            Assets = assetPositionsDto,
            TotalPortfolioValue = totalPortfolioValue,
            AsOfDate = DateTime.UtcNow
        };
        return Ok(dto);
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
