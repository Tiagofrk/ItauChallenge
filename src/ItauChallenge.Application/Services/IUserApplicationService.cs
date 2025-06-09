using ItauChallenge.Contracts.Dtos; // For CreateUserDto
using ItauChallenge.Domain; // For User entity
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public interface IUserApplicationService
    {
        Task<User> CreateUserAsync(CreateUserDto createUserDto);
    }
}
