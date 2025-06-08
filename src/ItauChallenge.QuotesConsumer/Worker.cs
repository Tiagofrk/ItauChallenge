using Confluent.Kafka;
using Polly;
using Polly.Retry;

namespace ItauChallenge.QuotesConsumer;

using ItauChallenge.Domain; // For Quote, Asset
using ItauChallenge.Infra;  // For IDatabaseService
using Microsoft.Extensions.Configuration; // For IConfiguration
using System.Text.Json;     // For JsonSerializer

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDatabaseService _databaseService;
    private readonly IConfiguration _configuration;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;
    private readonly AsyncRetryPolicy _consumerPolicy;

    public Worker(ILogger<Worker> logger, IDatabaseService databaseService, IConfiguration configuration)
    {
        _logger = logger;
        _databaseService = databaseService;
        _configuration = configuration;

        // Read Kafka settings from configuration
        _bootstrapServers = _configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
        _topic = _configuration.GetValue<string>("Kafka:Topic") ?? "financial-quotes";
        _groupId = _configuration.GetValue<string>("Kafka:GroupId") ?? "quotes-consumer-group";

        _consumerPolicy = Policy
            .Handle<ConsumeException>()
            .Or<KafkaException>() // Broader Kafka exceptions
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Consumer error on attempt {RetryCount}. Retrying in {TimeSpan}...", retryCount, timeSpan);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Quotes Consumer Worker running at: {time}", DateTimeOffset.Now);

        await _consumerPolicy.ExecuteAsync(async () =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false // Important for idempotency and control
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_topic);
            _logger.LogInformation("Subscribed to topic: {Topic} with group: {GroupId}", _topic, _groupId);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Poll for messages
                        var consumeResult = consumer.Consume(stoppingToken); // This will block until a message or cancellation

                        if (consumeResult.IsPartitionEOF)
                        {
                            _logger.LogInformation("Reached end of partition {Partition}, offset {Offset}.", consumeResult.Partition, consumeResult.Offset);
                            continue;
                        }

                        _logger.LogInformation("Consumed message: '{MessageValue}' at: '{TopicPartitionOffset}'.", consumeResult.Message.Value, consumeResult.TopicPartitionOffset);

                        // Simulate processing
                        await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);

                        // Commit the offset after successful processing
                        try
                        {
                            consumer.Commit(consumeResult);
                            _logger.LogInformation("Offset committed for message at {TopicPartitionOffset}", consumeResult.TopicPartitionOffset);
                        }
                        catch (KafkaException e)
                        {
                            _logger.LogError(e, "Commit failed for offset {TopicPartitionOffset}", consumeResult.TopicPartitionOffset);
                            // Decide if this is critical enough to stop or if retry policy should handle it
                        }
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError(e, "Error consuming from Kafka topic {Topic}", _topic);
                        // Polly will handle retry for this exception based on policy
                        if (e.Error.IsFatal)
                        {
                            _logger.LogCritical(e, "Fatal error consuming from Kafka. Exiting loop.");
                            break; // Exit loop on fatal error
                        }
                        throw; // Re-throw to let Polly handle it
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation requested. Shutting down consumer.");
                        break;
                    }
                    catch (Exception ex) // Catch-all for unexpected errors during message loop
                    {
                        _logger.LogError(ex, "Unexpected error in consumer loop for topic {Topic}", _topic);
                        // Depending on the error, might need to decide if to break or continue
                        await Task.Delay(5000, stoppingToken); // Wait a bit before trying next consume
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer service stopping.");
            }
            finally
            {
                _logger.LogInformation("Closing Kafka consumer.");
                consumer.Close(); // Ensure consumer is closed properly
            }
        });
    }

    private async Task ProcessMessageAsync(string kafkaMessage, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Attempting to process message: {KafkaMessage}", kafkaMessage);

        KafkaMessageDto messageDto;
        try
        {
            messageDto = JsonSerializer.Deserialize<KafkaMessageDto>(kafkaMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (messageDto == null || string.IsNullOrWhiteSpace(messageDto.MessageId) || string.IsNullOrWhiteSpace(messageDto.AssetTicker))
            {
                _logger.LogError("Failed to deserialize Kafka message or message is missing required fields: {KafkaMessage}", kafkaMessage);
                return; // Skip invalid message
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON Deserialization error for message: {KafkaMessage}", kafkaMessage);
            return; // Skip malformed message
        }

        try
        {
            // 1. Check for idempotency
            if (await _databaseService.IsMessageProcessedAsync(messageDto.MessageId))
            {
                _logger.LogInformation("Message {MessageId} has already been processed. Skipping.", messageDto.MessageId);
                return;
            }

            // 2. Get Asset ID from Ticker
            var asset = await _databaseService.GetAssetByTickerAsync(messageDto.AssetTicker);
            if (asset == null)
            {
                _logger.LogWarning("Asset with ticker {AssetTicker} not found. Skipping message {MessageId}.", messageDto.AssetTicker, messageDto.MessageId);
                // Consider sending to a dead-letter queue (DLQ) here if configured
                return;
            }

            // 3. Create Quote domain object
            var quoteDomainObject = new Quote
            {
                AssetId = asset.Id,
                Price = messageDto.Price,
                QuoteDth = messageDto.Timestamp, // Assuming Timestamp from Kafka is the actual quote time
                CreatedDth = DateTime.UtcNow    // System timestamp for record creation
            };

            // 4. Save Quote and MessageId (transactionally)
            await _databaseService.SaveQuoteAsync(quoteDomainObject, messageDto.MessageId);
            _logger.LogInformation("Successfully saved quote for AssetId {AssetId} (Ticker: {AssetTicker}), Price: {Price}, MessageId: {MessageId}",
                asset.Id, asset.Ticker, quoteDomainObject.Price, messageDto.MessageId);

            // 5. Update client positions (calls the SP to update pos.updated_dth)
            // The SP UpdateClientPositions takes p_asset_id, p_new_price, p_quote_dth
            await _databaseService.UpdateClientPositionsAsync(quoteDomainObject.AssetId, quoteDomainObject.Price);
            _logger.LogInformation("Successfully triggered client position update for AssetId {AssetId} with new price {Price}",
                quoteDomainObject.AssetId, quoteDomainObject.Price);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId} for AssetTicker {AssetTicker}.", messageDto.MessageId, messageDto.AssetTicker);
            // Depending on the exception, you might want to re-throw to let Polly handle retries,
            // or if it's a data validation issue that won't resolve with retries, avoid re-throwing.
            // For critical DB errors, re-throwing might be appropriate.
            // throw; // Uncomment if retries are desired for this type of exception
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Quotes Consumer Worker stopping.");
        await base.StopAsync(stoppingToken);
    }
}
