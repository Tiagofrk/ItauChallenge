using ItauChallenge.Domain; // Changed from ItauChallenge.Domain.Entities
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ItauChallenge.Domain.Repositories
{
    public interface IPositionRepository
    {
        Task<IEnumerable<Position>> GetClientPositionsAsync(int userId);
        Task UpdatePositionsForAssetPriceAsync(int assetId, decimal newPrice);
        Task AddOrUpdatePositionAsync(Position position);
    }
}
