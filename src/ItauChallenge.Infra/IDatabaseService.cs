using ItauChallenge.Domain.Repositories;
using ItauChallenge.Domain; // Added for Quote type

namespace ItauChallenge.Infra
{
    public interface IDatabaseService : IAssetRepository, IOperationRepository, IUserRepository, IPositionRepository, IQuoteRepository, IProcessedMessageRepository, IDbInitializer
    {
        Task SaveQuoteAndMarkMessageProcessedAsync(Quote quote, string messageId);
        // Existing specific methods in IDatabaseService (if any) that are not part of the new repositories can remain for now,
        // or be marked as obsolete, or removed if they are now covered by the repository interfaces.
        // For this task, we assume it will only compose the new interfaces.
    }
}
