using ItauChallenge.Domain; // Changed from ItauChallenge.Domain.Entities
using System.Threading.Tasks;

namespace ItauChallenge.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user, decimal brokeragePercent);
        Task<User> GetByIdAsync(int userId);
    }
}
