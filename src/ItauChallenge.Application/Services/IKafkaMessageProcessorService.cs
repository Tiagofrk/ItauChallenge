using ItauChallenge.Domain; // For Quote
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public interface IKafkaMessageProcessorService
    {
        Task ProcessQuoteMessageAsync(Quote quote, string messageId);
    }
}
