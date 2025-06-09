using ItauChallenge.Domain; // Changed from ItauChallenge.Domain.Entities
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ItauChallenge.Domain.Repositories
{
    public interface IOperationRepository
    {
        Task<IEnumerable<Operation>> GetUserOperationsAsync(int userId, int assetId, int days);
        Task AddAsync(Operation operation);
    }
}
