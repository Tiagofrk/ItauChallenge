using ItauChallenge.Domain; // For Quote
using ItauChallenge.Domain.Repositories; // For IProcessedMessageRepository
using ItauChallenge.Infra; // For IDatabaseService
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ItauChallenge.Application.Services
{
    public class KafkaMessageProcessorService : IKafkaMessageProcessorService
    {
        private readonly IDatabaseService _databaseService; // The main one from Infra, for the new atomic method
        private readonly IProcessedMessageRepository _processedMessageRepository; // Still needed for checking
        private readonly ILogger<KafkaMessageProcessorService> _logger;

        public KafkaMessageProcessorService(
            IDatabaseService databaseService,
            IProcessedMessageRepository processedMessageRepository,
            ILogger<KafkaMessageProcessorService> logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _processedMessageRepository = processedMessageRepository ?? throw new ArgumentNullException(nameof(processedMessageRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessQuoteMessageAsync(Quote quote, string messageId)
        {
            _logger.LogInformation("KafkaMessageProcessorService: Processing message ID {MessageId} for asset {AssetId}", messageId, quote.AssetId);

            // Idempotency check: See if message has already been processed.
            if (await _processedMessageRepository.IsMessageProcessedAsync(messageId).ConfigureAwait(false))
            {
                _logger.LogWarning("KafkaMessageProcessorService: Message ID {MessageId} has already been processed. Skipping.", messageId);
                return;
            }

            // Call the new atomic method on IDatabaseService
            // This method handles quote saving and marking message as processed in a single transaction.
            await _databaseService.SaveQuoteAndMarkMessageProcessedAsync(quote, messageId).ConfigureAwait(false);

            _logger.LogInformation("KafkaMessageProcessorService: Successfully processed and saved quote for message ID {MessageId}, Asset ID {AssetId}", messageId, quote.AssetId);
        }
    }
}
