using ItauChallenge.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ItauChallenge.Infra
{
    public interface IDatabaseService
    {
        Task InitializeDatabaseAsync();
        Task<IEnumerable<Operation>> GetUserOperationsAsync(int userId, int assetId, int days = 30);
        Task SaveQuoteAsync(Quote quote, string messageId);
        Task<bool> IsMessageProcessedAsync(string messageId);
        Task UpdateClientPositionsAsync(int assetId, decimal newPrice);
        Task<User> CreateUserAsync(User user, decimal brokeragePercent);

        // New methods for API controllers
        Task<Quote> GetLatestQuoteAsync(int assetId);
        Task<IEnumerable<Position>> GetClientPositionsAsync(int userId);
        Task<Asset> GetAssetByIdAsync(int assetId);
        Task<Asset> GetAssetByTickerAsync(string ticker); // For QuotesConsumer
    }
}
