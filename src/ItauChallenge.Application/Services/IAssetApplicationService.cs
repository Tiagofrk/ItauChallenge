using ItauChallenge.Contracts.Dtos; // For LatestQuoteDto
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public interface IAssetApplicationService
    {
        Task<LatestQuoteDto> GetLatestQuoteAsync(string assetId);
    }
}
