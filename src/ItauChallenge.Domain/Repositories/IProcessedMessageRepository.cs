using System.Threading.Tasks;

namespace ItauChallenge.Domain.Repositories
{
    public interface IProcessedMessageRepository
    {
        Task<bool> IsMessageProcessedAsync(string messageId);
        Task MarkAsProcessedAsync(string messageId);
    }
}
