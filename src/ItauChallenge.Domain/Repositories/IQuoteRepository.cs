using ItauChallenge.Domain; // Changed from ItauChallenge.Domain.Entities
using System.Threading.Tasks;

namespace ItauChallenge.Domain.Repositories
{
    public interface IQuoteRepository
    {
        Task<Quote> GetLatestQuoteAsync(int assetId);
        Task SaveAsync(Quote quote);
    }
}
