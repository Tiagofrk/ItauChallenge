using ItauChallenge.Contracts.Dtos;
using ItauChallenge.Application.Services;
using ItauChallenge.Domain; // For User entity
using ItauChallenge.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public class UserApplicationService : IUserApplicationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserApplicationService> _logger;

        public UserApplicationService(
            IUserRepository userRepository,
            ILogger<UserApplicationService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User> CreateUserAsync(CreateUserDto createUserDto)
        {
            _logger.LogInformation("Attempting to create user with email: {Email}", createUserDto.Email);

            if (createUserDto == null)
            {
                throw new ArgumentNullException(nameof(createUserDto));
            }

            var user = new User
            {
                // Id will be set by the database
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                // CreatedDth should be set by the repository or database
            };

            // BrokeragePercent is passed separately to the repository method
            var createdUser = await _userRepository.CreateAsync(user, createUserDto.BrokeragePercent)
                                                   .ConfigureAwait(false);

            _logger.LogInformation("Successfully created user with ID: {UserId}", createdUser.Id);
            return createdUser;
        }
    }
}
