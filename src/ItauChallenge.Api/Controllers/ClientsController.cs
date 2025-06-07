using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Api.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/clients")]
public class ClientsController : ControllerBase
{
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(ILogger<ClientsController> logger)
    {
        _logger = logger;
    }

    // GET /api/v1/clients/{clientId}/positions
    [HttpGet("{clientId}/positions")]
    [ProducesResponseType(typeof(ClientPositionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetClientPosition(string clientId)
    {
        _logger.LogInformation("API: Requesting position for Client {ClientId}", clientId);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return BadRequest("Client ID cannot be empty.");
        }
        await Task.Delay(50); // Simulate async work
        var dto = new ClientPositionDto
        {
            ClientId = clientId,
            Assets = new List<AssetPositionDto>
            {
                new AssetPositionDto { AssetId = "ITUB4", Quantity = 1000, CurrentMarketPrice = 30.50m, TotalValue = 30500, AverageAcquisitionPrice = 28.75m },
                new AssetPositionDto { AssetId = "VALE3", Quantity = 500, CurrentMarketPrice = 70.10m, TotalValue = 35050, AverageAcquisitionPrice = 65.20m }
            },
            TotalPortfolioValue = 65550,
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
        await Task.Delay(50);
        var clients = Enumerable.Range(1, count)
            .Select(i => new ClientRankingInfoDto { ClientId = $"Client{i:D3}", ClientName = $"Top Client {i} by Position", Value = new Random().Next(100000, 10000000) })
            .ToList();
        var dto = new TopClientsDto { Criteria = "ByPosition", Count = count, Clients = clients, AsOfDate = DateTime.UtcNow };
        return Ok(dto);
    }

    // GET /api/v1/clients/top-by-brokerage?count=10
    [HttpGet("top-by-brokerage")]
    [ProducesResponseType(typeof(TopClientsDto), 200)]
    public async Task<IActionResult> GetTopClientsByBrokerage([FromQuery] int count = 10)
    {
        _logger.LogInformation("API: Requesting Top {Count} clients by brokerage", count);
        await Task.Delay(50);
         var clients = Enumerable.Range(1, count)
            .Select(i => new ClientRankingInfoDto { ClientId = $"ClientB{i:D3}", ClientName = $"Top Client {i} by Brokerage", Value = new Random().Next(1000, 50000) })
            .ToList();
        var dto = new TopClientsDto { Criteria = "ByBrokerage", Count = count, Clients = clients, AsOfDate = DateTime.UtcNow };
        return Ok(dto);
    }
}
