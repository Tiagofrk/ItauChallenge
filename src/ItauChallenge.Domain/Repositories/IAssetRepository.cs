using ItauChallenge.Domain; // Changed from ItauChallenge.Domain.Entities
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ItauChallenge.Domain.Repositories
{
    public interface IAssetRepository
    {
        Task<Asset> GetByIdAsync(int assetId);
        Task<Asset> GetByTickerAsync(string ticker);
        Task<IEnumerable<Asset>> GetAllAsync();
    }
}
