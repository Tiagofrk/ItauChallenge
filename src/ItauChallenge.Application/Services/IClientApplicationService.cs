using ItauChallenge.Contracts.Dtos; // For ClientPositionDto, AveragePriceDto
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public interface IClientApplicationService
    {
        Task<IEnumerable<ClientPositionDto>> GetClientPositionsAsync(int userId);
        Task<AveragePriceDto> GetAveragePurchasePriceAsync(int userId, string assetTicker);
        // Add other client-related operations as needed
    }
}
