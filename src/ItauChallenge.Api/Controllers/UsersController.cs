using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Api.Dtos;
using System;
using System.Threading.Tasks;
using ItauChallenge.Infra; // For IDatabaseService
using ItauChallenge.Domain; // For Purchase, FinancialCalculations, Operation
using System.Linq; // For Select and Sum
using Microsoft.Extensions.Logging; // For ILogger

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IDatabaseService _databaseService;

    public UsersController(ILogger<UsersController> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    // GET /api/v1/users/{userId}/assets/{assetId}/average-price
    [HttpGet("{userId}/assets/{assetId}/average-price")]
    [ProducesResponseType(typeof(AveragePriceDto), 200)]
    [ProducesResponseType(400)] // Bad Request for parsing errors
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAveragePrice(string userId, string assetId)
    {
        _logger.LogInformation("API: Requesting average price for User {UserId}, Asset {AssetId}", userId, assetId);

        if (!int.TryParse(userId, out var parsedUserId))
        {
            return BadRequest("Invalid User ID format.");
        }

        if (!int.TryParse(assetId, out var parsedAssetId))
        {
            return BadRequest("Invalid Asset ID format.");
        }

        var operations = await _databaseService.GetUserOperationsAsync(parsedUserId, parsedAssetId);

        if (operations == null || !operations.Any())
        {
            _logger.LogWarning("No operations found for User {UserId}, Asset {AssetId}", parsedUserId, parsedAssetId);
            // Return NotFound or an empty DTO based on requirements. NotFound seems appropriate.
            return NotFound($"No operations found for User ID {parsedUserId} and Asset ID {parsedAssetId}.");
        }

        // Filter for "Compra" (Buy) operations only for average purchase price calculation
        var buyOperations = operations.Where(op => op.Type == OperationType.Compra).ToList();

        if (!buyOperations.Any())
        {
            _logger.LogWarning("No 'Compra' (Buy) operations found for User {UserId}, Asset {AssetId} to calculate average price.", parsedUserId, parsedAssetId);
            return NotFound($"No 'Compra' (Buy) operations found for User ID {parsedUserId} and Asset ID {parsedAssetId} to calculate average price.");
        }

        var purchases = buyOperations.Select(op => new Purchase(op.Quantity, op.Price)).ToList();

        var averagePrice = FinancialCalculations.CalculateWeightedAveragePrice(purchases);
        var totalQuantity = buyOperations.Sum(op => op.Quantity); // Total quantity of "Compra" operations

        var dto = new AveragePriceDto
        {
            UserId = userId, // Keep original string representation or parsed int? String for consistency with request.
            AssetId = assetId, // Same as UserId
            AveragePrice = averagePrice,
            TotalQuantity = totalQuantity,
            CalculationDate = DateTime.UtcNow
        };
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(User), 201)] // Assuming User domain object is returned
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new User
        {
            Name = createUserDto.Name,
            Email = createUserDto.Email
            // CreatedDth will be set by the service/database
        };

        // The service method now takes brokeragePercent as a separate parameter
        var createdUser = await _databaseService.CreateUserAsync(user, createUserDto.BrokeragePercent);

        // Return a 201 Created response with the location of the new resource and the resource itself
        // The location URL might need adjustment based on how one would typically retrieve a single user (e.g., GET /api/v1/users/{id})
        // For now, returning the created user object directly.
        // A common practice is return CreatedAtAction or CreatedAtRoute.
        // Let's assume a GetUserById method exists or will exist for the Location header.
        // If not, we can return Ok(createdUser) or just the createdUser for simplicity as per initial thought.
        // For now, returning 201 with the object. A `Location` header is best practice.
        // To keep it simple for now, and since there isn't a GET endpoint for a single user yet:
        return StatusCode(201, createdUser);
    }
}
