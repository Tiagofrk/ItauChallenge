using Confluent.Kafka;
using Polly;
using Polly.Retry;

namespace ItauChallenge.QuotesConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _bootstrapServers = "YOUR_KAFKA_BROKER_HERE"; // Placeholder
    private readonly string _topic = "financial-quotes"; // Example topic name
    private readonly string _groupId = "quotes-consumer-group"; // Example consumer group
    private readonly AsyncRetryPolicy _consumerPolicy;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;

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

    private async Task ProcessMessageAsync(string message, CancellationToken stoppingToken)
    {
        // Simulate message processing and idempotency check
        _logger.LogInformation("Processing message: {Message}", message);

        // TODO: Implement actual processing logic (e.g., saving to DB)
        // TODO: Implement idempotency check (e.g., check if message ID already processed)
        // For now, just a delay to simulate work
        await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);

        _logger.LogInformation("Finished processing message: {Message}", message);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Quotes Consumer Worker stopping.");
        await base.StopAsync(stoppingToken);
    }
}
