using ItauChallenge.Contracts.Dtos; // For BrokerageEarningsDto
using System;
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public interface IBrokerageApplicationService
    {
        Task<BrokerageEarningsDto> GetBrokerageEarningsAsync(DateTime? startDate, DateTime? endDate);
    }
}
