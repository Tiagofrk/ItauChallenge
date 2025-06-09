using Microsoft.AspNetCore.Mvc;
using ItauChallenge.Contracts.Dtos;
using System;
using System.Threading.Tasks;
using ItauChallenge.Application.Services; // Using Application Service Interface
using ItauChallenge.Domain; // For User entity
using Microsoft.Extensions.Logging; // For ILogger

namespace ItauChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IUserApplicationService _userApplicationService; // Changed dependency

    public UsersController(ILogger<UsersController> logger, IUserApplicationService userApplicationService) // Changed dependency
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userApplicationService = userApplicationService ?? throw new ArgumentNullException(nameof(userApplicationService));
    }

    // Note: GetAveragePrice endpoint was moved to ClientsController as per IClientApplicationService design.
    // If a GET /api/v1/users/{id} endpoint were to exist, CreateUser would use it in CreatedAtAction.
    // For now, GetUserById is a placeholder name for that concept.

    [HttpPost]
    [ProducesResponseType(typeof(User), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        _logger.LogInformation("API: Attempting to create user with email: {Email}", createUserDto.Email);
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("API: CreateUser request failed due to invalid model state.");
            return BadRequest(ModelState);
        }

        try
        {
            var createdUser = await _userApplicationService.CreateUserAsync(createUserDto).ConfigureAwait(false);
            _logger.LogInformation("API: Successfully created user with ID: {UserId}", createdUser.Id);
            // Assuming a GetUserById action exists or will exist:
            // return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
            // If GetUserById is not available/planned, StatusCode 201 with the object is also acceptable.
            return StatusCode(StatusCodes.Status201Created, createdUser);
        }
        catch (Exception ex) // Catch potential exceptions from the service layer
        {
            _logger.LogError(ex, "An error occurred while creating user with email: {Email}", createUserDto.Email);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    // Placeholder for GetUserById if it were to be implemented for CreatedAtAction:
    // [HttpGet("{id}")]
    // public async Task<IActionResult> GetUserById(int id)
    // {
    //     // Implementation using _userApplicationService.GetUserByIdAsync(id)
    //     await Task.CompletedTask;
    //     return NotFound(); // Placeholder
    // }
}
